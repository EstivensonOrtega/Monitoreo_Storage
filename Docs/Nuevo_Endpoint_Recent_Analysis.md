# Nuevo Endpoint: GET /api/Analysis/recent

## 📋 **Descripción General**

Se ha implementado exitosamente el nuevo endpoint **GET /api/Analysis/recent** que permite consultar y analizar logs recientes sin necesidad de especificar fechas exactas. El usuario solo necesita indicar cuántos minutos hacia atrás desea analizar desde el momento actual.

## 🎯 **Características Implementadas**

### **Ruta del Endpoint**
```
GET /api/Analysis/recent
```

### **Parámetros de Query**

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `applicationName` | string | ✅ Sí | - | Nombre de la aplicación (ej: "LinaChatbot") |
| `tablesToAnalyze` | string | ✅ Sí | - | Tablas separadas por coma (ej: "LinaLog,LogLinaMobile,LoggerMiddleware") |
| `minutesBack` | int | ❌ No | 30 | Minutos hacia atrás desde ahora |
| `maxRecords` | int | ❌ No | 10 | Máximo registros por tabla |
| `maxResponseTimeMs` | int? | ❌ No | null | Filtro de tiempo de respuesta en ms |

### **Ejemplos de Uso**

#### **1. Consulta Básica - Últimos 30 minutos**
```http
GET /api/Analysis/recent?applicationName=LinaChatbot&tablesToAnalyze=LinaLog,LogLinaMobile
```

#### **2. Última Hora con Más Registros**
```http
GET /api/Analysis/recent?applicationName=LinaChatbot&tablesToAnalyze=LinaLog,LogLinaMobile&minutesBack=60&maxRecords=20
```

#### **3. Con Filtro de Rendimiento**
```http
GET /api/Analysis/recent?applicationName=LinaChatbot&tablesToAnalyze=LinaLog&minutesBack=60&maxRecords=15&maxResponseTimeMs=5000
```

## 🔧 **Comportamiento Técnico**

### **Cálculo Automático de Fechas**
- **Fecha Fin**: `DateTime.UtcNow` (momento actual)
- **Fecha Inicio**: `DateTime.UtcNow.AddMinutes(-minutesBack)`
- **Ejemplo**: Si `minutesBack=60`, analizará desde hace 1 hora hasta ahora

### **Modo de Análisis**
- **Siempre Inteligente**: El endpoint usa automáticamente `analysisMode = "intelligent"`
- **Análisis LLM**: Utiliza Azure OpenAI para detección avanzada de patrones
- **Fallback**: Si LLM no está disponible, usa análisis basado en reglas

### **Validaciones Implementadas**
- ✅ `applicationName` no puede estar vacío
- ✅ `tablesToAnalyze` debe contener al menos una tabla
- ✅ `minutesBack` debe ser mayor a 0
- ✅ `maxRecords` debe ser mayor a 0
- ✅ Parsing automático de tablas separadas por comas

## 📊 **Respuesta del Endpoint**

### **Estructura de Respuesta**
```json
{
  "applicationName": "LinaChatbot",
  "analysisTimestamp": "2025-09-08T10:30:00Z",
  "totalRecordsAnalyzed": 45,
  "analysisResults": {
    "errorSummary": {
      "criticalErrors": 2,
      "nonCriticalErrors": 3,
      "performanceIssues": 1,
      "recurrentPatterns": 2
    },
    "detectedIssues": [
      {
        "issueType": "Error de Conexión a Base de Datos",
        "severity": "Critical",
        "pattern": "Timeouts de conexión recurrentes",
        "occurrences": 5,
        "affectedService": "ServicioUsuarios",
        "suggestedActions": [
          "Revisar pool de conexiones de base de datos",
          "Verificar latencia de red hacia el servidor"
        ],
        "escalationRequired": true,
        "escalationReason": "Errores críticos afectan funcionalidad principal"
      }
    ],
    "performanceAnalysis": {
      "slowServices": [
        {
          "serviceName": "ConsultaUsuarios",
          "averageResponseTime": "3500ms",
          "threshold": "3000ms",
          "recommendation": "Optimizar consultas de base de datos"
        }
      ]
    },
    "recommendations": {
      "immediate": ["Verificar conectividad de base de datos"],
      "shortTerm": ["Implementar circuit breaker"],
      "longTerm": ["Considerar cache distribuido"]
    }
  },
  "auditLog": {
    "analysisId": "abc123-def456-ghi789",
    "processingTimeMs": 1250,
    "llmTokensUsed": 1500,
    "rulesApplied": [
      "recent-analysis-30min",
      "llm-analysis",
      "intelligent-classification",
      "error-pattern-matching",
      "performance-threshold-check",
      "recurrence-detection",
      "time-window-filtering"
    ],
    "usedFallback": false
  }
}
```

