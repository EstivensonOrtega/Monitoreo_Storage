using Microsoft.AspNetCore.Mvc;
using MonitoreoStorage.Api.Models;
using MonitoreoStorage.Api.Services;

namespace MonitoreoStorage.Api.Controllers;

/// <summary>
/// Controlador para buscar repositorios públicos de GitHub
/// que contienen paquetes NuGet específicos en sus archivos .csproj.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GitHubController : ControllerBase
{
    private readonly IGitHubSearchService _gitHubSearchService;

    /// <summary>
    /// Crea una nueva instancia de <see cref="GitHubController"/>.
    /// </summary>
    /// <param name="gitHubSearchService">Servicio de búsqueda en GitHub.</param>
    public GitHubController(IGitHubSearchService gitHubSearchService)
    {
        _gitHubSearchService = gitHubSearchService;
    }

    /// <summary>
    /// Busca repositorios públicos de GitHub cuyos archivos .csproj contienen
    /// todos los paquetes NuGet indicados.
    /// Por defecto busca proyectos que incluyan tanto "Reactor.Maui" como "Syncfusion.Maui.Toolkit".
    /// </summary>
    /// <param name="request">
    /// Parámetros de búsqueda: lista de paquetes NuGet y número máximo de resultados.
    /// </param>
    /// <returns>Lista de repositorios públicos con sus archivos .csproj encontrados.</returns>
    [HttpPost("search-nuget-packages")]
    public async Task<IActionResult> SearchNugetPackages(
        [FromBody] GitHubSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest("El body de la petición es requerido.");

        if (request.Packages == null || request.Packages.Length == 0)
            return BadRequest("Se requiere al menos un paquete NuGet en la lista 'packages'.");

        var result = await _gitHubSearchService.SearchRepositoriesByNugetPackagesAsync(
            request.Packages,
            request.MaxResults,
            cancellationToken);

        return Ok(result);
    }
}
