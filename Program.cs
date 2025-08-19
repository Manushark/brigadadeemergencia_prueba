using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.Core.Interfaces;
using BrigadasEmergenciaRD.Core.Models;
using BrigadasEmergenciaRD.Data;
using BrigadasEmergenciaRD.Parallelism;

class Program
{
    private static List<Provincia> _provincias = new();
    private static List<Brigada> _todasBrigadas = new();
    private static Random _random = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("🇩🇴 SISTEMA DE BRIGADAS DE EMERGENCIA - REPÚBLICA DOMINICANA");
        Console.WriteLine("============================================================\n");

        try
        {
            // Inicializar datos
            await InicializarSistemaAsync();

            // Mostrar menú principal
            await MostrarMenuPrincipalAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error crítico: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\n👋 Gracias por usar el sistema de Brigadas RD");
    }

    static async Task InicializarSistemaAsync()
    {
        Console.WriteLine("🔧 Inicializando sistema...");

        // Configurar ruta de datos
        var rutaData = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", "data"));
        Console.WriteLine($"📁 Directorio de datos: {rutaData}");

        // Crear data provider real
        var dataProvider = new JsonDataProvider(rutaData);

        // Cargar datos completos
        _provincias = await dataProvider.ObtenerDatosCompletosAsync();
        _todasBrigadas = _provincias.SelectMany(p => p.BrigadasDisponibles).ToList();

        Console.WriteLine($"✅ Sistema inicializado:");
        Console.WriteLine($"   📊 Provincias: {_provincias.Count}");
        Console.WriteLine($"   🏛️ Municipios: {_provincias.Sum(p => p.Municipios.Count)}");
        Console.WriteLine($"   🏘️ Barrios: {_provincias.SelectMany(p => p.Municipios).Sum(m => m.Barrios.Count)}");
        Console.WriteLine($"   🚑 Brigadas: {_todasBrigadas.Count}\n");
    }

