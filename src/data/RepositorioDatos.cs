using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

// Clase que representa una provincia
public class Provincia
{
    // Código único de la provincia
    public string Id { get; set; } = string.Empty;
    // Nombre de la provincia
    public string Nombre { get; set; } = string.Empty;
    // Lista de municipios asociados a la provincia
    public List<Municipio> Municipios { get; set; } = new List<Municipio>();
}

// Clase que representa un municipio
public class Municipio
{
    // Código único del municipio
    public string Id { get; set; } = string.Empty;
    // Nombre del municipio
    public string Nombre { get; set; } = string.Empty;
    // Lista de barrios asociados al municipio
    public List<Barrio> Barrios { get; set; } = new List<Barrio>();
}

// Clase que representa un barrio
public class Barrio
{
    // Identificador único del barrio
    public string Id { get; set; } = string.Empty;
    // Nombre del barrio
    public string Nombre { get; set; } = string.Empty;
    // Lista de subbarrios asociados al barrio
    public List<SubBarrio> Subbarrios { get; set; } = new List<SubBarrio>();
}

// Clase que representa un subbarrio
public class SubBarrio
{
    // Identificador único del subbarrio
    public string Id { get; set; } = string.Empty;
    // Nombre del subbarrio
    public string Nombre { get; set; } = string.Empty;
}

// Clase que contiene la configuración del sistema
public class ConfiguracionSistema
{
    // Número inicial de brigadas para la simulación
    public int NumeroBrigadasInicial { get; set; }
    // Duración de la simulación en segundos
    public int TiempoSimulacionSegundos { get; set; }
    // Probabilidad de que ocurra una emergencia por minuto
    public double ProbabilidadEmergenciaPorMinuto { get; set; }
    // Ruta donde se encuentran los archivos de datos
    public string RutaDatos { get; set; } = string.Empty;
    // Indica si se debe usar paralelismo en la simulación
    public bool UsarParalelismo { get; set; }
    // Número máximo de hilos para el paralelismo
    public int MaxHilos { get; set; }
    // Configuración de métricas
    public MetricasConfig Metricas { get; set; } = new MetricasConfig();
    // Configuración de la simulación
    public SimulacionConfig Simulacion { get; set; } = new SimulacionConfig();
}

// Clase que contiene la configuración de métricas
public class MetricasConfig
{
    // Indica si se debe medir el tiempo de ejecución
    public bool MedirTiempo { get; set; }
    // Indica si se deben generar gráficas
    public bool GenerarGraficas { get; set; }
    // Formato de las gráficas generadas
    public string FormatoGraficas { get; set; } = string.Empty;
}

// Clase que contiene la configuración de la simulación
public class SimulacionConfig
{
    // Tipos de emergencias posibles en la simulación
    public string[] TiposEmergencias { get; set; } = Array.Empty<string>();
    // Prioridades asociadas a las emergencias
    public int[] Prioridades { get; set; } = Array.Empty<int>();
}

// Clase para manejar la carga de datos desde archivos
public class RepositorioDatos
{
    // Ruta base donde se encuentran los archivos de datos
    private readonly string _rutaDatos;

    // Constructor que inicializa la ruta de datos y crea el directorio si no existe
    public RepositorioDatos(string rutaDatos = "./data/")
    {
        _rutaDatos = rutaDatos;
        Directory.CreateDirectory(_rutaDatos);
    }

    // Carga las provincias desde un archivo JSON
    public List<Provincia> CargarProvincias()
    {
        try
        {
            // Construye la ruta al archivo barrios_afectados.json
            string ruta = Path.Combine(_rutaDatos, "barrios_afectados.json");
            // Lee el contenido del archivo
            string json = File.ReadAllText(ruta);
            // Deserializa el JSON a una lista de provincias
            var provincias = JsonConvert.DeserializeObject<List<Provincia>>(json) ?? new List<Provincia>();
            // Muestra información de las provincias cargadas
            Console.WriteLine($"Provincias cargadas: {provincias.Count}");
            foreach (var prov in provincias.Take(5))
                Console.WriteLine($"  - {prov.Id}: {prov.Nombre}");
            if (provincias.Count > 5) Console.WriteLine("  ...");
            return provincias;
        }
        catch (Exception ex)
        {
            // Maneja errores y retorna una lista vacía si falla
            Console.WriteLine($"Error al cargar provincias: {ex.Message}");
            return new List<Provincia>();
        }
    }

    // Carga la configuración del sistema desde un archivo JSON
    public ConfiguracionSistema CargarConfiguracion()
    {
        try
        {
            // Construye la ruta al archivo configuracion.json
            string ruta = Path.Combine(_rutaDatos, "configuracion.json");
            // Lee el contenido del archivo
            string json = File.ReadAllText(ruta);
            // Deserializa el JSON a un diccionario y extrae la configuración
            var data = JsonConvert.DeserializeObject<Dictionary<string, ConfiguracionSistema>>(json);
            var config = data?["configuracion"] ?? new ConfiguracionSistema();
            // Muestra información de la configuración cargada
            Console.WriteLine("Configuración cargada:");
            Console.WriteLine($"  Brigadas iniciales: {config.NumeroBrigadasInicial}");
            Console.WriteLine($"  Tiempo simulación: {config.TiempoSimulacionSegundos}");
            Console.WriteLine($"  Probabilidad emergencia: {config.ProbabilidadEmergenciaPorMinuto}");
            Console.WriteLine($"  Tipos emergencias: {string.Join(", ", config.Simulacion.TiposEmergencias)}");
            return config;
        }
        catch (Exception ex)
        {
            // Maneja errores durante la carga de la configuración
            Console.WriteLine($"Error al cargar configuración: {ex.Message}");
            return new ConfiguracionSistema();
        }
    }
}