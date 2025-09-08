# Plan de trabajo — Parte 2: Análisis inteligente de logs con LLM

Fecha: 2025-09-06

Resumen
------
Este documento describe un plan de trabajo detallado para la segunda parte del flujo: análisis inteligente de los registros obtenidos en la Parte 1 utilizando un modelo de lenguaje (LLM) para detectar errores, clasificarlos y sugerir acciones de resolución. El servicio evolucionará de retornar registros sin procesar a proporcionar análisis inteligente y recomendaciones.

Configuración LLM validada
--------------------------
- **Endpoint**: `https://orquestador-foundry.openai.azure.com/openai/deployments/gpt-5-mini/chat/completions`
- **API Version**: `2025-03-01-preview`
- **Deployment**: `gpt-5-mini`
- **Max Tokens**: 128000
- **Estado**: ✅ Probado exitosamente

JSON de request de ejemplo (entrada desde Parte 1)
--------------------------------------------------
```json
{
  "applicationName": "AppSalud",
  "tablesToAnalyze": ["AppLog", "Log", "LogCsAuthenticate"],
  "startDateUtc": "2025-03-05T17:30:00Z",
  "endDateUtc": "2025-03-05T18:15:00Z",
  "maxRecords": 10,
  "maxResponseTimeMs": 5000,
  "analysisMode": "intelligent" // Nuevo parámetro para habilitar análisis LLM
}
```

Checklist de requisitos (extraído de Monitoreo_Proactivo.txt)
------------------------------------------------------------
- [ ] Analizar registros en busca de errores de servicios externos
- [ ] Identificar errores internos recurrentes de la aplicación
- [ ] Detectar tiempos de respuesta elevados según el tipo de servicio
- [ ] Aplicar LLM para identificar posibles acciones de resolución
- [ ] Clasificar errores como críticos o no críticos
- [ ] Documentar la lógica de análisis utilizada
- [ ] Permitir configurar umbrales personalizados por tipo de servicio
- [ ] Registrar acciones sugeridas en log de auditoría

Supuestos para la Parte 2
-------------------------
1. Los registros de entrada provienen del endpoint de la Parte 1 ya implementado.
2. El análisis LLM se ejecutará solo cuando `analysisMode` sea "intelligent".
3. El modelo LLM puede procesar JSON estructurado y retornar análisis en formato específico.
4. Los umbrales de tiempo de respuesta pueden ser configurables por aplicación/servicio.
5. Las notificaciones automáticas se implementarán en la Parte 3 (no incluidas aquí).

Contrato extendido (Inputs / Outputs / Errores)
----------------------------------------------
### Inputs
- JSON request (incluye nuevo parámetro `analysisMode`)
- Configuración de umbrales por aplicación/servicio

### Outputs (análisis inteligente)
```json
{
  "applicationName": "AppSalud",
  "analysisTimestamp": "2025-09-06T10:30:00Z",
  "totalRecordsAnalyzed": 47,
  "analysisResults": {
    "errorSummary": {
      "criticalErrors": 2,
      "nonCriticalErrors": 15,
      "performanceIssues": 5,
      "recurrentPatterns": 3
    },
    "detectedIssues": [
      {
        "issueType": "ExternalServiceError",
        "severity": "Critical",
        "pattern": "JsonReaderException",
        "occurrences": 25,
        "affectedService": "CsAuthenticate API",
        "suggestedActions": [
          "Verificar formato de respuesta del servicio externo",
          "Implementar validación de contenido antes de deserialización"
        ],
        "escalationRequired": true
      }
    ],
    "performanceAnalysis": {
      "slowServices": [
        {
          "serviceName": "ConsultarAfiliado",
          "averageResponseTime": "17776ms",
          "threshold": "5000ms",
          "recommendation": "Optimizar consulta o aumentar timeout"
        }
      ]
    },
    "recommendations": {
      "immediate": ["Validar conectividad con servicio CsAuthenticate"],
      "shortTerm": ["Implementar circuit breaker pattern"],
      "longTerm": ["Considerar servicio alternativo para consultas"]
    }
  },
  "auditLog": {
    "analysisId": "uuid-here",
    "processingTimeMs": 2500,
    "llmTokensUsed": 1847,
    "rulesApplied": ["time-threshold", "error-pattern", "recurrence-detection"]
  }
}
```

### Errores esperados
- Fallo en conexión con LLM service
- Tokens insuficientes para procesar el volumen de datos
- Configuración de umbrales inválida
- Timeout en análisis por volumen excesivo

Flujo detallado de análisis inteligente
--------------------------------------
1. **Validación inicial del request**
   - Verificar presencia de `analysisMode: "intelligent"`
   - Validar configuración de umbrales para la aplicación
   
2. **Obtención de datos** (reutilizar Parte 1)
   - Llamar al servicio existente de la Parte 1 para obtener registros
   - Aplicar filtros existentes (tipos excluidos, timeService, etc.)
   
