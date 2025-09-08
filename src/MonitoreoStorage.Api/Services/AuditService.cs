using MonitoreoStorage.Api.Models;
using System.Text.Json;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Implementación del servicio de auditoría para análisis.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;

    /// <summary>
    /// Crea una nueva instancia del servicio de auditoría.
    /// </summary>
    /// <param name="logger">Logger para registrar eventos.</param>
    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registra el inicio de un análisis.
    /// </summary>
    public Task LogAnalysisStartAsync(string analysisId, AnalysisRequest request)
    {
        _logger.LogInformation("Análisis iniciado - ID: {AnalysisId}, Aplicación: {ApplicationName}, Modo: {AnalysisMode}", 
            analysisId, request.ApplicationName, request.AnalysisMode);

        // Estructura de log para análisis posterior
        var auditData = new
        {
            AnalysisId = analysisId,
            Timestamp = DateTime.UtcNow,
            Event = "AnalysisStarted",
            ApplicationName = request.ApplicationName,
            AnalysisMode = request.AnalysisMode,
            TablesCount = request.TablesToAnalyze.Length,
            DateRange = new { request.StartDateUtc, request.EndDateUtc },
            MaxRecords = request.MaxRecords,
            MaxResponseTimeMs = request.MaxResponseTimeMs
        };

        _logger.LogInformation("Audit: {AuditData}", JsonSerializer.Serialize(auditData));
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registra la finalización exitosa de un análisis.
    /// </summary>
    public Task LogAnalysisCompletedAsync(string analysisId, AuditLog auditLog)
    {
        _logger.LogInformation("Análisis completado - ID: {AnalysisId}, Tiempo: {ProcessingTimeMs}ms, Tokens: {LlmTokensUsed}", 
            analysisId, auditLog.ProcessingTimeMs, auditLog.LlmTokensUsed);

        // Estructura de log para análisis posterior
        var auditData = new
        {
            AnalysisId = analysisId,
            Timestamp = DateTime.UtcNow,
            Event = "AnalysisCompleted",
            ProcessingTimeMs = auditLog.ProcessingTimeMs,
            LlmTokensUsed = auditLog.LlmTokensUsed,
            RulesApplied = auditLog.RulesApplied,
            UsedFallback = auditLog.UsedFallback
        };

        _logger.LogInformation("Audit: {AuditData}", JsonSerializer.Serialize(auditData));

        // Log métricas para monitoreo
        if (auditLog.ProcessingTimeMs > 30000) // Más de 30 segundos
        {
            _logger.LogWarning("Análisis lento detectado - ID: {AnalysisId}, Tiempo: {ProcessingTimeMs}ms", 
                analysisId, auditLog.ProcessingTimeMs);
        }

        if (auditLog.LlmTokensUsed > 100000) // Muchos tokens utilizados
        {
            _logger.LogWarning("Alto uso de tokens detectado - ID: {AnalysisId}, Tokens: {LlmTokensUsed}", 
                analysisId, auditLog.LlmTokensUsed);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Registra un error durante el análisis.
    /// </summary>
    public Task LogAnalysisErrorAsync(string analysisId, string error, Exception? exception = null)
    {
        _logger.LogError(exception, "Error en análisis - ID: {AnalysisId}, Error: {Error}", analysisId, error);

        // Estructura de log para análisis posterior
        var auditData = new
        {
            AnalysisId = analysisId,
            Timestamp = DateTime.UtcNow,
            Event = "AnalysisError",
            Error = error,
            ExceptionType = exception?.GetType().Name,
            ExceptionMessage = exception?.Message
        };

        _logger.LogError("Audit: {AuditData}", JsonSerializer.Serialize(auditData));

        return Task.CompletedTask;
    }
}
