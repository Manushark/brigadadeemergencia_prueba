using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Core.Interfaces
{
    // Define cómo se maneja la simulación en paralelo (múltiples hilos/procesos)
    public interface IGestorParalelo
    {
        Task ProcesarEmergenciasPorTerritorioAsync(IEnumerable<Provincia> provincias); // Procesa emergencias por territorio
        Task ProcesarEmergenciasPorTareaAsync(IEnumerable<EmergenciaEvento> eventos);  // Procesa emergencias por tareas
        Task<ResultadoComparacion> CompararEstrategiasAsync(IEnumerable<Provincia> provincias); // Compara diferentes estrategias

        void ConfigurarGradoParalelismo(int maxParalelismo); // Ajusta el número máximo de tareas en paralelo
        EstadisticasParalelismo ObtenerEstadisticas();       // Obtiene estadísticas del sistema paralelo
    }
}
