using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace BrigadasEmergenciaRD.Metrics.Models
{
    // Resumen inmutable de metricas
    public sealed record MetricSnapshot
    {
        // nombre del escenario, ej: CPU_Secuencial
        public string Nombre { get; init; } = "";
        // cantidad de iteraciones
        public int Iteraciones { get; init; }
        // conteos de exito y fallo
        public long Exitos { get; init; }
        public long Fallos { get; init; }
        // duracion total del escenario en segundos
        public double DuracionSeg { get; init; }
        // operaciones por segundo
        public double ThroughputOpsSeg { get; init; }
        // latencias en milisegundos
        public double LatenciaMediaMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        // bytes totales procesados (opcional)
        public long BytesProcesados { get; init; }
        // info basica de proceso (aprox)
        public double CpuProcesoSeg { get; init; }
        public double MemoriaMb { get; init; }
        // grado de paralelismo
        public int GradoParalelismo { get; init; }
        // marca de tiempo
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    }

    // Buffer mutable interno para acumular latencias y contadores
    internal sealed class MetricBuffer
    {
        // latencias por operacion en ticks
        public readonly ConcurrentQueue<long> LatenciasTicks = new();
        // contadores
        public long Exitos;
        public long Fallos;
        public long Bytes;
        // reloj del escenario
        public readonly Stopwatch Reloj = new();
        // proceso actual para memoria y cpu
        public readonly Process Proc = Process.GetCurrentProcess();
        public TimeSpan CpuInicio;
        public double MemoriaMb;

        // inicia timers
        public void StartTimers()
        {
            Proc.Refresh();
            CpuInicio = Proc.TotalProcessorTime;
            Reloj.Restart();
        }

        // detiene timers y devuelve (cpuSeg, memMb, durSeg)
        public (double cpuSeg, double memMb, double durSeg) StopTimers()
        {
            Reloj.Stop();
            Proc.Refresh();
            var cpuSeg = (Proc.TotalProcessorTime - CpuInicio).TotalSeconds;
            MemoriaMb = Proc.WorkingSet64 / (1024.0 * 1024.0);
            return (cpuSeg, MemoriaMb, Reloj.Elapsed.TotalSeconds);
        }

        // convierte el buffer en snapshot calculando estadisticos
        public static MetricSnapshot ToSnapshot(string nombre, MetricBuffer b, int iters, int grado)
        {
            var lat = b.LatenciasTicks.ToArray();
            Array.Sort(lat); // requerido para percentiles

            // conversion ticks -> ms
            double ticksToMs = 1000.0 / Stopwatch.Frequency;

            // media y percentiles (fallback a 0 si no hay datos)
            double media = lat.Length == 0 ? 0 : lat.Average() * ticksToMs;
            double p95 = lat.Length == 0 ? 0 : lat[(int)Math.Floor(0.95 * (lat.Length - 1))] * ticksToMs;
            double p99 = lat.Length == 0 ? 0 : lat[(int)Math.Floor(0.99 * (lat.Length - 1))] * ticksToMs;

            var dur = b.Reloj.Elapsed.TotalSeconds;

            return new MetricSnapshot
            {
                Nombre = nombre,
                Iteraciones = iters,
                Exitos = b.Exitos,
                Fallos = b.Fallos,
                DuracionSeg = dur,
                ThroughputOpsSeg = dur <= 0 ? 0 : b.Exitos / dur,
                LatenciaMediaMs = media,
                P95Ms = p95,
                P99Ms = p99,
                BytesProcesados = b.Bytes,
                CpuProcesoSeg = 0, // placeholder, no usado en reportes
                MemoriaMb = b.MemoriaMb,
                GradoParalelismo = grado,
                Timestamp = DateTimeOffset.Now
            };
        }
    }
}
