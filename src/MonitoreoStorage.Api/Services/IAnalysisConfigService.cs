using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Interfaz para el servicio de configuración de análisis.
/// </summary>
public interface IAnalysisConfigService
{
    /// <summary>
    /// Obtiene la configuración de análisis para una aplicación específica.
    /// </summary>
    /// <param name="applicationName">Nombre de la aplicación.</param>
    /// <returns>Configuración de análisis para la aplicación.</returns>
    Task<AnalysisConfiguration> GetConfigurationAsync(string applicationName);

    /// <summary>
    /// Obtiene la configuración global de análisis.
    /// </summary>
    /// <returns>Configuración global de análisis.</returns>
    Task<GlobalAnalysisConfiguration> GetGlobalConfigurationAsync();
}