3. **Preparación para análisis LLM**
   - Estructurar datos en formato optimizado para LLM
   - Aplicar pre-filtros para reducir ruido
   - Calcular métricas básicas (conteos, promedios, patrones)
   
4. **Construcción del prompt para LLM**
   - Incluir contexto de la aplicación y umbrales
   - Estructurar registros por categorías (errores, performance, etc.)
   - Definir formato de respuesta esperado
   
5. **Llamada al LLM**
   - Ejecutar análisis con gpt-5-mini
   - Manejar timeouts y errores de conexión
   - Registrar tokens utilizados y tiempo de procesamiento
   
6. **Post-procesamiento de resultados LLM**
   - Validar formato de respuesta
   - Enriquecer con metadatos adicionales
   - Aplicar reglas de negocio para clasificación final
   
7. **Construcción de respuesta final**
   - Combinar análisis LLM con métricas calculadas
   - Generar log de auditoría
   - Estructurar recomendaciones por prioridad

Categorías de análisis detalladas
--------------------------------
### 1. Errores de servicios externos
- **Indicadores**: JsonReaderException, timeout, HTTP errors
- **Análisis**: Frecuencia, servicios afectados, patrones temporales
- **Acciones**: Validación de conectividad, implementación de fallbacks

### 2. Errores internos recurrentes
- **Indicadores**: Mismas excepciones repetidas, patrones en stack traces
- **Análisis**: Frecuencia por período, impacto en funcionalidad
- **Acciones**: Revisión de código, mejoras en manejo de errores

### 3. Problemas de rendimiento
- **Indicadores**: TimeService > umbral configurado
- **Análisis**: Servicios más lentos, tendencias, correlaciones
- **Acciones**: Optimización, scaling, circuit breaker

### 4. Patrones emergentes
- **Indicadores**: Nuevas combinaciones de errores, picos inusuales
- **Análisis**: Detección de anomalías, correlación temporal
- **Acciones**: Investigación proactiva, monitoreo intensivo

Configuración de umbrales por aplicación
---------------------------------------
```json
{
  "AppSalud": {
    "responseTimeThresholds": {
      "ConsultarAfiliado": 5000,
      "AuthenticationService": 2000,
      "default": 3000
    },
    "errorPatterns": {
      "critical": ["JsonReaderException", "ConnectionTimeout"],
      "warning": ["ValidationError", "SlowResponse"]
    },
    "recurrenceThreshold": 3
  },
  "LinaChatbot": {
    "responseTimeThresholds": {
      "MessageProcessing": 1000,
      "default": 2000
    }
  }
}
```

Prompt engineering para LLM
--------------------------
### System Prompt
```
Eres un experto en análisis de logs de aplicaciones empresariales. Tu objetivo es:
1. Identificar patrones de errores críticos que requieren atención inmediata
2. Clasificar problemas por severidad (Critical, High, Medium, Low)
3. Sugerir acciones específicas y prácticas para resolución
4. Detectar tendencias y patrones emergentes
5. Recomendar si se requiere escalamiento

Contexto: Aplicación {applicationName} con {recordCount} registros del período {dateRange}
Umbrales configurados: {thresholds}
```

### User Prompt Structure
```
Analiza los siguientes registros de log:
{structuredLogData}

Para cada problema identificado, proporciona:
- Tipo de issue (ExternalServiceError, InternalError, PerformanceIssue, etc.)
- Severidad (Critical, High, Medium, Low)
- Patrón detectado
- Número de ocurrencias
- Servicios/métodos afectados
- Acciones sugeridas (máximo 3, específicas y accionables)
- Si requiere escalamiento (true/false con justificación)

Responde en JSON con la estructura especificada.
```

Implementación técnica propuesta
-------------------------------
### Nuevos componentes
- `ILlmAnalysisService`: Interfaz para análisis LLM
- `AzureOpenAiService`: Implementación para Azure OpenAI
- `LogAnalysisController`: Nuevo endpoint para análisis inteligente
- `AnalysisConfigurationService`: Gestión de umbrales y configuraciones
- `AuditService`: Registro de análisis y acciones

### Estructura del proyecto extendida
```
src/MonitoreoStorage.Api/
├── Controllers/
│   ├── LogsController.cs              # Existente (Parte 1)
│   └── AnalysisController.cs          # Nuevo (Parte 2)
├── Services/
│   ├── ITableReadService.cs           # Existente
│   ├── TableReadService.cs            # Existente
│   ├── ILlmAnalysisService.cs         # Nuevo
│   ├── AzureOpenAiService.cs          # Nuevo
│   ├── IAnalysisConfigService.cs      # Nuevo
│   ├── AnalysisConfigService.cs       # Nuevo
│   └── IAuditService.cs               # Nuevo
├── Models/
│   ├── [modelos existentes]           # Parte 1
│   ├── AnalysisRequest.cs             # Nuevo
│   ├── AnalysisResponse.cs            # Nuevo
│   ├── DetectedIssue.cs               # Nuevo
│   └── AnalysisConfiguration.cs       # Nuevo
└── Configuration/
    └── analysis-thresholds.json       # Configuración de umbrales
```

