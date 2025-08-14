using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class EstadisticasParalelismo
    {
        public int TotalTareasEjecutadas { get; set; }
        public int TareasEjecutadasEnParalelo { get; set; }
        public int MaximoHilosConcurrentes { get; set; }
        public double UsoCPUPromedio { get; set; }
        public List<MedicionHilo> MedicionesPorHilo { get; set; }

        public EstadisticasParalelismo()
        {
            MedicionesPorHilo = new List<MedicionHilo>();
        }
    }

    public class MedicionHilo
    {
        public int IdHilo { get; set; }
        public int TareasCompletadas { get; set; }
        public TimeSpan TiempoTotalEjecucion { get; set; }
        public double PorcentajeUso { get; set; }
    }
}
