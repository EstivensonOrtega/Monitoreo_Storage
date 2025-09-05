namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Representa la petición para consultar registros en tablas de Azure Storage.
/// </summary>
public class LogsQueryRequest
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
}
