using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class MetricasSnapshot
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, TimeSpan> TiemposMedidos { get; set; }
        public Dictionary<string, object> EventosRegistrados { get; set; }
        public Dictionary<string, double> ContadoresRendimiento { get; set; }

        public MetricasSnapshot()
        {
            Timestamp = DateTime.Now;
            TiemposMedidos = new Dictionary<string, TimeSpan>();
            EventosRegistrados = new Dictionary<string, object>();
            ContadoresRendimiento = new Dictionary<string, double>();
        }
    }
}
