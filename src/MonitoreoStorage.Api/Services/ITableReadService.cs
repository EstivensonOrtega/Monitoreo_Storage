using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Servicio que proporciona operaciones para consultar tablas de Azure Storage.
/// </summary>
public interface ITableReadService
{
    /// <summary>
    /// Consulta las tablas indicadas en el request y devuelve los resultados por tabla.
    /// </summary>
    /// <param name="request">Parámetros de consulta (applicationName, tablas, rango y maxRecords).</param>
    /// <returns>Objeto <see cref="LogsQueryResponse"/> con los resultados por tabla.</returns>
    Task<LogsQueryResponse> QueryTablesAsync(LogsQueryRequest request);
}