Casos de borde y validaciones adicionales
----------------------------------------
- **Volumen excesivo**: Implementar chunking para análisis de grandes volúmenes
- **Tokens insuficientes**: Priorizar errores críticos y resumir información
- **LLM no disponible**: Fallback a análisis basado en reglas predefinidas
- **Respuesta LLM malformada**: Validación y re-procesamiento
- **Umbrales no configurados**: Usar valores por defecto documentados

Observaciones de seguridad para LLM
----------------------------------
- Nunca enviar datos sensibles (passwords, tokens, PII) al LLM
- Filtrar y anonymizar información antes del análisis
- Registrar solo metadatos de las llamadas LLM (tokens, tiempo, no contenido)
- Usar variables de entorno para API keys del LLM
- Implementar rate limiting para llamadas LLM

Pruebas y verificación de la Parte 2
-----------------------------------
### Pruebas unitarias
- Construcción correcta de prompts
- Validación de respuestas LLM
- Lógica de clasificación de severidad
- Configuración de umbrales

### Pruebas de integración
- Flujo completo: datos → LLM → análisis → respuesta
- Manejo de errores del servicio LLM
- Fallback a análisis basado en reglas
- Validación con datos reales de AppSalud

### Pruebas de performance
- Tiempo de análisis vs volumen de datos
- Uso de tokens LLM vs tamaño de entrada
- Memory usage durante procesamiento
- Concurrencia de análisis múltiples

Plan de trabajo y estimación (entregables por hito)
---------------------------------------------------
1. **Diseño de arquitectura y contratos** — 1 día
   - Definir interfaces y modelos
   - Estructura de configuración de umbrales
   - Validar integración con Parte 1

2. **Implementación del servicio LLM** — 2 días
   - Azure OpenAI service integration
   - Prompt engineering y validación
   - Manejo de errores y timeouts

3. **Lógica de análisis y clasificación** — 1.5 días
   - Reglas de negocio para severidad
   - Configuración de umbrales por aplicación
   - Fallback a análisis basado en reglas

4. **Nuevo endpoint y integración** — 1 día
   - Controller para análisis inteligente
   - Integración con servicio de Parte 1
   - Validación de request/response

5. **Servicio de auditoría y logging** — 0.5 días
   - Registro de análisis ejecutados
   - Métricas de uso de LLM
   - Logs de auditoría estructurados

6. **Testing integral y ajustes** — 1.5 días
   - Pruebas con datos reales
   - Validación de clasificaciones
   - Optimización de prompts

7. **Documentación y handoff** — 0.5 días
   - Actualización de documentación
   - Ejemplos de configuración
   - Guía de troubleshooting

**Total estimado: 8 días hábiles**

Riesgos y mitigaciones
----------------------
- **Costos LLM elevados**: Implementar caching y optimización de prompts
- **Latencia alta**: Análisis asíncrono para volúmenes grandes
- **Calidad del análisis**: Validación humana en fase inicial y ajuste continuo
- **Dependencia externa**: Fallback a reglas predefinidas

Métricas y KPIs para evaluar éxito
---------------------------------
- **Precisión**: % de errores críticos correctamente identificados
- **Cobertura**: % de problemas reales detectados vs total
- **Tiempo de respuesta**: Latencia promedio del análisis
- **Costo-efectividad**: Tokens LLM usados vs valor del análisis
- **Adopción**: Uso del modo inteligente vs modo básico

Configuración de entorno para Parte 2
------------------------------------
### Variables de entorno adicionales
```
AZURE_OPENAI_ENDPOINT=https://orquestador-foundry.openai.azure.com
AZURE_OPENAI_API_KEY=[SECRET_FROM_KEYVAULT]
AZURE_OPENAI_DEPLOYMENT=gpt-5-mini
AZURE_OPENAI_API_VERSION=2025-03-01-preview
ANALYSIS_CONFIG_PATH=./Configuration/analysis-thresholds.json
```

### Dependencias NuGet adicionales
```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.0" />
```

Siguientes pasos propuestos
--------------------------
- Confirmar configuración de Azure OpenAI y acceso a gpt-5-mini
- Revisar y aprobar estructura de umbrales de configuración
- Validar ejemplos de respuesta del LLM con datos reales
- Definir estrategia de fallback cuando LLM no esté disponible
- Tras aprobación, implementar según hitos definidos

Integración con Parte 3 (Notificaciones)
----------------------------------------
El análisis de esta parte proporcionará los datos estructurados necesarios para:
- Determinar cuándo enviar notificaciones automáticas
- Seleccionar el canal apropiado según severidad
- Incluir recomendaciones específicas en las alertas
- Mantener historial para trending y reportes

---

**Estado**: Pendiente de aprobación
**Fecha de creación**: 2025-09-06
**Dependencias**: Parte 1 completada ✅
**Siguientes pasos**: Implementación según hitos definidos tras aprobación
