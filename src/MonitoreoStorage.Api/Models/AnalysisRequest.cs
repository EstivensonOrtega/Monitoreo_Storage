namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Representa la petición para análisis inteligente de logs con LLM.
/// </summary>
public class AnalysisRequest
{
    /// <summary>
    /// Nombre lógico de la aplicación (se utiliza para resolver la cadena de conexión).
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de nombres de tablas a consultar.
    /// </summary>
    public string[] TablesToAnalyze { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Fecha/hora UTC inicio del rango de consulta.
    /// </summary>
    public DateTime StartDateUtc { get; set; }

    /// <summary>
    /// Fecha/hora UTC fin del rango de consulta.
    /// </summary>
    public DateTime EndDateUtc { get; set; }

    /// <summary>
    /// Límite de registros a retornar por tabla.
    /// </summary>
    public int MaxRecords { get; set; } = 10;

    /// <summary>
    /// Tiempo máximo de respuesta en milisegundos para filtrar registros lentos.
    /// </summary>
    public int? MaxResponseTimeMs { get; set; }

    /// <summary>
    /// Modo de análisis: "basic" retorna registros sin procesar, "intelligent" aplica análisis LLM.
    /// </summary>
    public string AnalysisMode { get; set; } = "basic";
}
