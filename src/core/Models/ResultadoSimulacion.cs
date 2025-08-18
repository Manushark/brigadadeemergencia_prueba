using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class ResultadoSimulacion
    {
        public Guid Id { get; set; }
        public DateTime FechaEjecucion { get; set; }
        public TimeSpan DuracionTotal { get; set; }
        public string ConfiguracionUsada { get; set; }

        // Estadísticas generales
        public int TotalEmergenciasGeneradas { get; set; }
        public int TotalEmergenciasAtendidas { get; set; }
        public int TotalLlamadasRecibidas { get; set; }
        public int TotalBrigadasActivadas { get; set; }

        // Métricas de rendimiento
        public TimeSpan TiempoPromedioRespuesta { get; set; }
        public TimeSpan TiempoMinimoRespuesta { get; set; }
        public TimeSpan TiempoMaximoRespuesta { get; set; }
        public double PorcentajeExito { get; set; }
        public double EficienciaPromedio { get; set; }

        // Estadísticas por provincia
        public List<EstadisticasProvincia> EstadisticasProvincias { get; set; }

        // Comparación secuencial vs paralelo
        public ResultadoComparacion ComparacionRendimiento { get; set; }

        // Uso de recursos
        public EstadisticasRecursos UsoRecursos { get; set; }

        public ResultadoSimulacion()
        {
            Id = Guid.NewGuid();
            FechaEjecucion = DateTime.Now;
            EstadisticasProvincias = new List<EstadisticasProvincia>();
            UsoRecursos = new EstadisticasRecursos();
        }

        public string GenerarResumen()
        {
           return $@"
            === RESUMEN SIMULACIÓN BRIGADAS RD ===
            Fecha: {FechaEjecucion:dd/MM/yyyy HH:mm:ss}
            Duración: {DuracionTotal:hh\:mm\:ss}

            📊 ESTADÍSTICAS GENERALES:
            - Emergencias generadas: {TotalEmergenciasGeneradas}
            - Emergencias atendidas: {TotalEmergenciasAtendidas} ({PorcentajeExito:F1}%)
            - Brigadas activadas: {TotalBrigadasActivadas}
            - Tiempo promedio respuesta: {TiempoPromedioRespuesta:mm\:ss}

            ⚡ RENDIMIENTO:
            - Eficiencia promedio: {EficienciaPromedio:F2}%
            - Tiempo mínimo respuesta: {TiempoMinimoRespuesta:mm\:ss}
            - Tiempo máximo respuesta: {TiempoMaximoRespuesta:mm\:ss}

            🏛️ PROVINCIAS MÁS AFECTADAS:
            {string.Join("\n", EstadisticasProvincias?.Take(5).Select(p => $"- {p.NombreProvincia}: {p.EmergenciasAtendidas} emergencias") ?? new string[0])}
            ";
        }
    }
}
