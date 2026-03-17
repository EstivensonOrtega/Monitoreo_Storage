namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Respuesta con los repositorios públicos de GitHub que contienen los paquetes NuGet buscados.
/// </summary>
public class GitHubSearchResponse
{
    /// <summary>Paquetes NuGet utilizados como criterio de búsqueda.</summary>
    public string[] PackagesSearched { get; set; } = [];

    /// <summary>Total de repositorios únicos encontrados.</summary>
    public int TotalRepositories { get; set; }

    /// <summary>
    /// Indica si la búsqueda se realizó sin token de GitHub (sujeta a límites de tasa más bajos).
    /// </summary>
    public bool UsedAnonymousAccess { get; set; }

    /// <summary>Repositorios públicos que contienen todos los paquetes buscados.</summary>
    public List<GitHubRepository> Repositories { get; set; } = [];

    /// <summary>Mensaje informativo o de advertencia sobre la búsqueda.</summary>
    public string? Message { get; set; }
}
