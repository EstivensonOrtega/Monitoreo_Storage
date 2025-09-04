using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services;

public interface ITableReadService
{
    Task<LogsQueryResponse> QueryTablesAsync(LogsQueryRequest request);
}
