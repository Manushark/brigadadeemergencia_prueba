using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Models;


namespace BrigadasEmergenciaRD.Core.Interfaces
{
    // Esta interfaz define las reglas para un sistema que simula emergencias y la respuesta de las brigadas.
    // Define cómo debe funcionar un simulador de emergencias
    public interface ISimulador
    {
        // Genera una lista de emergencias en un tiempo y territorio dado
        Task<IEnumerable<EmergenciaEvento>> GenerarEventosAsync(
            TimeSpan duracion,
            IEnumerable<Provincia> provincias);

        // Genera una emergencia aleatoria dentro de una provincia
        EmergenciaEvento GenerarEmergenciaAleatoria(Provincia provincia);

        // Inicia la simulación con parámetros de tiempo y provincias
        Task IniciarSimulacionAsync(
            IEnumerable<Provincia> provincias,
            TimeSpan duracion);

        // Detiene la simulación
        void DetenerSimulacion();

        // Evento que se dispara cuando ocurre una nueva emergencia
        event EventHandler<EmergenciaEvento> EmergenciaGenerada;
    }
}
