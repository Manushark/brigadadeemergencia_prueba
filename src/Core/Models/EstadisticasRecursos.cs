using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class EstadisticasRecursos
    {
        public double UsoCPUMaximo { get; set; }
        public double UsoCPUPromedio { get; set; }
        public long UsoMemoriaMaximoMB { get; set; }
        public long UsoMemoriaPromedioMB { get; set; }
        public int HilosMaximosConcurrentes { get; set; }
        public int OperacionesIOTotales { get; set; }

        public Dictionary<string, int> RecursosUtilizados { get; set; }

        public EstadisticasRecursos()
        {
            RecursosUtilizados = new Dictionary<string, int>();
        }
    }
}
