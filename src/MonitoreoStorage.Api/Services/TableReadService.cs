using Azure;
using Azure.Data.Tables;
using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services;

public class TableReadService : ITableReadService
{
    private readonly IConfiguration _configuration;

    public TableReadService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string? ResolveConnectionString(string applicationName)
    {
        // Map applicationName to environment variable key
        var key = applicationName switch
        {
            "AppSalud" => "AZURE_STORAGE_CONNECTIONSTRING_APPSALUD",
            "LinaChatbot" => "AZURE_STORAGE_CONNECTIONSTRING_LINACHATBOT",
            _ => null
        };

        if (key == null) return null;
        return _configuration[key];
    }

    public async Task<LogsQueryResponse> QueryTablesAsync(LogsQueryRequest request)
    {
        var response = new LogsQueryResponse { ApplicationName = request.ApplicationName };
        var results = new List<TableQueryResult>();

        var connStr = ResolveConnectionString(request.ApplicationName);
        if (string.IsNullOrEmpty(connStr))
        {
            // Return error for all requested tables
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
                // Check table exists by retrieving properties
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

                // Build filter
                var filter = TableClient.CreateQueryFilter($"Timestamp ge @start and Timestamp le @end", new { start = request.StartDateUtc, end = request.EndDateUtc });

                var entities = tableClient.QueryAsync<TableEntity>(filter: filter);

                var collected = new List<object>();
                await foreach (var ent in entities)
                {
                    if (collected.Count >= request.MaxRecords) break;
                    // Normalize basic properties
                    var obj = new Dictionary<string, object?>();
                    foreach (var prop in ent)
                    {
                        obj[prop.Key] = prop.Value;
                    }

                    // Ensure Timestamp is in ISO-8601
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
