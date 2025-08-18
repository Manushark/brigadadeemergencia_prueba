using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Core.Interfaces
{
    // Define cómo recolectar y generar métricas del sistema
    public interface IMetricas
    {
        void IniciarMedicion(string nombre);         // Inicia una medición con un nombre
        void FinalizarMedicion(string nombre);       // Finaliza una medición
        void RegistrarEvento(string nombre, object valor); // Registra un evento con un valor específico

        MetricasSnapshot TomarSnapshot();            // Captura un estado actual de métricas
        ResultadoComparacion GenerarComparacion();   // Compara resultados entre simulaciones

        Task GenerarReporteAsync(string rutaArchivo);      // Genera un reporte en archivo
        Task GenerarGraficasAsync(string rutaCarpeta);     // Genera gráficas de resultados
    }
}
