using Microsoft.AspNetCore.Mvc;
using MonitoreoStorage.Api.Models;
using MonitoreoStorage.Api.Services;
using System.Diagnostics;

namespace MonitoreoStorage.Api.Controllers;

/// <summary>
/// Controlador para análisis inteligente de logs utilizando LLM.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly ITableReadService _tableReadService;
    private readonly ILlmAnalysisService _llmAnalysisService;
    private readonly IAnalysisConfigService _configService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AnalysisController> _logger;

    /// <summary>
    /// Crea una nueva instancia del controlador de análisis.
    /// </summary>
    /// <param name="tableReadService">Servicio para lectura de tablas (Parte 1).</param>
    /// <param name="llmAnalysisService">Servicio de análisis LLM.</param>
    /// <param name="configService">Servicio de configuración de análisis.</param>
    /// <param name="auditService">Servicio de auditoría.</param>
    /// <param name="logger">Logger para registrar eventos.</param>
    public AnalysisController(
        ITableReadService tableReadService,
        ILlmAnalysisService llmAnalysisService,
        IAnalysisConfigService configService,
        IAuditService auditService,
        ILogger<AnalysisController> logger)
    {
        _tableReadService = tableReadService;
        _llmAnalysisService = llmAnalysisService;
        _configService = configService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza análisis inteligente de logs utilizando LLM para detectar errores, clasificarlos y sugerir acciones de resolución.
    /// </summary>
    /// <param name="request">Parámetros de análisis incluyendo modo inteligente.</param>
    /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
    /// <returns>Resultado del análisis inteligente con errores detectados, clasificación y recomendaciones.</returns>
    /// <response code="200">Análisis completado exitosamente.</response>
    /// <response code="400">Parámetros de request inválidos.</response>
    /// <response code="500">Error interno durante el análisis.</response>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalysisResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<AnalysisResponse>> AnalyzeLogsAsync(
        [FromBody] AnalysisRequest request, 
        CancellationToken cancellationToken = default)
    {
        var analysisId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validar request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(request.ApplicationName))
            {
                return BadRequest("ApplicationName es requerido");
            }

            if (request.TablesToAnalyze?.Length == 0)
            {
                return BadRequest("TablesToAnalyze debe contener al menos una tabla");
            }

            if (request.StartDateUtc >= request.EndDateUtc)
            {
                return BadRequest("StartDateUtc debe ser menor que EndDateUtc");
            }

            // Registrar inicio del análisis
            await _auditService.LogAnalysisStartAsync(analysisId, request);

            _logger.LogInformation("Iniciando análisis inteligente para aplicación: {ApplicationName}", request.ApplicationName);

            // Paso 1: Obtener datos usando el servicio de la Parte 1
            var logsRequest = new LogsQueryRequest
            {
                ApplicationName = request.ApplicationName,
                TablesToAnalyze = request.TablesToAnalyze,
                StartDateUtc = request.StartDateUtc,
                EndDateUtc = request.EndDateUtc,
                MaxRecords = request.MaxRecords,
                MaxResponseTimeMs = request.MaxResponseTimeMs
            };

            var logData = await _tableReadService.QueryTablesAsync(logsRequest);

            // Verificar si se obtuvieron datos
            var totalRecords = logData.TableResults.Sum(t => t.RecordsReturned);
            if (totalRecords == 0)
            {
                _logger.LogWarning("No se encontraron registros para analizar en aplicación: {ApplicationName}", request.ApplicationName);
                
                return Ok(new AnalysisResponse
                {
                    ApplicationName = request.ApplicationName,
                    AnalysisTimestamp = DateTime.UtcNow,
                    TotalRecordsAnalyzed = 0,
                    AnalysisResults = new AnalysisResults(),
                    AuditLog = new AuditLog
                    {
                        AnalysisId = analysisId,
                        ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                        LlmTokensUsed = 0,
                        RulesApplied = new[] { "no-data-found" }
                    }
                });
            }

            // Paso 2: Obtener configuración de análisis
            var configuration = await _configService.GetConfigurationAsync(request.ApplicationName);

            // Paso 3: Ejecutar análisis
            AnalysisResults analysisResults;
            var llmTokensUsed = 0;
            var usedFallback = false;

            if (request.AnalysisMode?.ToLowerInvariant() == "intelligent")
            {
                // Verificar disponibilidad del LLM
                var isLlmAvailable = await _llmAnalysisService.IsServiceAvailableAsync(cancellationToken);
                
                if (isLlmAvailable)
                {
                    _logger.LogInformation("Ejecutando análisis LLM para {TotalRecords} registros", totalRecords);
                    analysisResults = await _llmAnalysisService.AnalyzeLogsAsync(logData, configuration, cancellationToken);
                    llmTokensUsed = EstimateTokensUsed(logData); // Estimación aproximada
                }
                else
                {
                    _logger.LogWarning("LLM no disponible, ejecutando análisis de fallback");
                    analysisResults = await _llmAnalysisService.AnalyzeLogsAsync(logData, configuration, cancellationToken);
                    usedFallback = true;
                }
            }
            else
            {
                // Modo básico - análisis simple basado en reglas
                analysisResults = await ExecuteBasicAnalysisAsync(logData, configuration);
            }

            stopwatch.Stop();

            // Paso 4: Construir respuesta final
            var response = new AnalysisResponse
            {
                ApplicationName = request.ApplicationName,
                AnalysisTimestamp = DateTime.UtcNow,
                TotalRecordsAnalyzed = totalRecords,
                AnalysisResults = analysisResults,
                AuditLog = new AuditLog
                {
                    AnalysisId = analysisId,
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    LlmTokensUsed = llmTokensUsed,
                    RulesApplied = GetAppliedRules(request.AnalysisMode, usedFallback),
                    UsedFallback = usedFallback
                }
            };

            // Registrar finalización exitosa
            await _auditService.LogAnalysisCompletedAsync(analysisId, response.AuditLog);

            _logger.LogInformation("Análisis completado para aplicación: {ApplicationName}, Tiempo: {ProcessingTimeMs}ms", 
                request.ApplicationName, stopwatch.ElapsedMilliseconds);

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Error durante el análisis para aplicación: {ApplicationName}", request.ApplicationName);
            
            await _auditService.LogAnalysisErrorAsync(analysisId, ex.Message, ex);

            return StatusCode(500, new
            {
                error = "Error interno durante el análisis",
                analysisId = analysisId,
                processingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    /// <summary>
    /// Verifica el estado del servicio de análisis LLM.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Estado del servicio LLM.</returns>
    /// <response code="200">Estado del servicio obtenido exitosamente.</response>
    [HttpGet("status")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetServiceStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isLlmAvailable = await _llmAnalysisService.IsServiceAvailableAsync(cancellationToken);
            
            return Ok(new
            {
                serviceStatus = "OK",
                llmServiceAvailable = isLlmAvailable,
                timestamp = DateTime.UtcNow,
                supportedModes = new[] { "basic", "intelligent" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar estado del servicio");
            
            return Ok(new
            {
                serviceStatus = "WARNING",
                llmServiceAvailable = false,
                timestamp = DateTime.UtcNow,
                supportedModes = new[] { "basic" },
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Ejecuta análisis básico basado en reglas sin usar LLM.
    /// </summary>
    private async Task<AnalysisResults> ExecuteBasicAnalysisAsync(LogsQueryResponse logData, AnalysisConfiguration configuration)
    {
        await Task.Delay(1); // Para mantener consistencia asíncrona

        var detectedIssues = new List<DetectedIssue>();
        var criticalErrors = 0;
        var nonCriticalErrors = 0;
        var performanceIssues = 0;
        var recurrentPatterns = 0;

        // Contadores por patrón para detectar recurrencia
        var errorPatterns = new Dictionary<string, int>();

        foreach (var table in logData.TableResults)
        {
            if (table.Status != "OK") continue;

            foreach (var record in table.Records)
            {
                if (record is Dictionary<string, object?> dict)
                {
                    // Analizar errores
                    if (dict.TryGetValue("Exception", out var exception) && exception != null)
                    {
                        var exceptionText = exception.ToString() ?? string.Empty;
                        
                        // Extraer patrón básico del error
                        var pattern = ExtractErrorPattern(exceptionText);
                        errorPatterns[pattern] = errorPatterns.GetValueOrDefault(pattern, 0) + 1;

                        var isCritical = configuration.ErrorPatterns.Critical.Any(p => 
                            exceptionText.Contains(p, StringComparison.OrdinalIgnoreCase));

                        if (isCritical)
                        {
                            criticalErrors++;
                        }
                        else
                        {
                            nonCriticalErrors++;
                        }
                    }

                    // Analizar rendimiento
                    if (dict.TryGetValue("TimeService", out var timeService) && timeService != null)
                    {
                        if (TimeSpan.TryParse(timeService.ToString(), out var ts))
                        {
                            var timeMs = (int)ts.TotalMilliseconds;
                            var serviceName = dict.TryGetValue("NameMethod", out var nameMethod) ? 
                                nameMethod?.ToString() ?? "Unknown" : "Unknown";
                            
                            var threshold = configuration.ResponseTimeThresholds.GetValueOrDefault(serviceName, 
                                configuration.ResponseTimeThresholds.GetValueOrDefault("default", 3000));
                            
                            if (timeMs > threshold)
                            {
                                performanceIssues++;
                            }
                        }
                    }
                }
            }
        }

        // Detectar patrones recurrentes
        foreach (var pattern in errorPatterns.Where(p => p.Value >= configuration.RecurrenceThreshold))
        {
            recurrentPatterns++;
            
            detectedIssues.Add(new DetectedIssue
            {
                IssueType = "RecurrentError",
                Severity = "Medium",
                Pattern = pattern.Key,
                Occurrences = pattern.Value,
                AffectedService = "Multiple",
                SuggestedActions = new[] 
                { 
                    "Investigar patrón recurrente", 
                    "Revisar logs detallados", 
                    "Considerar fix preventivo" 
                },
                EscalationRequired = pattern.Value > 10,
                EscalationReason = pattern.Value > 10 ? "Alto número de ocurrencias" : ""
            });
        }

        return new AnalysisResults
        {
            ErrorSummary = new ErrorSummary
            {
                CriticalErrors = criticalErrors,
                NonCriticalErrors = nonCriticalErrors,
                PerformanceIssues = performanceIssues,
                RecurrentPatterns = recurrentPatterns
            },
            DetectedIssues = detectedIssues.ToArray(),
            PerformanceAnalysis = new PerformanceAnalysis(),
            Recommendations = new Recommendations
            {
                Immediate = criticalErrors > 0 ? new[] { "Revisar errores críticos inmediatamente" } : Array.Empty<string>(),
                ShortTerm = performanceIssues > 0 ? new[] { "Optimizar servicios lentos" } : Array.Empty<string>(),
                LongTerm = recurrentPatterns > 0 ? new[] { "Analizar patrones recurrentes para mejoras" } : Array.Empty<string>()
            }
        };
    }

    /// <summary>
    /// Extrae un patrón básico de error para análisis de recurrencia.
    /// </summary>
    private string ExtractErrorPattern(string? exceptionText)
    {
        if (string.IsNullOrEmpty(exceptionText))
            return "Unknown";

        // Extraer el tipo de excepción (primera línea hasta el primer ':')
        var lines = exceptionText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0)
        {
            var firstLine = lines[0];
            var colonIndex = firstLine.IndexOf(':');
            if (colonIndex > 0)
            {
                return firstLine.Substring(0, colonIndex).Trim();
            }
            return firstLine.Trim();
        }
        
        return "UnknownError";
    }

    /// <summary>
    /// Estima aproximadamente los tokens utilizados basándose en el volumen de datos.
    /// </summary>
    private int EstimateTokensUsed(LogsQueryResponse logData)
    {
        var totalRecords = logData.TableResults.Sum(t => t.RecordsReturned);
        // Estimación aproximada: ~100 tokens por registro + overhead del prompt
        return (totalRecords * 100) + 500;
    }

    /// <summary>
    /// Obtiene la lista de reglas aplicadas durante el análisis.
    /// </summary>
    private string[] GetAppliedRules(string? analysisMode, bool usedFallback)
    {
        var rules = new List<string>();

        if (analysisMode?.ToLowerInvariant() == "intelligent")
        {
            if (usedFallback)
            {
                rules.Add("fallback-analysis");
                rules.Add("rule-based-classification");
            }
            else
            {
                rules.Add("llm-analysis");
                rules.Add("intelligent-classification");
            }
        }
        else
        {
            rules.Add("basic-analysis");
            rules.Add("rule-based-classification");
        }

        rules.Add("error-pattern-matching");
        rules.Add("performance-threshold-check");
        rules.Add("recurrence-detection");

        return rules.ToArray();
    }
}
