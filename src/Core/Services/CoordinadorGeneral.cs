using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Interfaces;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Core.Services
{
    // Coordinador central que orquesta las diferentes partes del sistema
    // Este es un servicio básico de infraestructura que conecta los componentes
    public class CoordinadorGeneral
    {
        private readonly ISimulador _simulador;
        private readonly IGestorParalelo _gestorParalelo;
        private readonly IRepositorioDatos _repositorio;
        private readonly IMetricas _metricas;
        private readonly ValidadorSistema _validador;

        public CoordinadorGeneral(
            ISimulador simulador,
            IGestorParalelo gestorParalelo,
            IRepositorioDatos repositorio,
            IMetricas metricas,
            ValidadorSistema validador)
        {
            _simulador = simulador;
            _gestorParalelo = gestorParalelo;
            _repositorio = repositorio;
            _metricas = metricas;
            _validador = validador;
        }


        // Inicializa el sistema completo verificando que todos los componentes estén listos
        public async Task<bool> InicializarSistemaAsync()
        {
            Console.WriteLine("🚀 Iniciando coordinador general...");

            try
            {
                // 1. Validar configuración inicial
                if (!_validador.ValidarConfiguracionInicial())
                {
                    Console.WriteLine("❌ Configuración inicial inválida");
                    return false;
                }

                // 2. Cargar datos básicos
                var provincias = await _repositorio.CargarProvinciasAsync();
                if (!provincias.Any())
                {
                    Console.WriteLine("❌ No se pudieron cargar las provincias");
                    return false;
                }

                Console.WriteLine($"✅ Sistema inicializado - {provincias.Count()} provincias cargadas");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error inicializando sistema: {ex.Message}");
                return false;
            }
        }

       
        // Ejecuta el flujo principal del sistema
        public async Task EjecutarFlujoPrincipalAsync(TimeSpan duracion)
        {
            Console.WriteLine("🔄 Iniciando flujo principal del sistema...");

            try
            {
                // Cargar provincias
                var provincias = await _repositorio.CargarProvinciasAsync();
                var provinciasList = provincias.ToList();

                // Iniciar métricas
                _metricas?.IniciarMedicion("flujo_principal");

                // Configurar eventos del simulador
                _simulador.EmergenciaGenerada += OnEmergenciaGenerada;

                // Iniciar simulación
                await _simulador.IniciarSimulacionAsync(provinciasList, duracion);

                Console.WriteLine("✅ Flujo principal completado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en flujo principal: {ex.Message}");
                _metricas?.RegistrarEvento("error_flujo_principal", ex.Message);
            }
            finally
            {
                _metricas?.FinalizarMedicion("flujo_principal");
            }
        }

        // Maneja eventos de emergencia generados por el simulador
        private async void OnEmergenciaGenerada(object sender, EmergenciaEvento emergencia)
        {
            Console.WriteLine($"📡 Coordinador recibió emergencia: {emergencia.Tipo}");

            // Registrar evento para métricas
            _metricas?.RegistrarEvento("emergencia_coordinada", new
            {
                Tipo = emergencia.Tipo,
                ProvinciaId = emergencia.ProvinciaId
            });

            // Este coordinador solo registra - el procesamiento real lo hacen otros módulos
        }

        // Detiene todas las operaciones del sistema de manera ordenada
        public async Task DetenerSistemaAsync()
        {
            Console.WriteLine("🛑 Deteniendo sistema...");

            try
            {
                _simulador?.DetenerSimulacion();

                // Dar tiempo para que se completen operaciones pendientes
                await Task.Delay(2000);

                Console.WriteLine("✅ Sistema detenido correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error deteniendo sistema: {ex.Message}");
            }
        }

        // Obtiene estado general del sistema
        public string ObtenerEstadoSistema()
        {
            try
            {
                var estadoValidacion = _validador.ValidarEstadoActual();

                return $@"
📊 ESTADO DEL SISTEMA:
- Configuración: {(estadoValidacion ? "✅ Válida" : "❌ Inválida")}
- Simulador: {(_simulador != null ? "✅ Activo" : "❌ No disponible")}
- Repositorio: {(_repositorio != null ? "✅ Conectado" : "❌ No disponible")}
- Métricas: {(_metricas != null ? "✅ Funcionando" : "❌ No disponible")}
- Paralelización: {(_gestorParalelo != null ? "✅ Disponible" : "❌ No disponible")}
";
            }
            catch (Exception ex)
            {
                return $"❌ Error obteniendo estado: {ex.Message}";
            }
        }

        // Valida que todos los componentes necesarios estén disponibles
        public bool ValidarComponentes()
        {
            var componentes = new Dictionary<string, bool>
            {
                {"Simulador", _simulador != null},
                {"Repositorio", _repositorio != null},
                {"Métricas", _metricas != null},
                {"GestorParalelo", _gestorParalelo != null},
                {"Validador", _validador != null}
            };

            var componentesValidos = componentes.All(c => c.Value);

            if (!componentesValidos)
            {
                Console.WriteLine("❌ Componentes faltantes:");
                foreach (var comp in componentes.Where(c => !c.Value))
                {
                    Console.WriteLine($"  - {comp.Key}");
                }
            }

            return componentesValidos;
        }
    }

}
