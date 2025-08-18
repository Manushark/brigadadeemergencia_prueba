using System;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Simulation.Algorithms
{
    // Cargas de trabajo demo para validar el modulo de metricas
    public static class WorkloadsDemo
    {
        // CPU-bound: operaciones matematicas
        public static Task SecuencialCpu(int i)
        {
            double x = 0;
            for (int k = 0; k < 200000; k++) x += Math.Sqrt(k + 1);
            return Task.CompletedTask;
        }

        // Paralelo hace lo mismo en este demo
        public static Task ParaleloCpu(int i)
        {
            double x = 0;
            for (int k = 0; k < 200000; k++) x += Math.Sqrt(k + 1);
            return Task.CompletedTask;
        }

        // IO simulado
        public static Task SecuencialIoMock(int i) => Task.Delay(5);
        public static Task ParaleloIoMock(int i) => Task.Delay(5);
    }
}
