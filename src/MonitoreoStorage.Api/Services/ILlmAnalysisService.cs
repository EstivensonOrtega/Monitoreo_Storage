using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Interfaz para el servicio de análisis inteligente con LLM.
/// </summary>
public interface ILlmAnalysisService
{
    /// <summary>
    /// Analiza los datos de logs utilizando LLM para detectar errores, clasificarlos y sugerir acciones.
    /// </summary>
    /// <param name="logData">Datos de logs obtenidos de la Parte 1.</param>
    /// <param name="configuration">Configuración de análisis para la aplicación.</param>
    /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
    /// <returns>Resultado del análisis inteligente.</returns>
    Task<AnalysisResults> AnalyzeLogsAsync(LogsQueryResponse logData, AnalysisConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si el servicio LLM está disponible.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>True si el servicio está disponible, false en caso contrario.</returns>
    Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);
}
