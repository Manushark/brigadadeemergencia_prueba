using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Models;


namespace BrigadasEmergenciaRD.Core.Interfaces
{
    // Esta interfaz define la forma en que se obtienen y almacenan los datos usados por el sistema.
    public interface IRepositorioDatos
    {
        // Obtiene todas las provincias registradas.
        Task<IEnumerable<Provincia>> CargarProvinciasAsync();
        
        Task<IEnumerable<Municipio>> CargarMunicipiosAsync();
        // Obtiene los barrios de una provincia específica.
        Task<IEnumerable<Barrio>> CargarBarriosAsync();
        // Obtiene todas las llamadas de emergencia registradas.

        Task<ConfiguracionSistema> CargarConfiguracionAsync();

        // Métodos para guardar resultados y brigadas
        Task GuardarResultadosAsync(ResultadoSimulacion resultado);
        Task<IEnumerable<Brigada>> ObtenerBrigadasDisponiblesAsync(int provinciaId);
    }

}
