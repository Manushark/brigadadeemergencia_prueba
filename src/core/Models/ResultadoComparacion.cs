using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class ResultadoComparacion
    {
        public TimeSpan TiempoEjecucionSecuencial { get; set; }
        public TimeSpan TiempoEjecucionParalela { get; set; }
        public double SpeedupObtenido { get; set; }
        public double EficienciaParalela { get; set; }
        public int NucleosProcesadorUtilizados { get; set; }
        public int GradoParalealismoPromedio { get; set; }

        public string EstrategiaParalelizacion { get; set; }
        public EstadisticasParalelismo DetallesParalelismo { get; set; }

        public ResultadoComparacion()
        {
            DetallesParalelismo = new EstadisticasParalelismo();
        }

        public void CalcularMetricas()
        {
            if (TiempoEjecucionParalela.TotalMilliseconds > 0)
            {
                SpeedupObtenido = TiempoEjecucionSecuencial.TotalMilliseconds /
                                 TiempoEjecucionParalela.TotalMilliseconds;

                EficienciaParalela = SpeedupObtenido / NucleosProcesadorUtilizados * 100;
            }
        }

        public string GenerarReporte()
        {
            return $@"
=== COMPARACIÓN RENDIMIENTO SECUENCIAL VS PARALELO ===

⏱️ TIEMPOS DE EJECUCIÓN:
- Secuencial: {TiempoEjecucionSecuencial:mm\:ss\.fff}
- Paralela: {TiempoEjecucionParalela:mm\:ss\.fff}

📈 MÉTRICAS DE PARALELIZACIÓN:
- Speedup: {SpeedupObtenido:F2}x
- Eficiencia: {EficienciaParalela:F1}%
- Núcleos utilizados: {NucleosProcesadorUtilizados}
- Estrategia: {EstrategiaParalelizacion}

💪 MEJORA DE RENDIMIENTO: {((SpeedupObtenido - 1) * 100):F1}%
";
        }
    }
}
