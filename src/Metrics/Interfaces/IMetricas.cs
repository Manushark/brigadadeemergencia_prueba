namespace BrigadasEmergenciaRD.Metrics.Interfaces
{
    using BrigadasEmergenciaRD.Metrics.Models;

    // Contrato generico para recolectar metricas
    public interface IMetricas
    {
        // Hook de inicio de operacion
        void BeginOp();

        // Fin de operacion: duracion en ticks y si fue exitosa
        void EndOp(long elapsedTicks, bool success);

        // Suma bytes procesados
        void AddBytes(long bytes);

        // Devuelve un resumen inmutable del estado actual
        MetricSnapshot Snapshot();

        // Limpia buffers y contadores
        void Reset();
    }
}
