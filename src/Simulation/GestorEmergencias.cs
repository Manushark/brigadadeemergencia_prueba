using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.Core.Interfaces;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Simulation
{
    /// <summary>
    /// Orquesta la atenci�n de llamadas de emergencia:
    /// - Consume llamadas desde las colas concurrentes de cada barrio.
    /// - Selecciona la brigada adecuada (tipo + distancia).
    /// - Despacha la brigada y espera su resultado (simulado) sin bloquear todo el sistema.
    ///
    /// NOTA: Esta clase NO genera llamadas (eso lo hace el SimuladorTormenta).
    ///       Aqu� solo se decide el despacho y se coordina la atenci�n.
    /// </summary>
    public class GestorEmergencias
    {
        // ----------------------------
        //   Dependencias y configuraci�n
        // ----------------------------

        private readonly IDataProvider _data;                 // Acceso a provincias/municipios/barrios/brigadas (Netanel)
        private readonly int _maxAtencionesParalelas;         // L�mite de tareas simult�neas (control de paralelismo)
        private readonly TimeSpan _reintentoSinBrigada;       // Espera antes de reintentar si no hay brigada disponible

        // ----------------------------
        //   Estado interno
        // ----------------------------

        private List<Provincia> _provincias = new();
        private List<Municipio> _municipios = new();
        private List<Barrio> _barrios = new();

        // Control de ciclo
        private CancellationTokenSource _cts;
        private Task _loopTask;

        // L�mite de atenciones concurrentes (ej. igual a cantidad de n�cleos por defecto)
        private readonly SemaphoreSlim _concurrencyGate;

        // Lock liviano por brigada para evitar asignaciones dobles simult�neas
        private readonly ConcurrentDictionary<int, object> _brigadaLocks = new();

        // Intentos por llamada (para evitar reencolar infinitamente)
        private readonly ConcurrentDictionary<int, int> _intentosPorLlamada = new();

        // ----------------------------
        //   Eventos (hooks para m�tricas / logging)
        // ----------------------------

        /// <summary>Se dispara cuando el gestor saca una llamada de una cola de barrio para procesarla.</summary>
        public event Action<LlamadaEmergencia> OnLlamadaTomada;

        /// <summary>Se dispara cuando se asigna una brigada a una llamada.</summary>
        public event Action<LlamadaEmergencia, Brigada> OnBrigadaAsignada;

        /// <summary>Se dispara cuando la llamada fue atendida satisfactoriamente.</summary>
        public event Action<LlamadaEmergencia, Brigada, TimeSpan> OnLlamadaAtendida;

        /// <summary>Se dispara cuando no se encontr� brigada adecuada y la llamada se reencola.</summary>
        public event Action<LlamadaEmergencia> OnLlamadaReencolada;

        // ----------------------------
        //   Constructor
        // ----------------------------

        /// <summary>
        /// Crea un gestor de emergencias.
        /// </summary>
        /// <param name="data">Proveedor de datos (adaptador de Netanel).</param>
        /// <param name="maxAtencionesParalelas">M�ximo de atenciones simult�neas (por defecto: n�cleos de CPU).</param>
        /// <param name="reintentoSinBrigada">Tiempo a esperar antes de reencolar una llamada si no se hall� brigada.</param>
        public GestorEmergencias(
            IDataProvider data,
            int maxAtencionesParalelas = 0,
            TimeSpan? reintentoSinBrigada = null)
        {
            _data = data;
            _maxAtencionesParalelas = maxAtencionesParalelas > 0
                ? maxAtencionesParalelas
                : Math.Max(1, Environment.ProcessorCount);

            _reintentoSinBrigada = reintentoSinBrigada ?? TimeSpan.FromMilliseconds(800);
            _concurrencyGate = new SemaphoreSlim(_maxAtencionesParalelas, _maxAtencionesParalelas);
        }

        // ----------------------------
        //   Preparaci�n de datos
        // ----------------------------

        /// <summary>
        /// Carga provincias, municipios y barrios para poder consumir sus colas de llamadas.
        /// Debe llamarse antes de iniciar el gestor.
        /// </summary>
        public async Task PrepararDatosAsync()
        {
            _provincias = (await _data.ObtenerProvinciasAsync())?.ToList() ?? new List<Provincia>();

            _municipios.Clear();
            foreach (var prov in _provincias)
            {
                var munis = await _data.ObtenerMunicipiosAsync(prov.Id);
                if (munis != null) _municipios.AddRange(munis);
            }

            _barrios.Clear();
            foreach (var muni in _municipios)
            {
                var brrs = await _data.ObtenerBarriosAsync(muni.Id);
                if (brrs != null) _barrios.AddRange(brrs);
            }
        }

        // ----------------------------
        //   Ciclo de consumo
        // ----------------------------

        /// <summary>
        /// Inicia el bucle principal que revisa barrios, toma llamadas y asigna brigadas.
        /// </summary>
        public async Task IniciarAsync(CancellationToken externalToken = default)
        {
            if (_loopTask != null)
                throw new InvalidOperationException("El gestor ya est� en ejecuci�n.");

            if (_barrios.Count == 0)
                await PrepararDatosAsync();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _loopTask = Task.Run(() => LoopAsync(_cts.Token), _cts.Token);
        }

        /// <summary>
        /// Detiene el bucle principal y espera a que finalicen las atenciones en progreso.
        /// </summary>
        public async Task DetenerAsync()
        {
            try
            {
                _cts?.Cancel();
                if (_loopTask != null)
                    await _loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancelaci�n esperada
            }
            finally
            {
                _loopTask = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// Bucle que itera continuamente por los barrios, extrae llamadas y las procesa.
        /// </summary>
        private async Task LoopAsync(CancellationToken ct)
        {
            // Estrategia simple: ronda sobre los barrios y trata de tomar 1 llamada por barrio por iteraci�n.
            while (!ct.IsCancellationRequested)
            {
                var tareas = new List<Task>();

                foreach (var barrio in _barrios)
                {
                    ct.ThrowIfCancellationRequested();

                    if (barrio.TenerLlamadaPendiente(out var llamada))
                    {
                        // Hook: llamada tomada para procesamiento
                        OnLlamadaTomada?.Invoke(llamada);

                        // Procesar la llamada de manera as�ncrona y limitada por _concurrencyGate
                        tareas.Add(ProcesarLlamadaAsync(barrio, llamada, ct));
                    }
                }

                // Espera cooperativa: no bloqueamos si no hay nada que hacer.
                if (tareas.Count > 0)
                    await Task.WhenAll(tareas);

                // Peque�a pausa para no �quemar� CPU si no hay llamadas
                await Task.Delay(150, ct);
            }
        }

        // ----------------------------
        //   L�gica de despacho
        // ----------------------------

        /// <summary>
        /// Procesa una llamada: elige brigada y coordina su atenci�n.
        /// </summary>
        private async Task ProcesarLlamadaAsync(Barrio barrio, LlamadaEmergencia llamada, CancellationToken ct)
        {
            await _concurrencyGate.WaitAsync(ct); // l�mite de atenciones en paralelo
            try
            {
                // 1) Buscar una brigada adecuada (tipo + disponibilidad + cercan�a)
                var brigada = await SeleccionarBrigadaAsync(barrio, llamada, ct);

                if (brigada == null)
                {
                    // No hay brigada disponible adecuada ? reencolar con backoff
                    ManejarSinBrigada(barrio, llamada);
                    return;
                }

                // 2) Asignar de forma segura (lock por brigada para evitar doble asignaci�n)
                var locker = _brigadaLocks.GetOrAdd(brigada.Id, _ => new object());
                lock (locker)
                {
                    // Validaci�n r�pida: si cambi� de estado en el inter�n, abortar asignaci�n
                    if (brigada.Estado != EstadoBrigada.Disponible)
                    {
                        ManejarSinBrigada(barrio, llamada);
                        return;
                    }
                    // Pasar a EnRuta (la clase Brigada cambiar� el resto de estados)
                    brigada.CambiarEstado(EstadoBrigada.EnRuta);
                }

                OnBrigadaAsignada?.Invoke(llamada, brigada);

                // 3) Ejecutar la atenci�n (simulada por la clase Brigada)
                var t0 = DateTime.Now;
                var ok = await brigada.AtenderEmergenciaAsync(llamada);
                var dt = DateTime.Now - t0;

                if (ok)
                {
                    // Llamada atendida; si ya no hay m�s llamadas, marcar barrio como resuelto
                    OnLlamadaAtendida?.Invoke(llamada, brigada, dt);
                    if (barrio.LlamadasPendientes.IsEmpty)
                        barrio.EnEmergencia = false;
                }
                else
                {
                    // La brigada no pudo atender (cambios de estado o capacidades) ? reencolar
                    ManejarSinBrigada(barrio, llamada);
                }
            }
            finally
            {
                _concurrencyGate.Release();
            }
        }

        /// <summary>
        /// Selecciona una brigada disponible y apta para el tipo de emergencia, priorizando la m�s cercana.
        /// Si no encuentra, retorna null.
        /// </summary>
        private async Task<Brigada> SeleccionarBrigadaAsync(Barrio barrio, LlamadaEmergencia llamada, CancellationToken ct)
        {
            // Nota: usamos la provincia del municipio para consultar brigadas.
            var provinciaId = barrio?.Municipio?.ProvinciaId ?? 0;

            var candidatas = (await _data.ObtenerBrigadasDisponiblesAsync(provinciaId))?
                                .ToList() ?? new List<Brigada>();

            // Filtrar por capacidad de atenci�n (tipo)
            candidatas = candidatas
                .Where(b => b.Estado == EstadoBrigada.Disponible && b.PuedeAtender(llamada.TipoEmergencia))
                .ToList();

            if (candidatas.Count == 0)
                return null;

            // Ordenar por distancia a la ubicaci�n de la llamada; desempatar por tiempo desde �ltima activaci�n (brigadas m�s �descansadas� primero)
            var ordenadas = candidatas
                .Select(b => new
                {
                    Brigada = b,
                    Dist = (b.UbicacionActual ?? new Coordenada(0, 0))
                           .CalcularDistanciaKm(llamada.Ubicacion ?? new Coordenada(0, 0)),
                    Descanso = DateTime.Now - (b.UltimaActivacion ?? DateTime.MinValue)
                })
                .OrderBy(x => x.Dist)
                .ThenByDescending(x => x.Descanso)
                .Select(x => x.Brigada)
                .ToList();

            // Regresamos la m�s conveniente
            return ordenadas.FirstOrDefault();
        }

        /// <summary>
        /// Si no hay brigada disponible, la llamada vuelve a la cola del barrio con un peque�o retraso.
        /// Tambi�n se limita el n�mero de reintentos para evitar bucles infinitos.
        /// </summary>
        private void ManejarSinBrigada(Barrio barrio, LlamadaEmergencia llamada)
        {
            var intentos = _intentosPorLlamada.AddOrUpdate(llamada.Id, 1, (_, v) => v + 1);

            OnLlamadaReencolada?.Invoke(llamada);

            // Pol�tica simple: hasta 5 reintentos; luego dejamos la llamada en cola y marcamos barrio en emergencia.
            if (intentos <= 5)
            {
                // Peque�o �backoff�: dejamos pasar un tiempo antes de reencolar para evitar un ciclo inmediato
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(_reintentoSinBrigada);
                        barrio.AgregarLlamadaEmergencia(llamada);
                    }
                    catch { /* intencionalmente ignorado */ }
                });
            }

            barrio.EnEmergencia = true;
        }
    }
}
