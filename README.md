# Sistema de Monitoreo de Storage - Análisis Inteligente

Sistema de monitoreo proactivo para aplicaciones con capacidades de análisis inteligente usando Azure OpenAI.

## Características

### Parte 1: Consulta Básica de Logs
- Consulta de logs desde Azure Table Storage
- Filtrado por aplicación y rango de fechas
- Endpoints REST para consultas básicas

### Parte 2: Análisis Inteligente ✨ **NUEVO**
- Análisis inteligente de logs usando Azure OpenAI (GPT-5 Mini)
- Detección automática de errores críticos y no críticos
- Sugerencias específicas de resolución
- Clasificación de problemas por categoría
- Análisis de rendimiento y patrones
- Auditoría completa de análisis

## Configuración

### Prerrequisitos
- .NET 9.0
- Azure Storage Account
- Azure OpenAI Service (GPT-5 Mini)

### Variables de Entorno

```bash
# Azure Storage
AzureStorageAccount__ConnectionString="DefaultEndpointsProtocol=https;AccountName=..."

# Azure OpenAI
AZURE_OPENAI_ENDPOINT="https://orquestador-foundry.openai.azure.com"
AZURE_OPENAI_API_KEY="your-api-key"
AZURE_OPENAI_DEPLOYMENT="gpt-5-mini"
AZURE_OPENAI_API_VERSION="2024-02-15-preview"
```

### Configuración Rápida

Ejecute el script de configuración:

```powershell
.\Scripts\setup-dev-environment.ps1
```

## Uso

### Iniciar la aplicación

```bash
cd src/MonitoreoStorage.Api
dotnet run
```

La API estará disponible en: https://localhost:49878

### Swagger UI

Acceda a la documentación interactiva: https://localhost:49878/swagger

## Endpoints

### Análisis Inteligente

#### Status del Servicio
```http
GET /api/analysis/status
```

#### Análisis de Logs
```http
POST /api/analysis/analyze
Content-Type: application/json

{
  "applicationName": "MiApp",
  "tablesToAnalyze": ["AppEvents", "AppExceptions"],
  "startTime": "2024-01-01T00:00:00Z",
  "endTime": "2024-01-01T23:59:59Z",
  "analysisMode": "intelligent"  // "basic" o "intelligent"
}
```

### Respuesta del Análisis Inteligente

```json
{
  "applicationName": "MiApp",
  "analysisMode": "intelligent",
  "analysisResults": {
    "errorCount": 5,
    "criticalErrorCount": 2,
    "warningCount": 3,
    "summary": "Se detectaron 2 errores críticos relacionados con base de datos",
    "detectedIssues": [
      {
        "category": "Database",
        "severity": "Critical",
        "description": "Múltiples timeouts de conexión a base de datos",
        "suggestedAction": "Revisar pool de conexiones y configuración de timeouts",
        "firstOccurrence": "2024-01-01T10:30:00Z",
        "frequency": 15
      }
    ],
    "recommendations": [
      "Implementar circuit breaker para conexiones de base de datos",
      "Monitorear métricas de rendimiento de base de datos"
    ]
  },
  "performanceAnalysis": {
    "avgResponseTime": 250.5,
    "slowQueries": 3,
    "peakUsageHours": ["10:00", "14:00", "18:00"]
  },
  "auditLog": {
    "analysisId": "unique-guid",
    "timestamp": "2024-01-01T12:00:00Z",
    "llmModel": "gpt-5-mini",
    "processingTimeMs": 1500
  }
}
```

## Configuración de Análisis

El sistema usa configuración basada en JSON para personalizar umbrales y patrones:

```json
{
  "applications": {
    "MiApp": {
      "errorThresholds": {
        "critical": 10,
        "warning": 5
      },
      "criticalPatterns": [
        "OutOfMemoryException",
        "SqlException",
        "timeout"
      ]
    }
  }
}
```

## Arquitectura

### Servicios

- **ILlmAnalysisService**: Análisis inteligente con Azure OpenAI
- **IAnalysisConfigService**: Configuración de umbrales y patrones
- **IAuditService**: Auditoría y logging de análisis
- **ITableReadService**: Consulta de datos de Azure Storage

### Modelos

- **AnalysisRequest**: Solicitud de análisis
- **AnalysisResponse**: Respuesta estructurada con resultados
- **DetectedIssue**: Problema detectado con severidad y recomendaciones
- **AnalysisConfiguration**: Configuración por aplicación

## Pruebas

Use el archivo `test_intelligent_analysis.http` para probar los endpoints:

```bash
# Instalar extensión REST Client en VS Code
# Abrir test_intelligent_analysis.http
# Hacer clic en "Send Request" en cada endpoint
```

## Monitoreo y Logging

El sistema registra:
- Todos los análisis realizados
- Tiempo de procesamiento del LLM
- Errores y fallbacks
- Configuración aplicada

Los logs se pueden consultar en la consola de la aplicación y Azure Application Insights (si está configurado).

## Contribuir

1. Fork el proyecto
2. Cree una rama para su característica (`git checkout -b feature/AmazingFeature`)
3. Commit sus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abra un Pull Request

## Licencia

Este proyecto está bajo la Licencia MIT - vea el archivo [LICENSE](LICENSE) para detalles.
