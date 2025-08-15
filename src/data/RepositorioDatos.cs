using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using CsvHelper;
using System.Globalization;
using System.Linq;

// Representa una provincia
public class Provincia
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public List<Municipio> Municipios { get; set; } = new List<Municipio>();
}

// Representa un municipio
public class Municipio
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
}

// Representa un barrio
public class Barrio
{
    public string Fid { get; set; } = string.Empty;
    public string Prov { get; set; } = string.Empty;
    public string Mun { get; set; } = string.Empty;
    public string Dm { get; set; } = string.Empty;
    public string Secc { get; set; } = string.Empty;
    public string Bp { get; set; } = string.Empty;
    public double AreaKm { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// Configuración del sistema
public class ConfiguracionSistema
{
    public int NumeroBrigadasInicial { get; set; }
    public int TiempoSimulacionSegundos { get; set; }
    public double ProbabilidadEmergenciaPorMinuto { get; set; }
    public string RutaDatos { get; set; } = string.Empty;
    public bool UsarParalelismo { get; set; }
    public int MaxHilos { get; set; }
    public MetricasConfig Metricas { get; set; } = new MetricasConfig();
    public SimulacionConfig Simulacion { get; set; } = new SimulacionConfig();
}

// Configuración de métricas
public class MetricasConfig
{
    public bool MedirTiempo { get; set; }
    public bool GenerarGraficas { get; set; }
    public string FormatoGraficas { get; set; } = string.Empty;
}

// Configuración de simulación
public class SimulacionConfig
{
    public string[] TiposEmergencias { get; set; } = Array.Empty<string>();
    public int[] Prioridades { get; set; } = Array.Empty<int>();
}

// Repositorio para cargar datos
public class RepositorioDatos
{
    private readonly string _rutaDatos;

    public RepositorioDatos(string rutaDatos = "./data/")
    {
        _rutaDatos = rutaDatos;
    }

    // Carga provincias desde JSON
    public List<Provincia> CargarProvincias()
    {
        string ruta = Path.Combine(_rutaDatos, "provincias.json");
        string json = File.ReadAllText(ruta);
        var provincias = JsonConvert.DeserializeObject<List<Provincia>>(json) ?? new List<Provincia>();

        Console.WriteLine($"Provincias cargadas: {provincias.Count}");
        foreach (var prov in provincias.Take(5))
            Console.WriteLine($"  - {prov.Codigo}: {prov.Nombre}");
        if (provincias.Count > 5) Console.WriteLine("  ...");

        return provincias;
    }

    // Carga municipios desde JSON y los asocia a provincias
    public void CargarMunicipios(List<Provincia> provincias)
    {
        string ruta = Path.Combine(_rutaDatos, "municipios.json");
        string json = File.ReadAllText(ruta);
        var municipios = JsonConvert.DeserializeObject<List<Municipio>>(json) ?? new List<Municipio>();

        foreach (var mun in municipios)
        {
            var prov = provincias.FirstOrDefault(p => p.Codigo == mun.Provincia);
            if (prov != null) prov.Municipios.Add(mun);
        }

        Console.WriteLine($"Municipios cargados: {municipios.Count}");
        foreach (var mun in municipios.Take(5))
            Console.WriteLine($"  - {mun.Codigo}: {mun.Nombre} (Prov: {mun.Provincia})");
        if (municipios.Count > 5) Console.WriteLine("  ...");
    }

    // Carga configuración desde JSON
    public ConfiguracionSistema CargarConfiguracion()
    {
        string ruta = Path.Combine(_rutaDatos, "configuracion.json");
        string json = File.ReadAllText(ruta);

        var contenedor = JsonConvert.DeserializeObject<Dictionary<string, ConfiguracionSistema>>(json);
        var config = contenedor?["configuracion"] ?? new ConfiguracionSistema();

        Console.WriteLine("Configuración cargada:");
        Console.WriteLine($"  Brigadas iniciales: {config.NumeroBrigadasInicial}");
        Console.WriteLine($"  Tiempo simulación: {config.TiempoSimulacionSegundos}");
        Console.WriteLine($"  Probabilidad emergencia: {config.ProbabilidadEmergenciaPorMinuto}");
        Console.WriteLine($"  Tipos emergencias: {string.Join(", ", config.Simulacion.TiposEmergencias)}");

        return config;
    }

    // Carga barrios desde CSV
    public List<Barrio> CargarBarriosDesdeCsv(string archivoCsv = "result.csv")
    {
        try
        {
            string ruta = Path.Combine(_rutaDatos, archivoCsv);
            using var reader = new StreamReader(ruta);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var barrios = csv.GetRecords<Barrio>().ToList();

            Console.WriteLine($"Barrios cargados: {barrios.Count}");
            foreach (var barrio in barrios.Take(5))
                Console.WriteLine($"  - {barrio.Nombre} ({barrio.Mun})");
            if (barrios.Count > 5) Console.WriteLine("  ...");

            return barrios;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Archivo CSV de barrios no encontrado.");
            return new List<Barrio>();
        }
    }
}
