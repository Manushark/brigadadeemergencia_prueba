using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class EstadisticasProvincia
    {
        public int ProvinciaId { get; set; }
        public string NombreProvincia { get; set; }
        public int EmergenciasGeneradas { get; set; }
        public int EmergenciasAtendidas { get; set; }
        public int BrigadasUtilizadas { get; set; }
        public TimeSpan TiempoPromedioRespuesta { get; set; }
        public double PorcentajeExito { get; set; }

        public List<TipoEmergenciaStats> EmergenciasPorTipo { get; set; }

        public EstadisticasProvincia()
        {
            EmergenciasPorTipo = new List<TipoEmergenciaStats>();
        }
        public class TipoEmergenciaStats
        {
            public TipoEmergencia Tipo { get; set; }
            public int Cantidad { get; set; }
            public TimeSpan TiempoPromedioAtencion { get; set; }
            public int PersonasAfectadas { get; set; }
        }
    }
}