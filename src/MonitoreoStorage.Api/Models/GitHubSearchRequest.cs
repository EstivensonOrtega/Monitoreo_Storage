namespace MonitoreoStorage.Api.Models;

/// <summary>
/// Representa la petición para buscar repositorios públicos de GitHub
/// que contengan paquetes NuGet específicos en sus archivos .csproj.
/// </summary>
public class GitHubSearchRequest
{
    /// <summary>
    /// Lista de paquetes NuGet que deben estar presentes en el archivo .csproj.
    /// Por defecto incluye "Reactor.Maui" y "Syncfusion.Maui.Toolkit".
    /// </summary>
    public string[] Packages { get; set; } =
    [
        "Reactor.Maui",
        "Syncfusion.Maui.Toolkit"
    ];

    /// <summary>
    /// Número máximo de resultados a retornar (máximo 100 por limitación de la API de GitHub).
    /// </summary>
    public int MaxResults { get; set; } = 30;
}
