using MonitoreoStorage.Api.Models;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Interfaz para el servicio de auditoría.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Registra el inicio de un análisis.
    /// </summary>
    /// <param name="analysisId">Identificador único del análisis.</param>
    /// <param name="request">Petición de análisis.</param>
    /// <returns>Task para operación asíncrona.</returns>
    Task LogAnalysisStartAsync(string analysisId, AnalysisRequest request);

    /// <summary>
    /// Registra la finalización exitosa de un análisis.
    /// </summary>
    /// <param name="analysisId">Identificador único del análisis.</param>
    /// <param name="auditLog">Log de auditoría del análisis.</param>
    /// <returns>Task para operación asíncrona.</returns>
    Task LogAnalysisCompletedAsync(string analysisId, AuditLog auditLog);

    /// <summary>
    /// Registra un error durante el análisis.
    /// </summary>
    /// <param name="analysisId">Identificador único del análisis.</param>
    /// <param name="error">Descripción del error.</param>
    /// <param name="exception">Excepción capturada (opcional).</param>
    /// <returns>Task para operación asíncrona.</returns>
    Task LogAnalysisErrorAsync(string analysisId, string error, Exception? exception = null);
}
