using Microsoft.AspNetCore.Mvc;
using MonitoreoStorage.Api.Models;
using MonitoreoStorage.Api.Services;

namespace MonitoreoStorage.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ITableReadService _tableReadService;

    /// <summary>
    /// Crea una nueva instancia de <see cref="LogsController"/>.
    /// </summary>
    /// <param name="tableReadService">Servicio para lectura de tablas.</param>
    public LogsController(ITableReadService tableReadService)
    {
        _tableReadService = tableReadService;
    }

    /// <summary>
    /// Endpoint que recibe el request para consultar tablas y devuelve los resultados por tabla.
    /// </summary>
    /// <param name="request">Parámetros de consulta (applicationName, tablesToAnalyze, startDateUtc, endDateUtc, maxRecords)</param>
    /// <returns>Respuesta con resultados por tabla.</returns>
    [HttpPost("query")]
    public async Task<IActionResult> QueryTables([FromBody] LogsQueryRequest request)
    {
        if (request == null) return BadRequest("Request body required");

        var result = await _tableReadService.QueryTablesAsync(request);
        return Ok(result);
    }
}
