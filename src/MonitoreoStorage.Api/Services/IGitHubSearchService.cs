using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Servicio para buscar repositorios públicos de GitHub que contengan
/// paquetes NuGet específicos en sus archivos .csproj.
/// </summary>
public interface IGitHubSearchService
{
    /// <summary>
    /// Busca repositorios públicos en GitHub cuyos archivos .csproj contienen
    /// todos los paquetes NuGet indicados.
    /// </summary>
    /// <param name="packages">Lista de paquetes NuGet que deben estar presentes.</param>
    /// <param name="maxResults">Número máximo de repositorios a retornar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Respuesta con la lista de repositorios encontrados.</returns>
    Task<GitHubSearchResponse> SearchRepositoriesByNugetPackagesAsync(
        string[] packages,
        int maxResults = 30,
        CancellationToken cancellationToken = default);
}
