using System.Net.Http.Headers;
using System.Text.Json;
using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Implementación de <see cref="IGitHubSearchService"/> que usa la API de búsqueda de código de GitHub
/// para encontrar repositorios públicos cuyos archivos .csproj contienen los paquetes NuGet indicados.
/// </summary>
public class GitHubSearchService : IGitHubSearchService
{
    private const string GitHubApiBase = "https://api.github.com";
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubSearchService> _logger;
    private readonly string? _githubToken;

    /// <summary>
    /// Crea una nueva instancia de <see cref="GitHubSearchService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP para llamadas a la API de GitHub.</param>
    /// <param name="configuration">Configuración de la aplicación (lee GITHUB_TOKEN).</param>
    /// <param name="logger">Logger para registrar eventos.</param>
    public GitHubSearchService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GitHubSearchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _githubToken = configuration["GITHUB_TOKEN"];
    }

    /// <inheritdoc/>
    public async Task<GitHubSearchResponse> SearchRepositoriesByNugetPackagesAsync(
        string[] packages,
        int maxResults = 30,
        CancellationToken cancellationToken = default)
    {
        if (packages == null || packages.Length == 0)
        {
            return new GitHubSearchResponse
            {
                PackagesSearched = [],
                Message = "Se requiere al menos un paquete NuGet para buscar."
            };
        }

        _logger.LogInformation(
            "Buscando repositorios públicos de GitHub con los paquetes: {Packages}",
            string.Join(", ", packages));

        // Build query: all packages must appear in the same .csproj file
        // Example: "Reactor.Maui Syncfusion.Maui.Toolkit extension:csproj"
        var queryTerms = string.Join(" ", packages);
        var query = $"{queryTerms} extension:csproj";
        var perPage = Math.Min(maxResults, 100);

        var url = $"{GitHubApiBase}/search/code?q={Uri.EscapeDataString(query)}&per_page={perPage}";

        using var request = BuildRequest(HttpMethod.Get, url);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al conectar con la API de GitHub");
            return new GitHubSearchResponse
            {
                PackagesSearched = packages,
                Message = $"Error al conectar con la API de GitHub: {ex.Message}"
            };
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "GitHub API respondió con {StatusCode}: {Body}",
                (int)response.StatusCode, errorBody);

            var friendlyMsg = (int)response.StatusCode switch
            {
                401 => "Token de GitHub inválido o expirado. Configure la variable GITHUB_TOKEN.",
                403 => "Límite de tasa de la API de GitHub alcanzado. Configure GITHUB_TOKEN para aumentar el límite.",
                422 => "Consulta de búsqueda no válida para la API de GitHub.",
                _ => $"Error en la API de GitHub: {(int)response.StatusCode} {response.ReasonPhrase}"
            };

            return new GitHubSearchResponse
            {
                PackagesSearched = packages,
                UsedAnonymousAccess = string.IsNullOrEmpty(_githubToken),
                Message = friendlyMsg
            };
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        var items = doc.RootElement.TryGetProperty("items", out var itemsEl)
            ? itemsEl.EnumerateArray().ToList()
            : [];

        // Group .csproj files by repository and keep only repos that appear in results
        // (GitHub's code search already applies the AND logic for terms in the same file)
        var repoMap = new Dictionary<string, GitHubRepository>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (!item.TryGetProperty("repository", out var repoEl)) continue;

            var fullName = repoEl.TryGetProperty("full_name", out var fn) ? fn.GetString() ?? "" : "";
            var htmlUrl = repoEl.TryGetProperty("html_url", out var hu) ? hu.GetString() ?? "" : "";
            var description = repoEl.TryGetProperty("description", out var desc) && desc.ValueKind != JsonValueKind.Null
                ? desc.GetString()
                : null;
            var isPrivate = repoEl.TryGetProperty("private", out var priv) && priv.GetBoolean();
            var stars = repoEl.TryGetProperty("stargazers_count", out var st) ? st.GetInt32() : 0;

            var filePath = item.TryGetProperty("path", out var pathEl) ? pathEl.GetString() ?? "" : "";
            var fileHtmlUrl = item.TryGetProperty("html_url", out var fhu) ? fhu.GetString() ?? "" : "";

            if (!repoMap.TryGetValue(fullName, out var repo))
            {
                repo = new GitHubRepository
                {
                    FullName = fullName,
                    HtmlUrl = htmlUrl,
                    Description = description,
                    IsPrivate = isPrivate,
                    Stars = stars
                };
                repoMap[fullName] = repo;
            }

            repo.CsprojFiles.Add(new GitHubCsprojFile
            {
                Path = filePath,
                HtmlUrl = fileHtmlUrl
            });
        }

        var repositories = repoMap.Values
            .Where(r => !r.IsPrivate)
            .OrderByDescending(r => r.Stars)
            .Take(maxResults)
            .ToList();

        _logger.LogInformation(
            "Búsqueda completada: {Count} repositorios públicos encontrados con los paquetes {Packages}",
            repositories.Count, string.Join(", ", packages));

        return new GitHubSearchResponse
        {
            PackagesSearched = packages,
            TotalRepositories = repositories.Count,
            UsedAnonymousAccess = string.IsNullOrEmpty(_githubToken),
            Repositories = repositories,
            Message = repositories.Count == 0
                ? "No se encontraron repositorios públicos que contengan todos los paquetes indicados."
                : null
        };
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("MonitoreoStorage", "1.0"));
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        if (!string.IsNullOrEmpty(_githubToken))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _githubToken);
        }

        return req;
    }
}
