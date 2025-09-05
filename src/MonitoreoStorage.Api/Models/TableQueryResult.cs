namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Resultado de la consulta para una tabla específica.
/// </summary>
public class TableQueryResult
{
    /// <summary>
    /// Nombre de la tabla consultada.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Fecha/hora UTC inicio del rango solicitado.
    /// </summary>
    public DateTime StartDateUtc { get; set; }

    /// <summary>
    /// Fecha/hora UTC fin del rango solicitado.
    /// </summary>
    public DateTime EndDateUtc { get; set; }

    /// <summary>
    /// Cantidad de registros retornados (truncada a MaxRecords si aplica).
    /// </summary>
    public int RecordsReturned { get; set; }

    /// <summary>
    /// Registros devueltos (array de objetos con propiedades normalizadas).
    /// </summary>
    public object[] Records { get; set; } = Array.Empty<object>();

    /// <summary>
    /// Estado de la consulta para la tabla (OK | ERROR).
    /// </summary>
    public string Status { get; set; } = "OK";

    /// <summary>
    /// Mensaje de error en caso de fallo.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Respuesta agregada de la consulta que contiene resultados por tabla.
/// </summary>
public class LogsQueryResponse
{
    /// <summary>
    /// Nombre de la aplicación solicitante.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Resultados por cada tabla solicitada.
    /// </summary>
    public TableQueryResult[] TableResults { get; set; } = Array.Empty<TableQueryResult>();
}
