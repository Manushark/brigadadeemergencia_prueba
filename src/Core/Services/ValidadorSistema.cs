using System;
using System.Collections.Generic;
using System.Linq;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Core.Services
{
  
    // Servicio básico para validación de datos y configuraciones del sistema
    // Se enfoca únicamente en validar la integridad de los modelos del Core
    public class ValidadorSistema
    {
      
       // Valida la configuración inicial básica del sistema
       
        public bool ValidarConfiguracionInicial()
        {
            try
            {
                Console.WriteLine("🔍 Validando configuración inicial...");

                // Validar que los enums estén correctamente definidos
                if (!ValidarEnums())
                {
                    Console.WriteLine("❌ Error en definición de enums");
                    return false;
                }

                Console.WriteLine("✅ Configuración inicial válida");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error validando configuración: {ex.Message}");
                return false;
            }
        }

      
        // Valida una provincia y su estructura de datos
       
        public bool ValidarProvincia(Provincia provincia)
        {
            if (provincia == null)
            {
                Console.WriteLine("❌ Provincia es nula");
                return false;
            }

            var errores = new List<string>();

            // Validaciones básicas
            if (provincia.Id <= 0)
                errores.Add("ID de provincia inválido");

            if (string.IsNullOrWhiteSpace(provincia.Nombre))
                errores.Add("Nombre de provincia vacío");

            if (provincia.Poblacion < 0)
                errores.Add("Población no puede ser negativa");

            if (provincia.AreaKm2 <= 0)
                errores.Add("Área debe ser mayor que 0");

            if (provincia.Coordenadas == null)
                errores.Add("Coordenadas son requeridas");
            else if (!ValidarCoordenada(provincia.Coordenadas))
                errores.Add("Coordenadas inválidas");

            // Validar municipios
            if (provincia.Municipios != null)
            {
                foreach (var municipio in provincia.Municipios)
                {
                    if (!ValidarMunicipio(municipio))
                        errores.Add($"Municipio inválido: {municipio?.Nombre ?? "Sin nombre"}");
                }
            }

            if (errores.Any())
            {
                Console.WriteLine($"❌ Errores en provincia {provincia.Nombre}:");
                errores.ForEach(e => Console.WriteLine($"  - {e}"));
                return false;
            }

            return true;
        }

      
        // Valida un municipio
       
        public bool ValidarMunicipio(Municipio municipio)
        {
            if (municipio == null) return false;

            return municipio.Id > 0 &&
                   !string.IsNullOrWhiteSpace(municipio.Nombre) &&
                   municipio.ProvinciaId > 0 &&
                   municipio.Poblacion >= 0 &&
                   ValidarCoordenada(municipio.Coordenadas);
        }

      
        // Valida un barrio
       
        public bool ValidarBarrio(Barrio barrio)
        {
            if (barrio == null) return false;

            return barrio.Id > 0 &&
                   !string.IsNullOrWhiteSpace(barrio.Nombre) &&
                   barrio.MunicipioId > 0 &&
                   barrio.Poblacion >= 0 &&
                   ValidarCoordenada(barrio.Coordenadas);
        }

      
        // Valida una brigada
       
        public bool ValidarBrigada(Brigada brigada)
        {
            if (brigada == null) return false;

            var errores = new List<string>();

            if (brigada.Id <= 0)
                errores.Add("ID inválido");

            if (string.IsNullOrWhiteSpace(brigada.Nombre))
                errores.Add("Nombre vacío");

            if (!Enum.IsDefined(typeof(TipoBrigada), brigada.Tipo))
                errores.Add("Tipo de brigada inválido");

            if (!Enum.IsDefined(typeof(EstadoBrigada), brigada.Estado))
                errores.Add("Estado de brigada inválido");

            if (brigada.CapacidadMaxima <= 0)
                errores.Add("Capacidad debe ser mayor que 0");

            if (!ValidarCoordenada(brigada.UbicacionActual))
                errores.Add("Ubicación inválida");

            if (errores.Any())
            {
                Console.WriteLine($"❌ Errores en brigada {brigada.Nombre}:");
                errores.ForEach(e => Console.WriteLine($"  - {e}"));
                return false;
            }

            return true;
        }

      
        // Valida una llamada de emergencia
       
        public bool ValidarLlamadaEmergencia(LlamadaEmergencia llamada)
        {
            if (llamada == null) return false;

            return llamada.Id > 0 &&
                   Enum.IsDefined(typeof(TipoEmergencia), llamada.TipoEmergencia) &&
                   ValidarCoordenada(llamada.Ubicacion) &&
                   llamada.PersonasAfectadas >= 0 &&
                   llamada.Prioridad >= 0 && llamada.Prioridad <= 100 &&
                   llamada.BarrioId > 0;
        }

      
        // Valida un evento de emergencia
       
        public bool ValidarEmergenciaEvento(EmergenciaEvento evento)
        {
            if (evento == null) return false;

            return evento.Id > 0 &&
                   Enum.IsDefined(typeof(TipoEmergencia), evento.Tipo) &&
                   Enum.IsDefined(typeof(IntensidadTormenta), evento.Intensidad) &&
                   ValidarCoordenada(evento.Ubicacion) &&
                   evento.PersonasAfectadas >= 0 &&
                   evento.ProvinciaId > 0;
        }

      
        // Valida coordenadas geográficas
       
        public bool ValidarCoordenada(Coordenada coordenada)
        {
            if (coordenada == null) return false;

            // Validar rango de latitud y longitud para República Dominicana
            // RD aproximadamente: Latitud 17.5 - 19.9, Longitud -72.0 - -68.3
            return coordenada.Latitud >= 17.0 && coordenada.Latitud <= 20.0 &&
                   coordenada.Longitud >= -73.0 && coordenada.Longitud <= -68.0;
        }

      
       // Valida que los enums estén correctamente definidos
        private bool ValidarEnums()
        {
            try
            {
                // Verificar que los enums tengan valores válidos
                var tiposEmergencia = Enum.GetValues<TipoEmergencia>();
                var estadosBrigada = Enum.GetValues<EstadoBrigada>();
                var tiposBrigada = Enum.GetValues<TipoBrigada>();
                var intensidadesTormenta = Enum.GetValues<IntensidadTormenta>();

                return tiposEmergencia.Length > 0 &&
                       estadosBrigada.Length > 0 &&
                       tiposBrigada.Length > 0 &&
                       intensidadesTormenta.Length > 0;
            }
            catch
            {
                return false;
            }
        }

      
        // Valida el estado actual general del sistema
        public bool ValidarEstadoActual()
        {
            try
            {
                // Validaciones básicas del sistema
                var memoriaDisponible = GC.GetTotalMemory(false);
                var procesadores = Environment.ProcessorCount;

                Console.WriteLine($"💾 Memoria en uso: {memoriaDisponible / (1024 * 1024)} MB");
                Console.WriteLine($"⚡ Procesadores disponibles: {procesadores}");

                // Sistema está en buen estado si tiene recursos básicos
                return memoriaDisponible < 500 * 1024 * 1024 && // Menos de 500MB
                       procesadores > 0;
            }
            catch
            {
                return false;
            }
        }

      
        // Valida compatibilidad entre brigada y emergencia
        public bool ValidarCompatibilidadBrigadaEmergencia(Brigada brigada, TipoEmergencia tipoEmergencia)
        {
            if (brigada == null) return false;

            return brigada.PuedeAtender(tipoEmergencia);
        }

      
        // Genera reporte de validación de una lista de provincias
        public string GenerarReporteValidacion(IEnumerable<Provincia> provincias)
        {
            if (provincias == null) return "❌ Lista de provincias es nula";

            var provinciasList = provincias.ToList();
            var totalProvincias = provinciasList.Count;
            var provinciasValidas = provinciasList.Count(ValidarProvincia);
            var totalMunicipios = provinciasList.SelectMany(p => p.Municipios ?? new List<Municipio>()).Count();
            var totalBarrios = provinciasList.SelectMany(p => p.Municipios ?? new List<Municipio>())
                                           .SelectMany(m => m.Barrios ?? new List<Barrio>()).Count();

            return $@"
📋 REPORTE DE VALIDACIÓN:
- Provincias válidas: {provinciasValidas}/{totalProvincias}
- Total municipios: {totalMunicipios}
- Total barrios: {totalBarrios}
- Estado general: {(provinciasValidas == totalProvincias ? "✅ VÁLIDO" : "⚠️ CON ERRORES")}
";
        }
    }
}