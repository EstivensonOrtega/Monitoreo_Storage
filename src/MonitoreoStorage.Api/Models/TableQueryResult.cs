namespace MonitoreoStorage.Api.Models;

public class TableQueryResult
{
    public string TableName { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int RecordsReturned { get; set; }
    public object[] Records { get; set; } = Array.Empty<object>();
    public string Status { get; set; } = "OK";
    public string? ErrorMessage { get; set; }
}

public class LogsQueryResponse
{
    public string ApplicationName { get; set; } = string.Empty;
    public TableQueryResult[] TableResults { get; set; } = Array.Empty<TableQueryResult>();
}
