namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Configuración de análisis por aplicación.
/// </summary>
public class AnalysisConfiguration
{
    /// <summary>
    /// Umbrales de tiempo de respuesta por servicio en milisegundos.
    /// </summary>
    public Dictionary<string, int> ResponseTimeThresholds { get; set; } = new();

    /// <summary>
    /// Patrones de error categorizados por severidad.
    /// </summary>
    public ErrorPatterns ErrorPatterns { get; set; } = new();

    /// <summary>
    /// Número mínimo de ocurrencias para considerar un patrón como recurrente.
    /// </summary>
    public int RecurrenceThreshold { get; set; } = 3;
}

/// <summary>
/// Patrones de error por categoría de severidad.
/// </summary>
public class ErrorPatterns
{
    /// <summary>
    /// Patrones que indican errores críticos.
    /// </summary>
    public string[] Critical { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Patrones que indican advertencias.
    /// </summary>
    public string[] Warning { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Configuración global de análisis para todas las aplicaciones.
/// </summary>
public class GlobalAnalysisConfiguration
{
    /// <summary>
    /// Configuraciones específicas por aplicación.
    /// </summary>
    public Dictionary<string, AnalysisConfiguration> Applications { get; set; } = new();
}
