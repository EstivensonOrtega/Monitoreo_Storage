namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Información de un repositorio público de GitHub que contiene los paquetes NuGet buscados.
/// </summary>
public class GitHubRepository
{
    /// <summary>Nombre del repositorio (formato owner/repo).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>URL HTML del repositorio.</summary>
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>Descripción del repositorio.</summary>
    public string? Description { get; set; }

    /// <summary>Indica si el repositorio es privado.</summary>
    public bool IsPrivate { get; set; }

    /// <summary>Número de estrellas.</summary>
    public int Stars { get; set; }

    /// <summary>Archivos .csproj encontrados que contienen todos los paquetes buscados.</summary>
    public List<GitHubCsprojFile> CsprojFiles { get; set; } = [];
}

/// <summary>
/// Información de un archivo .csproj encontrado en el repositorio.
/// </summary>
public class GitHubCsprojFile
{
    /// <summary>Ruta del archivo .csproj dentro del repositorio.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>URL HTML del archivo en GitHub.</summary>
    public string HtmlUrl { get; set; } = string.Empty;
}
