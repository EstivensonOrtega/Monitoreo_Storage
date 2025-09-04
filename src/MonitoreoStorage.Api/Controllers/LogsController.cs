using Microsoft.AspNetCore.Mvc;
using MonitoreoStorage.Api.Models;
using MonitoreoStorage.Api.Services;

namespace MonitoreoStorage.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ITableReadService _tableReadService;

    public LogsController(ITableReadService tableReadService)
    {
        _tableReadService = tableReadService;
    }

    [HttpPost("query")]
    public async Task<IActionResult> QueryTables([FromBody] LogsQueryRequest request)
    {
        if (request == null) return BadRequest("Request body required");

        var result = await _tableReadService.QueryTablesAsync(request);
        return Ok(result);
    }
}
