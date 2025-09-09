namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Representa la petición para análisis de logs recientes sin especificar fechas exactas.
/// </summary>
public class RecentAnalysisRequest
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
    /// Número de minutos hacia atrás desde el momento actual para filtrar los registros por Timestamp.
    /// Por ejemplo: 30 = últimos 30 minutos, 60 = última hora, 1440 = último día.
    /// </summary>
    public int MinutesBack { get; set; } = 30;

    /// <summary>
    /// Límite de registros a retornar por tabla.
    /// </summary>
    public int MaxRecords { get; set; } = 10;

    /// <summary>
    /// Tiempo máximo de respuesta en milisegundos para filtrar registros lentos.
    /// Opcional: si no se especifica, no se filtra por tiempo de respuesta.
    /// </summary>
    public int? MaxResponseTimeMs { get; set; }

    /// <summary>
    /// Convierte esta petición de logs recientes a una petición de análisis estándar calculando las fechas.
    /// </summary>
    /// <returns>AnalysisRequest equivalente con fechas calculadas.</returns>
    public AnalysisRequest ToAnalysisRequest()
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddMinutes(-MinutesBack);

        return new AnalysisRequest
        {
            ApplicationName = ApplicationName,
            TablesToAnalyze = TablesToAnalyze,
            StartDateUtc = startTime,
            EndDateUtc = endTime,
            MaxRecords = MaxRecords,
            MaxResponseTimeMs = MaxResponseTimeMs,
            AnalysisMode = "intelligent" // Siempre usar análisis inteligente para logs recientes
        };
    }
}
