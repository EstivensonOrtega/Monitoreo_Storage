using Azure;
using Azure.Data.Tables;
using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services
{
    /// <summary>
    /// Implementación del servicio que consulta tablas de Azure Storage y devuelve los registros solicitados.
    /// </summary>
    public class TableReadService : ITableReadService
    {
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Crea una nueva instancia de <see cref="TableReadService"/>.
    /// </summary>
    /// <param name="configuration">Proveedor de configuración para resolver connection strings.</param>
    public TableReadService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Resuelve la cadena de conexión asociada a un nombre de aplicación.
    /// </summary>
    /// <param name="applicationName">Nombre lógico de la aplicación (por ejemplo "AppSalud").</param>
    /// <returns>Connection string si existe; en caso contrario null.</returns>
    private string? ResolveConnectionString(string applicationName)
    {
        // First try to get from ConnectionStrings section
        var connectionString = _configuration.GetConnectionString(applicationName);
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Fallback to environment variable key mapping for production
        var key = applicationName switch
        {
            "AppSalud" => "AZURE_STORAGE_CONNECTIONSTRING_APPSALUD",
            "LinaChatbot" => "AZURE_STORAGE_CONNECTIONSTRING_LINACHATBOT",
            _ => null
        };

        if (key == null) return null;
        return _configuration[key];
    }

    /// <summary>
    /// Consulta las tablas indicadas en el request, aplica el filtro por Timestamp y retorna hasta <c>MaxRecords</c> por tabla.
    /// </summary>
    /// <param name="request">Parámetros de consulta.</param>
    /// <returns>Respuesta con resultados por tabla.</returns>
    public async Task<LogsQueryResponse> QueryTablesAsync(LogsQueryRequest request)
    {
        var response = new LogsQueryResponse { ApplicationName = request.ApplicationName };
        var results = new List<TableQueryResult>();

        var connStr = ResolveConnectionString(request.ApplicationName);
        if (string.IsNullOrEmpty(connStr))
        {
            // Devolver error para cada tabla solicitada si no existe la cadena de conexión
            foreach (var t in request.TablesToAnalyze)
            {
                results.Add(new TableQueryResult
                {
                    TableName = t,
                    StartDateUtc = request.StartDateUtc,
                    EndDateUtc = request.EndDateUtc,
                    RecordsReturned = 0,
                    Records = Array.Empty<object>(),
                    Status = "ERROR",
                    ErrorMessage = "Connection string not found for application"
                });
            }

            response.TableResults = results.ToArray();
            return response;
        }

        var serviceClient = new TableServiceClient(connStr);

        foreach (var tableName in request.TablesToAnalyze)
        {
            var tableResult = new TableQueryResult
            {
                TableName = tableName,
                StartDateUtc = request.StartDateUtc,
                EndDateUtc = request.EndDateUtc
            };

            try
            {
                var tableClient = serviceClient.GetTableClient(tableName);
                // Verificar existencia de la tabla
                try
                {
                    await tableClient.GetAccessPoliciesAsync();
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    tableResult.Status = "ERROR";
                    tableResult.ErrorMessage = "Table not found";
                    tableResult.RecordsReturned = 0;
                    tableResult.Records = Array.Empty<object>();
                    results.Add(tableResult);
                    continue;
                }

                // Construir filtro OData para Timestamp en UTC
                var startUtc = request.StartDateUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'");
                var endUtc = request.EndDateUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'");
                var filter = $"Timestamp ge datetime'{startUtc}' and Timestamp le datetime'{endUtc}'";

                // Agregar filtro adicional para AppSalud: excluir tipos específicos
                if (request.ApplicationName == "AppSalud")
                {
                    filter += " and Type ne 'REST_ExternalServiceTraceability' and Type ne 'SOAP_ExternalServiceTraceability'";
                }

                var entities = tableClient.QueryAsync<TableEntity>(filter: filter);

                var collected = new List<object>();
                await foreach (var ent in entities)
                {
                    if (collected.Count >= request.MaxRecords) break;
                    // Normalizar propiedades básicas, excluyendo campos odata
                    var obj = new Dictionary<string, object?>();
                    foreach (var prop in ent)
                    {
                        // Excluir campos que comienzan con "odata." como odata.etag
                        if (!prop.Key.StartsWith("odata.", StringComparison.OrdinalIgnoreCase))
                        {
                            obj[prop.Key] = prop.Value;
                        }
                    }

                    // Asegurar formato ISO-8601 para Timestamp
                    if (ent.TryGetValue("Timestamp", out var ts) && ts is DateTimeOffset dto)
                    {
                        obj["Timestamp"] = dto.UtcDateTime.ToString("o");
                    }

                    collected.Add(obj);
                }

                tableResult.Records = collected.ToArray();
                tableResult.RecordsReturned = collected.Count;
                tableResult.Status = "OK";
            }
            catch (Exception ex)
            {
                tableResult.Status = "ERROR";
                tableResult.ErrorMessage = ex.Message;
                tableResult.RecordsReturned = 0;
                tableResult.Records = Array.Empty<object>();
            }

            results.Add(tableResult);
        }

        response.TableResults = results.ToArray();
        return response;
    }
    }
}
