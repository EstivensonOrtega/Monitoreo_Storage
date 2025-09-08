using Azure;
using MonitoreoStorage.Api.Models;
using System.Text.Json;
using System.Text;

namespace MonitoreoStorage.Api.Services;

/// <summary>
/// Implementación del servicio de análisis LLM usando Azure OpenAI vía HTTP.
/// </summary>
public class AzureOpenAiService : ILlmAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAiService> _logger;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    private readonly string _apiVersion;

    /// <summary>
    /// Crea una nueva instancia del servicio Azure OpenAI.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP para realizar llamadas REST.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="logger">Logger para registrar eventos.</param>
    public AzureOpenAiService(HttpClient httpClient, IConfiguration configuration, ILogger<AzureOpenAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _endpoint = _configuration["AZURE_OPENAI_ENDPOINT"] ?? 
                   throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT no está configurado");
        
        _apiKey = _configuration["AZURE_OPENAI_API_KEY"] ?? 
                 throw new InvalidOperationException("AZURE_OPENAI_API_KEY no está configurado");
        
        _deploymentName = _configuration["AZURE_OPENAI_DEPLOYMENT"] ?? "gpt-5-mini";
        _apiVersion = _configuration["AZURE_OPENAI_API_VERSION"] ?? "2025-03-01-preview";
    }

    /// <summary>
    /// Analiza los datos de logs utilizando Azure OpenAI para detectar errores y sugerir acciones.
    /// </summary>
    public async Task<AnalysisResults> AnalyzeLogsAsync(LogsQueryResponse logData, AnalysisConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando análisis LLM para aplicación: {ApplicationName}", logData.ApplicationName);

            var prompt = BuildAnalysisPrompt(logData, configuration);
            var response = await CallOpenAIAsync(prompt, cancellationToken);
            
            var analysisResults = ParseLLMResponse(response);
            
            _logger.LogInformation("Análisis LLM completado para aplicación: {ApplicationName}", logData.ApplicationName);
            
            return analysisResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el análisis LLM para aplicación: {ApplicationName}", logData.ApplicationName);
            
            // Fallback a análisis basado en reglas
            return FallbackAnalysis(logData, configuration);
        }
    }

    /// <summary>
    /// Verifica si el servicio Azure OpenAI está disponible.
    /// </summary>
    public async Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = "Eres un asistente útil." },
                    new { role = "user", content = "Responde solo 'OK'" }
                },
                temperature = 1,
                max_completion_tokens = 128000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
            
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("api-key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Servicio Azure OpenAI no está disponible");
            return false;
        }
    }

    /// <summary>
    /// Construye el prompt para el análisis LLM.
    /// </summary>
    private string BuildAnalysisPrompt(LogsQueryResponse logData, AnalysisConfiguration configuration)
    {
        var totalRecords = logData.TableResults.Sum(t => t.RecordsReturned);
        var dateRange = $"{logData.TableResults.FirstOrDefault()?.StartDateUtc:yyyy-MM-dd} a {logData.TableResults.FirstOrDefault()?.EndDateUtc:yyyy-MM-dd}";

        var structuredData = PrepareStructuredData(logData);

        var prompt = $@"Eres un experto en análisis de logs de aplicaciones empresariales. RESPONDE SIEMPRE EN ESPAÑOL.

Tu objetivo es:
1. Identificar patrones de errores críticos que requieren atención inmediata
2. Clasificar problemas por severidad (Critical, High, Medium, Low)
3. Sugerir acciones específicas y prácticas para resolución EN ESPAÑOL
4. Detectar tendencias y patrones emergentes
5. Recomendar si se requiere escalamiento

Contexto: Aplicación {logData.ApplicationName} con {totalRecords} registros del período {dateRange}
Umbrales configurados: {JsonSerializer.Serialize(configuration.ResponseTimeThresholds)}

Analiza los siguientes registros de log:
{structuredData}

Para cada problema identificado, proporciona:
- Tipo de issue (ErrorServicioExterno, ErrorInterno, ProblemaRendimiento, etc.) EN ESPAÑOL
- Severidad (Critical, High, Medium, Low)
- Patrón detectado EN ESPAÑOL
- Número de ocurrencias
- Servicios/métodos afectados
- Acciones sugeridas EN ESPAÑOL (máximo 3, específicas y accionables)
- Si requiere escalamiento (true/false con justificación EN ESPAÑOL)

IMPORTANTE: Todas las descripciones, recomendaciones y textos deben estar EN ESPAÑOL.

Responde SOLO en JSON válido con esta estructura exacta (todos los textos EN ESPAÑOL):
{{
  ""errorSummary"": {{
    ""criticalErrors"": 0,
    ""nonCriticalErrors"": 0,
    ""performanceIssues"": 0,
    ""recurrentPatterns"": 0
  }},
  ""detectedIssues"": [
    {{
      ""issueType"": ""Descripción en español del tipo de problema"",
      ""severity"": ""Critical|High|Medium|Low"",
      ""pattern"": ""Descripción en español del patrón detectado"",
      ""occurrences"": 0,
      ""affectedService"": ""Nombre del servicio afectado"",
      ""suggestedActions"": [""Acción 1 en español"", ""Acción 2 en español""],
      ""escalationRequired"": true/false,
      ""escalationReason"": ""Razón en español si requiere escalamiento""
    }}
  ],
  ""performanceAnalysis"": {{
    ""slowServices"": [
      {{
        ""serviceName"": ""Nombre del servicio"",
        ""averageResponseTime"": ""Tiempo promedio"",
        ""threshold"": ""Umbral configurado"",
        ""recommendation"": ""Recomendación en español""
      }}
    ]
  }},
  ""recommendations"": {{
    ""immediate"": [""Recomendación inmediata en español""],
    ""shortTerm"": [""Recomendación a corto plazo en español""],
    ""longTerm"": [""Recomendación a largo plazo en español""]
  }}
}}";

        return prompt;
    }

    /// <summary>
    /// Prepara los datos en formato estructurado para el LLM.
    /// </summary>
    private string PrepareStructuredData(LogsQueryResponse logData)
    {
        var sb = new StringBuilder();
        
        foreach (var table in logData.TableResults)
        {
            if (table.Status != "OK" || table.Records.Length == 0) continue;

            sb.AppendLine($"Tabla: {table.TableName} ({table.RecordsReturned} registros)");
            
            // Agrupar por tipo de registro para análisis más eficiente
            var errorRecords = new List<object>();
            var performanceRecords = new List<object>();

            foreach (var record in table.Records)
            {
                if (record is Dictionary<string, object?> dict)
                {
                    if (dict.ContainsKey("Exception") && dict["Exception"] != null)
                    {
                        errorRecords.Add(record);
                    }
                    else if (dict.ContainsKey("TimeService") && dict["TimeService"] != null)
                    {
                        performanceRecords.Add(record);
                    }
                }
            }

            if (errorRecords.Count > 0)
            {
                sb.AppendLine($"Errores encontrados ({errorRecords.Count}):");
                foreach (var error in errorRecords)
                {
                    sb.AppendLine(JsonSerializer.Serialize(error));
                }
            }

            if (performanceRecords.Count > 0)
            {
                sb.AppendLine($"Registros de rendimiento ({performanceRecords.Count}):");
                foreach (var perf in performanceRecords)
                {
                    sb.AppendLine(JsonSerializer.Serialize(perf));
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Realiza la llamada REST a Azure OpenAI.
    /// </summary>
    private async Task<string> CallOpenAIAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = BuildSystemPrompt() },
                new { role = "user", content = prompt }
            },
            temperature = 1,
            max_completion_tokens = 128000
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
        
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Add("api-key", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Error al llamar Azure OpenAI: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        return responseObj.GetProperty("choices")[0]
                         .GetProperty("message")
                         .GetProperty("content").GetString() ?? string.Empty;
    }

    /// <summary>
    /// Construye el prompt del sistema para el análisis de logs.
    /// </summary>
    private string BuildSystemPrompt()
    {
        return @"Eres un experto en análisis de logs de aplicaciones empresariales. DEBES RESPONDER SIEMPRE EN ESPAÑOL.

Tu tarea es analizar datos de logs y:
1. Detectar errores y problemas en las aplicaciones
2. Clasificar los problemas como críticos o no críticos
3. Sugerir acciones específicas de resolución
4. Proporcionar un resumen claro del estado de la aplicación

IMPORTANTE: Todas tus respuestas, descripciones, recomendaciones y textos deben estar EN ESPAÑOL.

Responde siempre en formato JSON válido con la estructura exacta que se te proporcione. Todos los valores de texto deben estar en español.";
    }

    /// <summary>
    /// Parsea la respuesta del LLM al formato estructurado.
    /// </summary>
    private AnalysisResults ParseLLMResponse(string llmResponse)
    {
        try
        {
            // Limpiar la respuesta en caso de que tenga markdown o texto adicional
            var jsonStart = llmResponse.IndexOf('{');
            var jsonEnd = llmResponse.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<AnalysisResults>(jsonContent, options) ?? new AnalysisResults();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear respuesta del LLM: {Response}", llmResponse);
        }

        // Fallback si no se puede parsear
        return new AnalysisResults
        {
            ErrorSummary = new ErrorSummary(),
            DetectedIssues = Array.Empty<DetectedIssue>(),
            PerformanceAnalysis = new PerformanceAnalysis(),
            Recommendations = new Recommendations
            {
                Immediate = new[] { 
                    "Error en el análisis automático - Se requiere revisión manual de los logs",
                    "Verificar la conectividad y configuración del servicio de análisis inteligente"
                }
            }
        };
    }

    /// <summary>
    /// Análisis de fallback basado en reglas cuando el LLM no está disponible.
    /// </summary>
    private AnalysisResults FallbackAnalysis(LogsQueryResponse logData, AnalysisConfiguration configuration)
    {
        _logger.LogInformation("Ejecutando análisis de fallback basado en reglas");

        var detectedIssues = new List<DetectedIssue>();
        var criticalErrors = 0;
        var nonCriticalErrors = 0;
        var performanceIssues = 0;

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
                        var isCritical = configuration.ErrorPatterns.Critical.Any(pattern => 
                            exceptionText.Contains(pattern, StringComparison.OrdinalIgnoreCase));

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
                            var threshold = configuration.ResponseTimeThresholds.GetValueOrDefault("default", 3000);
                            
                            if (timeMs > threshold)
                            {
                                performanceIssues++;
                            }
                        }
                    }
                }
            }
        }

        // Crear issues básicos basados en reglas
        if (criticalErrors > 0)
        {
            detectedIssues.Add(new DetectedIssue
            {
                IssueType = "Error Crítico",
                Severity = "Critical",
                Pattern = "Errores críticos detectados en la aplicación",
                Occurrences = criticalErrors,
                AffectedService = "Múltiples servicios",
                SuggestedActions = new[] { 
                    "Revisar logs inmediatamente para identificar la causa raíz", 
                    "Verificar el estado de los servicios críticos",
                    "Implementar monitoreo adicional en los servicios afectados"
                },
                EscalationRequired = criticalErrors > 5,
                EscalationReason = criticalErrors > 5 ? "Alto número de errores críticos requiere atención inmediata del equipo de soporte" : ""
            });
        }

        if (performanceIssues > 0)
        {
            detectedIssues.Add(new DetectedIssue
            {
                IssueType = "Problema de Rendimiento",
                Severity = "Medium",
                Pattern = "Tiempos de respuesta elevados detectados",
                Occurrences = performanceIssues,
                AffectedService = "Servicios con latencia alta",
                SuggestedActions = new[] { 
                    "Optimizar consultas y procesos lentos", 
                    "Revisar la configuración de timeouts",
                    "Considerar escalado de recursos si es necesario"
                },
                EscalationRequired = performanceIssues > 10,
                EscalationReason = performanceIssues > 10 ? "Múltiples problemas de rendimiento pueden afectar la experiencia del usuario" : ""
            });
        }

        return new AnalysisResults
        {
            ErrorSummary = new ErrorSummary
            {
                CriticalErrors = criticalErrors,
                NonCriticalErrors = nonCriticalErrors,
                PerformanceIssues = performanceIssues,
                RecurrentPatterns = 0
            },
            DetectedIssues = detectedIssues.ToArray(),
            PerformanceAnalysis = new PerformanceAnalysis(),
            Recommendations = new Recommendations
            {
                Immediate = new[] { 
                    "Análisis de fallback ejecutado - Verificar disponibilidad del servicio LLM",
                    "Revisar manualmente los logs para problemas críticos",
                    "Confirmar que todos los servicios esenciales están operativos"
                },
                ShortTerm = new[] { 
                    "Restaurar la conectividad con el servicio de análisis inteligente",
                    "Implementar alertas adicionales para monitoreo proactivo"
                },
                LongTerm = new[] { 
                    "Evaluar la redundancia del sistema de análisis",
                    "Considerar implementar análisis predictivo"
                }
            }
        };
    }
}
