namespace MonitoreoStorage.Api.Models;

public class LogsQueryRequest
{
    public string ApplicationName { get; set; } = string.Empty;
    public string[] TablesToAnalyze { get; set; } = Array.Empty<string>();
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int MaxRecords { get; set; } = 10;
}