    static async Task MostrarMenuPrincipalAsync()
    {
        while (true)
        {
            Console.WriteLine("📋 MENÚ PRINCIPAL");
            Console.WriteLine("================");
            Console.WriteLine("1. 🔥 Simulación con datos reales (Secuencial)");
            Console.WriteLine("2. ⚡ Simulación con datos reales (Paralela)");
            Console.WriteLine("3. ⚖️ Comparar rendimiento (Secuencial vs Paralela)");
            Console.WriteLine("4. 📊 Ver estadísticas del sistema");
            Console.WriteLine("5. 🗺️ Ver datos por región");
            Console.WriteLine("0. 🚪 Salir");
            Console.Write("\nElige una opción: ");

            var opcion = Console.ReadKey().KeyChar;
            Console.WriteLine("\n");

            switch (opcion)
            {
                case '1':
                    await EjecutarSimulacionSecuencialAsync();
                    break;
                case '2':
                    await EjecutarSimulacionParalelaAsync();
                    break;
                case '3':
                    await CompararRendimientoAsync();
                    break;
                case '4':
                    MostrarEstadisticasSistema();
                    break;
                case '5':
                    MostrarDatosPorRegion();
                    break;
                case '0':
                    return;
                default:
                    Console.WriteLine("❌ Opción inválida");
                    break;
            }

            Console.WriteLine("\nPresiona cualquier tecla para continuar...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    static async Task EjecutarSimulacionSecuencialAsync()
    {
        Console.WriteLine("🔄 SIMULACIÓN SECUENCIAL CON DATOS REALES");
        Console.WriteLine("=========================================\n");

        // Configurar simulación
        var cantidadEmergencias = SolicitarCantidadEmergencias();
        var emergencias = GenerarEmergenciasReales(cantidadEmergencias);

        Console.WriteLine($"🎯 Procesando {emergencias.Count} emergencias de forma SECUENCIAL...\n");

        var cronometro = Stopwatch.StartNew();
        var procesadas = 0;
        var errores = 0;

        // Procesar secuencialmente
        for (int i = 0; i < emergencias.Count; i++)
        {
            try
            {
                var emergencia = emergencias[i];
                var resultado = await ProcesarEmergenciaConDatosRealesAsync(emergencia, CancellationToken.None);

                procesadas++;

                // Mostrar progreso cada 25%
                if (i > 0 && (i % (emergencias.Count / 4) == 0 || i == emergencias.Count - 1))
                {
                    Console.WriteLine($"📈 Progreso: {i + 1}/{emergencias.Count} ({(double)(i + 1) / emergencias.Count * 100:F1}%) - {resultado.brigada}");
                }
            }
            catch (Exception ex)
            {
                errores++;
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        cronometro.Stop();
        MostrarResultadosSimulacion("SECUENCIAL", cronometro.Elapsed, procesadas, errores, emergencias.Count);
    }

    static async Task EjecutarSimulacionParalelaAsync()
    {
        Console.WriteLine("⚡ SIMULACIÓN PARALELA CON DATOS REALES");
        Console.WriteLine("======================================\n");

        // Configurar simulación
        var cantidadEmergencias = SolicitarCantidadEmergencias();
        var emergencias = GenerarEmergenciasReales(cantidadEmergencias);

        // Configurar paralelismo
        var nucleos = Environment.ProcessorCount;
        var config = new ConfigParalelo
        {
            MaxGradoParalelismo = nucleos,
            HabilitarMetricas = true,
            CapacidadCola = cantidadEmergencias + 100
        };

        Console.WriteLine($"🧵 Usando {nucleos} núcleos de procesador");
        Console.WriteLine($"🎯 Procesando {emergencias.Count} emergencias de forma PARALELA...\n");

        using var gestorParalelo = new GestorParaleloExtendido(config, recursosDisponibles: _todasBrigadas.Count);

        // Encolar emergencias
        foreach (var emergencia in emergencias)
        {
            gestorParalelo.EncolarEmergencia(emergencia);
        }

        // Procesar en paralelo
        var (tiempo, procesadas) = await gestorParalelo.ProcesarEnParaleloAsync(ProcesarEmergenciaConDatosRealesAsync);

        // Mostrar estadísticas paralelas
        var stats = gestorParalelo.ObtenerEstadisticas();
        var errores = gestorParalelo.Resultados.Count(r => r.brigadaId == "ERROR");

        MostrarResultadosSimulacion("PARALELA", tiempo, procesadas, errores, emergencias.Count);
        MostrarEstadisticasParalelismo(stats);
    }

    static async Task CompararRendimientoAsync()
    {
        Console.WriteLine("⚖️ COMPARACIÓN DE RENDIMIENTO");
        Console.WriteLine("=============================\n");

        var cantidadEmergencias = SolicitarCantidadEmergencias();
        var emergencias = GenerarEmergenciasReales(cantidadEmergencias);

        Console.WriteLine("🔄 Ejecutando versión secuencial...");
        var tiempoSecuencial = await MedirTiempoSecuencialAsync(emergencias);

        Console.WriteLine("⚡ Ejecutando versión paralela...");
        var (tiempoParalelo, statsParalelo) = await MedirTiempoParaleloAsync(emergencias);

        // Mostrar comparación
        Console.WriteLine("\n📊 RESULTADOS DE COMPARACIÓN");
        Console.WriteLine("============================");
        Console.WriteLine($"🔄 Tiempo secuencial:  {tiempoSecuencial.TotalSeconds:F2} segundos");
        Console.WriteLine($"⚡ Tiempo paralelo:    {tiempoParalelo.TotalSeconds:F2} segundos");

        if (tiempoParalelo.TotalSeconds > 0)
        {
            var speedup = tiempoSecuencial.TotalSeconds / tiempoParalelo.TotalSeconds;
            var eficiencia = speedup / Environment.ProcessorCount * 100;

            Console.WriteLine($"🚀 Aceleración (Speedup): {speedup:F2}x");
            Console.WriteLine($"📈 Eficiencia: {eficiencia:F1}%");
            Console.WriteLine($"💾 Núcleos utilizados: {Environment.ProcessorCount}");

            if (speedup > 1.5)
                Console.WriteLine("✅ El paralelismo ofrece una mejora significativa");
            else if (speedup > 1.0)
                Console.WriteLine("⚠️ El paralelismo ofrece una mejora modest");
            else
                Console.WriteLine("❌ El paralelismo no ofrece ventajas para este caso");
        }
    }

    static int SolicitarCantidadEmergencias()
    {
        while (true)
        {
            Console.Write("📝 Cantidad de emergencias a simular (50-1000): ");
            if (int.TryParse(Console.ReadLine(), out int cantidad) && cantidad >= 50 && cantidad <= 1000)
            {
                return cantidad;
            }
            Console.WriteLine("❌ Por favor ingresa un número entre 50 y 1000");
        }
    }

    static List<EmergenciaEvento> GenerarEmergenciasReales(int cantidad)
    {
        var emergencias = new List<EmergenciaEvento>();
        var tipos = Enum.GetValues<TipoEmergencia>();

        Console.WriteLine("🔧 Generando emergencias con datos geográficos reales...");

        for (int i = 0; i < cantidad; i++)
        {
            // Seleccionar provincia, municipio y barrio reales
            var provincia = _provincias[_random.Next(_provincias.Count)];
            var municipio = provincia.Municipios[_random.Next(provincia.Municipios.Count)];
            var barrio = municipio.Barrios.Count > 0
                ? municipio.Barrios[_random.Next(municipio.Barrios.Count)]
                : null;

            // Usar coordenadas reales con pequeña variación
            var coordenadas = barrio?.Coordenadas ?? municipio.Coordenadas;
            var ubicacionFinal = new Coordenada(
                coordenadas.Latitud + (_random.NextDouble() - 0.5) * 0.01,  // ±0.01 grados
                coordenadas.Longitud + (_random.NextDouble() - 0.5) * 0.01
            );

            var tipoEmergencia = tipos[_random.Next(tipos.Length)];

            emergencias.Add(new EmergenciaEvento
            {
                Id = i + 1,
                Tipo = tipoEmergencia,
                ProvinciaId = provincia.Id,
                MunicipioId = municipio.Id,
                BarrioId = barrio?.Id ?? municipio.Id,
                PersonasAfectadas = GenerarPersonasAfectadas(tipoEmergencia),
                Intensidad = GenerarIntensidad(provincia.VulnerabilidadClimatica),
                Ubicacion = ubicacionFinal,
                Descripcion = $"{tipoEmergencia} en {barrio?.Nombre ?? municipio.Nombre}, {municipio.Nombre}",
                Timestamp = DateTime.Now.AddMinutes(-_random.Next(0, 180))
            });
        }

        return emergencias;
    }

    static int GenerarPersonasAfectadas(TipoEmergencia tipo)
    {
        return tipo switch
        {
            TipoEmergencia.PersonasAtrapadas => _random.Next(1, 8),
            TipoEmergencia.IncendioEstructural => _random.Next(5, 25),
            TipoEmergencia.EmergenciaMedica => _random.Next(1, 3),
            TipoEmergencia.Inundacion => _random.Next(10, 100),
            TipoEmergencia.DeslizamientoTierra => _random.Next(3, 20),
            TipoEmergencia.AccidenteVehicular => _random.Next(1, 6),
            _ => _random.Next(1, 10)
        };
    }

    static IntensidadTormenta GenerarIntensidad(string vulnerabilidadClimatica)
    {
        var intensidades = Enum.GetValues<IntensidadTormenta>();

        return vulnerabilidadClimatica switch
        {
            "Alta" => intensidades[_random.Next(2, 4)], // Moderada a Extrema
            "Media" => intensidades[_random.Next(1, 3)], // Baja a Alta
            _ => intensidades[_random.Next(0, 2)] // Baja a Moderada
        };
    }

    static async Task<(string brigada, TimeSpan tiempo)> ProcesarEmergenciaConDatosRealesAsync(
        EmergenciaEvento emergencia, CancellationToken ct)
    {
        // Buscar brigada más cercana y apropiada
        var brigadaMasCercana = EncontrarBrigadaMasCercana(emergencia);

        // Simular tiempo de procesamiento realista
        var tiempoBase = CalcularTiempoRespuesta(emergencia, brigadaMasCercana);
        var variacion = _random.Next(-50, 150); // Variabilidad realista
        var tiempoFinal = Math.Max(50, tiempoBase + variacion); // Mínimo 50ms

        await Task.Delay(tiempoFinal, ct);

        return (brigadaMasCercana.Nombre, TimeSpan.FromMilliseconds(tiempoFinal));
    }

    static Brigada EncontrarBrigadaMasCercana(EmergenciaEvento emergencia)
    {
        // Filtrar brigadas que pueden atender este tipo de emergencia
        var brigadasCapaces = _todasBrigadas.Where(b =>
            b.Estado == EstadoBrigada.Disponible &&
            b.PuedeAtender(emergencia.Tipo)
        ).ToList();

        if (!brigadasCapaces.Any())
        {
            // Si no hay brigadas específicas disponibles, usar Defensa Civil
            brigadasCapaces = _todasBrigadas.Where(b =>
                b.Tipo == TipoBrigada.DefensaCivil &&
                b.Estado == EstadoBrigada.Disponible
            ).ToList();
        }

        if (!brigadasCapaces.Any())
        {
            // Fallback: cualquier brigada disponible
            brigadasCapaces = _todasBrigadas.Where(b => b.Estado == EstadoBrigada.Disponible).ToList();
        }

        if (!brigadasCapaces.Any())
        {
            // Última opción: primera brigada disponible
            return _todasBrigadas.First();
        }

        // Encontrar la más cercana geográficamente
        var brigada = brigadasCapaces
            .OrderBy(b => b.UbicacionActual.CalcularDistanciaKm(emergencia.Ubicacion))
            .First();

        // Simular que la brigada se ocupa temporalmente
        brigada.CambiarEstado(EstadoBrigada.AtendendoEmergencia);

        // Simular que regresa disponible después de un tiempo
        _ = Task.Run(async () =>
        {
            await Task.Delay(_random.Next(5000, 15000));
            brigada.CambiarEstado(EstadoBrigada.Disponible);
        });

        return brigada;
    }

    static int CalcularTiempoRespuesta(EmergenciaEvento emergencia, Brigada brigada)
    {
        var tiempoBase = emergencia.Tipo switch
        {
            TipoEmergencia.PersonasAtrapadas => 300,
            TipoEmergencia.IncendioEstructural => 400,
            TipoEmergencia.EmergenciaMedica => 200,
            TipoEmergencia.Inundacion => 350,
            TipoEmergencia.DeslizamientoTierra => 450,
            TipoEmergencia.AccidenteVehicular => 250,
            TipoEmergencia.VientosFuertes => 300,
            TipoEmergencia.CorteEnergia => 500,
            _ => 250
        };

        // Ajustar por distancia
        var distancia = brigada.UbicacionActual.CalcularDistanciaKm(emergencia.Ubicacion);
        var ajusteDistancia = (int)(distancia * 10); // 10ms por km

        // Ajustar por intensidad
        var ajusteIntensidad = (int)emergencia.Intensidad * 50;

        return tiempoBase + ajusteDistancia + ajusteIntensidad;
    }

    static void MostrarResultadosSimulacion(string modo, TimeSpan tiempo, int procesadas, int errores, int total)
    {
        Console.WriteLine($"\n🎯 RESULTADOS - MODO {modo}");
        Console.WriteLine("=" + new string('=', 25 + modo.Length));
        Console.WriteLine($"📊 Total emergencias: {total}");
        Console.WriteLine($"✅ Procesadas: {procesadas}");
        Console.WriteLine($"❌ Errores: {errores}");
        Console.WriteLine($"📈 Tasa de éxito: {(double)procesadas / total * 100:F1}%");
        Console.WriteLine($"⏱️ Tiempo total: {tiempo.TotalSeconds:F2} segundos");
        Console.WriteLine($"🚀 Throughput: {procesadas / Math.Max(tiempo.TotalSeconds, 0.1):F1} emergencias/segundo");
    }

    static void MostrarEstadisticasParalelismo(EstadisticasParalelismo stats)
    {
        Console.WriteLine($"\n🧵 ESTADÍSTICAS DE PARALELISMO");
        Console.WriteLine("==============================");
        Console.WriteLine($"🔢 Hilos concurrentes: {stats.MaximoHilosConcurrentes}");
        Console.WriteLine($"📈 Tareas totales: {stats.TotalTareasEjecutadas}");
        Console.WriteLine($"⚡ Tareas en paralelo: {stats.TareasEjecutadasEnParalelo}");

        if (stats.MedicionesPorHilo.Any())
        {
            var minTareas = stats.MedicionesPorHilo.Min(m => m.TareasCompletadas);
            var maxTareas = stats.MedicionesPorHilo.Max(m => m.TareasCompletadas);
            var desbalance = maxTareas > 0 ? (double)(maxTareas - minTareas) / maxTareas * 100 : 0;

            Console.WriteLine($"⚖️ Balance de carga: {desbalance:F1}% desbalance");
            Console.WriteLine($"📊 Tareas por hilo: {minTareas}-{maxTareas}");
        }
    }

    static async Task<TimeSpan> MedirTiempoSecuencialAsync(List<EmergenciaEvento> emergencias)
    {
        var cronometro = Stopwatch.StartNew();

        foreach (var emergencia in emergencias)
        {
            try
            {
                await ProcesarEmergenciaConDatosRealesAsync(emergencia, CancellationToken.None);
            }
            catch
            {
                // Ignorar errores para la medición
            }
        }

        cronometro.Stop();
        return cronometro.Elapsed;
    }

    static async Task<(TimeSpan tiempo, EstadisticasParalelismo stats)> MedirTiempoParaleloAsync(List<EmergenciaEvento> emergencias)
    {
        var config = new ConfigParalelo
        {
            MaxGradoParalelismo = Environment.ProcessorCount,
            HabilitarMetricas = true
        };

        using var gestor = new GestorParaleloExtendido(config, recursosDisponibles: _todasBrigadas.Count);

        foreach (var emergencia in emergencias)
        {
            gestor.EncolarEmergencia(emergencia);
        }

        var (tiempo, _) = await gestor.ProcesarEnParaleloAsync(ProcesarEmergenciaConDatosRealesAsync);
        var stats = gestor.ObtenerEstadisticas();

        return (tiempo, stats);
    }

    static void MostrarEstadisticasSistema()
    {
        Console.WriteLine("📊 ESTADÍSTICAS DEL SISTEMA");
        Console.WriteLine("===========================\n");

        Console.WriteLine("🏛️ PROVINCIAS:");
        foreach (var provincia in _provincias.OrderBy(p => p.Nombre))
        {
            Console.WriteLine($"   {provincia.Nombre}: {provincia.Poblacion:N0} habitantes");
            Console.WriteLine($"      Municipios: {provincia.Municipios.Count}");
            Console.WriteLine($"      Barrios: {provincia.Municipios.Sum(m => m.Barrios.Count)}");
            Console.WriteLine($"      Brigadas: {provincia.BrigadasDisponibles.Count}");
            Console.WriteLine($"      Vulnerabilidad: {provincia.VulnerabilidadClimatica}\n");
        }

        Console.WriteLine("🚑 BRIGADAS POR TIPO:");
        var brigadasPorTipo = _todasBrigadas.GroupBy(b => b.Tipo);
        foreach (var grupo in brigadasPorTipo)
        {
            Console.WriteLine($"   {grupo.Key}: {grupo.Count()} brigadas");
        }

        Console.WriteLine("\n🌍 COBERTURA TOTAL:");
        Console.WriteLine($"   📍 Población total: {_provincias.Sum(p => p.Poblacion):N0} habitantes");
        Console.WriteLine($"   🏘️ Total barrios: {_provincias.SelectMany(p => p.Municipios).Sum(m => m.Barrios.Count)}");
        Console.WriteLine($"   🚑 Total brigadas: {_todasBrigadas.Count}");
        Console.WriteLine($"   👥 Capacidad promedio por brigada: {_todasBrigadas.Average(b => b.CapacidadMaxima):F1} personas");
    }

    static void MostrarDatosPorRegion()
    {
        Console.WriteLine("🗺️ DATOS POR REGIÓN");
        Console.WriteLine("===================\n");

        var regiones = _provincias.GroupBy(p => p.Region);

        foreach (var region in regiones.OrderBy(r => r.Key))
        {
            Console.WriteLine($"📍 REGIÓN {region.Key.ToUpper()}");
            Console.WriteLine($"   Provincias: {region.Count()}");
            Console.WriteLine($"   Población: {region.Sum(p => p.Poblacion):N0} habitantes");
            Console.WriteLine($"   Municipios: {region.Sum(p => p.Municipios.Count)}");
            Console.WriteLine($"   Brigadas: {region.Sum(p => p.BrigadasDisponibles.Count)}");

            Console.WriteLine("   Provincias incluidas:");
            foreach (var provincia in region.OrderBy(p => p.Nombre))
            {
                Console.WriteLine($"     • {provincia.Nombre} ({provincia.VulnerabilidadClimatica} vulnerabilidad)");
            }
            Console.WriteLine();
        }
    }
}