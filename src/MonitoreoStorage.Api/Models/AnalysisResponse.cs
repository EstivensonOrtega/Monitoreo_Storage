namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Representa la respuesta del análisis inteligente de logs.
/// </summary>
public class AnalysisResponse
{
    /// <summary>
    /// Nombre de la aplicación analizada.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora UTC cuando se realizó el análisis.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }

    /// <summary>
    /// Total de registros analizados.
    /// </summary>
    public int TotalRecordsAnalyzed { get; set; }

    /// <summary>
    /// Resultados del análisis estructurado.
    /// </summary>
    public AnalysisResults AnalysisResults { get; set; } = new();

    /// <summary>
    /// Log de auditoría del análisis realizado.
    /// </summary>
    public AuditLog AuditLog { get; set; } = new();
}

/// <summary>
/// Contiene los resultados estructurados del análisis.
/// </summary>
public class AnalysisResults
{
    /// <summary>
    /// Resumen de errores encontrados.
    /// </summary>
    public ErrorSummary ErrorSummary { get; set; } = new();

    /// <summary>
    /// Lista de problemas específicos detectados.
    /// </summary>
    public DetectedIssue[] DetectedIssues { get; set; } = Array.Empty<DetectedIssue>();

    /// <summary>
    /// Análisis de rendimiento de servicios.
    /// </summary>
    public PerformanceAnalysis PerformanceAnalysis { get; set; } = new();

    /// <summary>
    /// Recomendaciones categorizadas por tiempo.
    /// </summary>
    public Recommendations Recommendations { get; set; } = new();
}

/// <summary>
/// Resumen cuantitativo de errores detectados.
/// </summary>
public class ErrorSummary
{
    /// <summary>
    /// Número de errores críticos detectados.
    /// </summary>
    public int CriticalErrors { get; set; }

    /// <summary>
    /// Número de errores no críticos detectados.
    /// </summary>
    public int NonCriticalErrors { get; set; }

    /// <summary>
    /// Número de problemas de rendimiento detectados.
    /// </summary>
    public int PerformanceIssues { get; set; }

    /// <summary>
    /// Número de patrones recurrentes identificados.
    /// </summary>
    public int RecurrentPatterns { get; set; }
}

/// <summary>
/// Análisis de rendimiento de servicios.
/// </summary>
public class PerformanceAnalysis
{
    /// <summary>
    /// Lista de servicios con problemas de rendimiento.
    /// </summary>
    public SlowService[] SlowServices { get; set; } = Array.Empty<SlowService>();
}

/// <summary>
/// Información de un servicio con problemas de rendimiento.
/// </summary>
public class SlowService
{
    /// <summary>
    /// Nombre del servicio afectado.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Tiempo promedio de respuesta en milisegundos.
    /// </summary>
    public string AverageResponseTime { get; set; } = string.Empty;

    /// <summary>
    /// Umbral configurado para el servicio.
    /// </summary>
    public string Threshold { get; set; } = string.Empty;

    /// <summary>
    /// Recomendación específica para mejorar el rendimiento.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Recomendaciones categorizadas por horizonte temporal.
/// </summary>
public class Recommendations
{
    /// <summary>
    /// Acciones que deben tomarse inmediatamente.
    /// </summary>
    public string[] Immediate { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Acciones a implementar a corto plazo.
    /// </summary>
    public string[] ShortTerm { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Acciones a considerar a largo plazo.
    /// </summary>
    public string[] LongTerm { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Información de auditoría del análisis realizado.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Identificador único del análisis.
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Tiempo total de procesamiento en milisegundos.
    /// </summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>
    /// Número de tokens utilizados por el LLM.
    /// </summary>
    public int LlmTokensUsed { get; set; }

    /// <summary>
    /// Lista de reglas aplicadas durante el análisis.
    /// </summary>
    public string[] RulesApplied { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Indica si se utilizó fallback a reglas predefinidas.
    /// </summary>
    public bool UsedFallback { get; set; }
}
