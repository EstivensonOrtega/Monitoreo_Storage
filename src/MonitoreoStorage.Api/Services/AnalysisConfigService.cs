using MonitoreoStorage.Api.Models;
using System.Text.Json;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Implementación del servicio de configuración de análisis.
/// </summary>
public class AnalysisConfigService : IAnalysisConfigService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnalysisConfigService> _logger;
    private GlobalAnalysisConfiguration? _cachedConfig;
    private readonly SemaphoreSlim _configLock = new(1, 1);

    /// <summary>
    /// Crea una nueva instancia del servicio de configuración.
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="logger">Logger para registrar eventos.</param>
    public AnalysisConfigService(IConfiguration configuration, ILogger<AnalysisConfigService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la configuración de análisis para una aplicación específica.
    /// </summary>
    public async Task<AnalysisConfiguration> GetConfigurationAsync(string applicationName)
    {
        var globalConfig = await GetGlobalConfigurationAsync();
        
        if (globalConfig.Applications.TryGetValue(applicationName, out var appConfig))
        {
            return appConfig;
        }

        _logger.LogWarning("No se encontró configuración específica para {ApplicationName}, usando configuración por defecto", applicationName);
        
        // Configuración por defecto
        return GetDefaultConfiguration(applicationName);
    }

    /// <summary>
    /// Obtiene la configuración global de análisis.
    /// </summary>
    public async Task<GlobalAnalysisConfiguration> GetGlobalConfigurationAsync()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        await _configLock.WaitAsync();
        try
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var configPath = _configuration["ANALYSIS_CONFIG_PATH"] ?? "./Configuration/analysis-thresholds.json";
            
            if (File.Exists(configPath))
            {
                var configJson = await File.ReadAllTextAsync(configPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                _cachedConfig = JsonSerializer.Deserialize<GlobalAnalysisConfiguration>(configJson, options);
                _logger.LogInformation("Configuración de análisis cargada desde {ConfigPath}", configPath);
            }
            else
            {
                _logger.LogWarning("Archivo de configuración no encontrado en {ConfigPath}, usando configuración por defecto", configPath);
                _cachedConfig = GetDefaultGlobalConfiguration();
            }

            return _cachedConfig ?? GetDefaultGlobalConfiguration();
        }
        finally
        {
            _configLock.Release();
        }
    }

    /// <summary>
    /// Obtiene la configuración por defecto para una aplicación.
    /// </summary>
    private AnalysisConfiguration GetDefaultConfiguration(string applicationName)
    {
        return applicationName switch
        {
            "AppSalud" => new AnalysisConfiguration
            {
                ResponseTimeThresholds = new Dictionary<string, int>
                {
                    ["ConsultarAfiliado"] = 20000,
                    ["AuthenticationService"] = 20000,
                    ["default"] = 20000
                },
                ErrorPatterns = new ErrorPatterns
                {
                    Critical = new[] { "JsonReaderException", "ConnectionTimeout", "OutOfMemoryException", "StackOverflowException" },
                    Warning = new[] { "ValidationError", "SlowResponse", "RetryableError" }
                },
                RecurrenceThreshold = 3
            },
            "LinaChatbot" => new AnalysisConfiguration
            {
                ResponseTimeThresholds = new Dictionary<string, int>
                {
                    ["MessageProcessing"] = 1000,
                    ["default"] = 2000
                },
                ErrorPatterns = new ErrorPatterns
                {
                    Critical = new[] { "MessageDeliveryFailure", "AuthenticationFailure" },
                    Warning = new[] { "SlowProcessing", "RetryableError" }
                },
                RecurrenceThreshold = 2
            },
            _ => new AnalysisConfiguration
            {
                ResponseTimeThresholds = new Dictionary<string, int>
                {
                    ["default"] = 3000
                },
                ErrorPatterns = new ErrorPatterns
                {
                    Critical = new[] { "Exception", "Error", "Failed" },
                    Warning = new[] { "Warning", "Slow" }
                },
                RecurrenceThreshold = 3
            }
        };
    }

    /// <summary>
    /// Obtiene la configuración global por defecto.
    /// </summary>
    private GlobalAnalysisConfiguration GetDefaultGlobalConfiguration()
    {
        return new GlobalAnalysisConfiguration
        {
            Applications = new Dictionary<string, AnalysisConfiguration>
            {
                ["AppSalud"] = GetDefaultConfiguration("AppSalud"),
                ["LinaChatbot"] = GetDefaultConfiguration("LinaChatbot")
            }
        };
    }
}
