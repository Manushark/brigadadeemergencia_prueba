using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using BrigadasEmergenciaRD.Metrics.Analyzers;
using BrigadasEmergenciaRD.Metrics.Collectors;
using BrigadasEmergenciaRD.Metrics.Models;

namespace BrigadasEmergenciaRD.Metrics.Comparisons
{
    // Ejecuta escenarios secuencial y paralelo y devuelve snapshots y comparacion
    public static class BenchmarkRunner
    {
        public static (MetricSnapshot sec, MetricSnapshot par, double speedup, double eficiencia)
            Run(string nombre, Func<int, Task> secuencial, Func<int, Task> paralelo, int iteraciones, int gradoParalelismo)
        {
            var met = new MetricasRendimiento();

            // warmup simple para estabilizar JIT
            Warmup(secuencial).Wait();

            // ----- Secuencial -----
            met.Reset(); met.Start();
            var t0 = Stopwatch.GetTimestamp();
            for (int i = 0; i < iteraciones; i++)
            {
                var a0 = Stopwatch.GetTimestamp();
                try { secuencial(i).Wait(); met.EndOp(Stopwatch.GetTimestamp() - a0, true); }
                catch { met.EndOp(Stopwatch.GetTimestamp() - a0, false); }
            }
            var secSnap = met.Snapshot();
            var secDur = (Stopwatch.GetTimestamp() - t0) / (double)Stopwatch.Frequency;

            // ----- Paralelo -----
            Warmup(paralelo).Wait();
            met.Reset(); met.Start();
            t0 = Stopwatch.GetTimestamp();
            Parallel.For(0, iteraciones, new ParallelOptions { MaxDegreeOfParallelism = gradoParalelismo }, i =>
            {
                var a0 = Stopwatch.GetTimestamp();
                try { paralelo(i).Wait(); met.EndOp(Stopwatch.GetTimestamp() - a0, true); }
                catch { met.EndOp(Stopwatch.GetTimestamp() - a0, false); }
            });
            var parSnap = met.Snapshot();
            var parDur = (Stopwatch.GetTimestamp() - t0) / (double)Stopwatch.Frequency;

            // comparacion
            var r = AnalizadorResultados.Comparar(secDur, parDur, gradoParalelismo);

            // completa nombres y campos
            secSnap = secSnap with { Nombre = nombre + "_Secuencial", GradoParalelismo = 1, DuracionSeg = secDur };
            parSnap = parSnap with { Nombre = nombre + "_Paralela", GradoParalelismo = gradoParalelismo, DuracionSeg = parDur };

            return (secSnap, parSnap, r.speedup, r.eficiencia);
        }

        private static async Task Warmup(Func<int, Task> f)
        {
            for (int i = 0; i < 5; i++) await f(i);
        }
    }
}