### **Caso Sin Datos**
```json
{
  "applicationName": "LinaChatbot",
  "analysisTimestamp": "2025-09-08T10:30:00Z",
  "totalRecordsAnalyzed": 0,
  "analysisResults": {
    "errorSummary": {},
    "detectedIssues": [],
    "performanceAnalysis": {
      "slowServices": []
    },
    "recommendations": {
      "immediate": ["No se encontraron registros en los últimos 30 minutos"]
    }
  },
  "auditLog": {
    "analysisId": "abc123-def456-ghi789",
    "processingTimeMs": 150,
    "llmTokensUsed": 0,
    "rulesApplied": ["recent-analysis-30min", "no-data-found"],
    "usedFallback": false
  }
}
```

## 🚨 **Códigos de Respuesta**

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **200** | Análisis completado exitosamente | Datos analizados correctamente |
| **400** | Parámetros inválidos | Falta `applicationName` |
| **500** | Error interno del servidor | Error de conectividad LLM |

### **Ejemplos de Errores 400**
```json
// Falta applicationName
{
  "error": "El parámetro 'applicationName' es requerido"
}

// minutesBack inválido
{
  "error": "El parámetro 'minutesBack' debe ser mayor a 0"
}

// tablesToAnalyze vacío
{
  "error": "Debe especificar al menos una tabla en 'tablesToAnalyze'"
}
```

## 🔄 **Diferencias con /api/Analysis/analyze**

| Aspecto | `/analyze` (POST) | `/recent` (GET) |
|---------|-------------------|-----------------|
| **Método HTTP** | POST | GET |
| **Fechas** | Debe especificar `startDateUtc` y `endDateUtc` | Solo especifica `minutesBack` |
| **Modo Análisis** | Configurable (`basic` o `intelligent`) | Siempre `intelligent` |
| **Parámetros** | JSON en body | Query parameters |
| **Uso Típico** | Análisis histórico de períodos específicos | Monitoreo en tiempo real |

## 🧪 **Casos de Uso Recomendados**

### **1. Monitoreo en Tiempo Real**
```http
GET /api/Analysis/recent?applicationName=LinaChatbot&tablesToAnalyze=LinaLog&minutesBack=5
```
*Para alertas y monitoreo continuo*

### **2. Análisis de Última Hora**
```http
GET /api/Analysis/recent?applicationName=ECommerce&tablesToAnalyze=Orders,Payments&minutesBack=60&maxRecords=50
```
*Para revisiones periódicas*

### **3. Análisis de Turno de Trabajo**
```http
GET /api/Analysis/recent?applicationName=CRM&tablesToAnalyze=UserActions,Errors&minutesBack=480&maxRecords=100
```
*Para revisión de 8 horas de trabajo*

### **4. Detección de Problemas de Rendimiento**
```http
GET /api/Analysis/recent?applicationName=WebApp&tablesToAnalyze=Requests&minutesBack=30&maxResponseTimeMs=3000
```
*Para identificar requests lentos recientes*

## 📈 **Ventajas del Nuevo Endpoint**

### **Para Desarrolladores**
- ✅ **Simplicidad**: No necesita calcular fechas
- ✅ **Monitoreo Rápido**: Consulta directa de logs recientes
- ✅ **Integración Fácil**: Compatible con herramientas de monitoreo

### **Para DevOps**
- ✅ **Alertas Automáticas**: Perfecto para sistemas de alerting
- ✅ **Dashboards en Tiempo Real**: Actualización continua
- ✅ **Diagnóstico Rápido**: Análisis inmediato de problemas

### **Para Operaciones**
- ✅ **Monitoreo Proactivo**: Detecta problemas antes que escalen
- ✅ **Respuesta Rápida**: Información inmediata del estado
- ✅ **Análisis Contextual**: LLM entiende patrones recientes

## 🛠️ **Implementación Técnica**

### **Modelo Creado**
- ✅ `RecentAnalysisRequest.cs` - Modelo para conversión automática a `AnalysisRequest`
- ✅ Método `ToAnalysisRequest()` para compatibilidad con la lógica existente

### **Controlador Actualizado**
- ✅ Método `AnalyzeRecentLogsAsync()` en `AnalysisController`
- ✅ Validaciones completas de parámetros
- ✅ Logging específico para análisis recientes
- ✅ Sobrecarga de `GetAppliedRules()` para incluir información temporal

### **Auditoría Mejorada**
- ✅ Reglas específicas: `recent-analysis-{minutesBack}min`
- ✅ Información de ventana temporal en los logs
- ✅ Tracking diferenciado para análisis recientes

---

## ✅ **Estado: Completamente Implementado y Listo para Usar**

El endpoint está funcionando correctamente y puede probarse usando el archivo `test_recent_analysis.http` con múltiples escenarios de prueba.
