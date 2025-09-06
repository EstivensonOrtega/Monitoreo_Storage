# Plan de trabajo — Parte 1: Obtención de registros desde tablas de Azure Storage

Fecha: 2025-09-04

Resumen
------
Este documento describe un plan de trabajo detallado para la primera parte del flujo: consultar y obtener registros desde tablas alojadas en Azure Storage según el JSON de request proporcionado. No se realiza implementación en este punto; el objetivo es acordar el comportamiento, contract, pasos, validaciones y consideraciones de seguridad.

JSON de request de ejemplo
-------------------------
{
  "applicationName": "AppSalud",
  "tablesToAnalyze": ["AppLog", "Log", "LogCsAuthenticate"],
  "startDateUtc": "2025-03-05T17:30:00Z",
  "endDateUtc": "2025-03-05T18:15:00Z",
  "maxRecords": 10
}

Checklist de requisitos (extraído del pedido)
---------------------------------------------
- [ ] Usar `applicationName` para identificar cadena de conexión.
- [ ] Iterar las tablas listadas en `tablesToAnalyze`.
- [ ] Consultar registros por `Timestamp` entre `startDateUtc` y `endDateUtc`.
- [ ] Respetar `maxRecords` al retornar registros.
- [ ] Escribir un plan en Markdown dentro de `Docs/` (archivo creado aquí).

Supuestos razonables (si falta precisión)
-----------------------------------------
1. `maxRecords` se aplicará por tabla (límite por tabla). Alternativa plausible: aplicar `maxRecords` como límite global total; esto debe confirmarse en la siguiente fase.
  - Aclaración: al obtener los registros, siempre se truncarán los resultados devueltos a `maxRecords` por tabla. Por ejemplo, si la consulta encuentra 100 registros y `maxRecords` tiene el valor 10, la respuesta incluirá únicamente 10 registros para esa tabla (los 10 primeros según orden de consulta/paginación).
2. Las tablas referidas son Azure Table Storage (modelo OData) o Azure Storage Tables compatibles.
3. `applicationName` tiene una correspondencia preexistente con una o varias cadenas de conexión almacenadas de forma segura (Key Vault, variables de entorno o fichero configurado fuera del repositorio).

Contrato mínimo (Inputs / Outputs / Errores)
-------------------------------------------
- Inputs
  - JSON request (estructura arriba).
- Outputs (por tabla)
  - tableName: string
  - requestedRange: { startDateUtc, endDateUtc }
  - recordsReturned: integer
  - records: [ ... ] (hasta `maxRecords`)
  - status: OK | ERROR
  - errorMessage: string (si aplica)
- Errores esperados
  - Cadena de conexión no encontrada para applicationName
  - Tabla inexistente o permisos insuficientes
  - Fechas inválidas o rango vacío
  - Límite de tasa / timeouts de Azure

Flujo detallado (pasos operativos)
----------------------------------
1. Validación inicial del request
   - Verificar presencia y formato de `applicationName`, `tablesToAnalyze`, `startDateUtc`, `endDateUtc`, `maxRecords`.
   - Parsear `startDateUtc` y `endDateUtc` como UTC. Rechazar si start > end.
2. Resolución de cadena de conexión
   - Mapear `applicationName` a la cadena de conexión (no almacenar en repo).
   - Verificar que la cadena de conexión esté accesible (KeyVault / env var).
3. Conexión al servicio de tablas (per app/namespace según la cadena)
4. Para cada `tableName` en `tablesToAnalyze`:
   a. Validar que la tabla exista o manejar 404/no encontrada.
   b. Construir filtro OData para `Timestamp`:
      - Ejemplo: `Timestamp ge datetime'2025-03-05T17:30:00Z' and Timestamp le datetime'2025-03-05T18:15:00Z'`
   c. Ejecutar consulta con paginación si es necesario.
  d. Acumular hasta `maxRecords` (por tabla, según supuesto) y detener la consulta cuando se alcanza el límite. Ejemplo: si la consulta coincide con 100 entidades y `maxRecords` = 10, devolveremos 10 entidades para esa tabla.
   e. Normalizar resultados (convertir Timestamp a ISO-8601 UTC, incluir PartitionKey/RowKey si relevantes).
5. Construir y retornar el objeto de respuesta por tabla con conteo `recordsReturned` = número de items devueltos (no el total posible en la tabla si se truncó).

Formato de respuesta sugerido (ejemplo)
--------------------------------------
{
  "applicationName": "AppSalud",
  "tableResults": [
    {
      "tableName": "AppLog",
      "requestedRange": {"startDateUtc":"...","endDateUtc":"..."},
      "recordsReturned": 10,
      "records": [ /* registros */ ],
      "status":"OK"
    },
    {
      "tableName": "NoExiste",
      "requestedRange": {...},
      "recordsReturned": 0,
      "records": [],
      "status":"ERROR",
      "errorMessage":"Table not found"
    }
  ]
}

Ejemplos de query / filtros (Azure Table Storage - OData)
---------------------------------------------------------
- OData filter sobre Timestamp:
  Timestamp ge datetime'2025-03-05T17:30:00Z' and Timestamp le datetime'2025-03-05T18:15:00Z'
- Si se necesita filtrar por PartitionKey, añadir `and PartitionKey eq '...'`.

Pseudocódigo (alto nivel)
-------------------------
- recibir request
- validar request
- connStr = resolverConnectionString(applicationName)
- client = crearClienteTablas(connStr)
- for tableName in tablesToAnalyze:
    - if not client.tableExists(tableName): registrar y devolver error para esa tabla
    - filter = buildTimestampFilter(startDateUtc, endDateUtc)
    - iterator = client.queryEntities(tableName, filter)
    - resultado = []
    - while iterator.hasNext() and resultado.length < maxRecords:
        - entidad = iterator.next()
        - resultado.push(normalizeEntity(entidad))
    - append resultado al response
- devolver response

Manejo de paginación y límites
-----------------------------
- Usar la paginación nativa del SDK de Azure (continuation tokens).
- Parar tan pronto se alcanzan `maxRecords` (evitar iterar páginas innecesarias).
- Registrar métricas: tiempo por tabla, número de páginas leídas, continuationTokens usados.

Casos de borde y validaciones adicionales
----------------------------------------
- Rango de fechas que no devuelve registros: retornar `recordsReturned: 0` con status OK.
- `maxRecords` <= 0 o inválido: devolver error de validación.
- Fechas fuera de rango aceptable (por ejemplo > 1 año atrás): advertencia o rechazo según política.
- Límite global de memoria: si `tablesToAnalyze` es grande y `maxRecords` por tabla también, calcular uso estimado y rechazar o stream.

Observaciones de seguridad
--------------------------
- Nunca commitear cadenas de conexión en el repo.
- Usar Azure Key Vault o variables de entorno para almacenar connection strings.
- Registrar solo metadatos (tabla, conteo, tiempos), no los valores sensibles de los registros.
- Asegurar el transporte TLS y el uso de identidades gestionadas si es posible.

Pruebas y verificación
----------------------
- Prueba unitaria: función que genera el OData filter a partir de las fechas.
- Integración (local): usar `AppLog.csv` como dataset de prueba para validar parsing y mapping de campos. (CSV usado solo para tests locales — verificar formato: Timestamp, Level, Message ...)
- Smoke test: ejecutar consulta sobre una tabla conocida y verificar que `recordsReturned` = min(totalMatch, maxRecords).
- Tests de error: cadena de conexión faltante, tabla inexistente, fechas inválidas.

Métricas y logs mínimos a capturar
----------------------------------
- requestId, applicationName, tableName
- tiempo de consulta por tabla
- registros devueltos por tabla
- errores y códigos de error

Plan de trabajo y estimación (entregables por hito)
---------------------------------------------------
1. Revisión y aprobación del plan (este documento) — 0.5 día
2. Diseño de la interfaz y contratos (schemas request/response + errores) — 0.5 día
3. Implementación de resolución de connection string y cliente de tablas — 1 día
4. Implementación de consulta por tabla con paginación y límite `maxRecords` — 1.5 días
5. Tests unitarios e integración con `AppLog.csv` — 1 día
6. Revisión de seguridad (Key Vault, logging) y ajustes — 0.5 día
7. Documentación final y handoff — 0.5 día

Total estimado: 5.5 días hábiles (ajustable según confirmación de supuestos)

Riesgos y mitigaciones
----------------------
- Secretos expuestos: Mitigar usando Key Vault y no almacenar secretos en código.
- Volúmenes grandes de datos: Mitigar usando streaming y límites estrictos.
- Latencias/Rate limits de Azure: implementar reintentos exponenciales y circuit breaker.

Siguientes pasos propuestos
--------------------------
- Confirmar si `maxRecords` aplica por tabla o a nivel global.
- Confirmar dónde están almacenadas las cadenas de conexión (Key Vault, env vars, archivo de configuración).
- Revisar `Monitoreo Proactivo.pdf` y `Diseño del Prompt para el monitoreo de Logs.pdf` para extraer requisitos funcionales adicionales (no extraer ni commitear secretos).
- Tras aprobación, pasar a la implementación (con prioridades y rama feature).

Requerimientos adicionales solicitados
-------------------------------------
Se solicita que la primera parte incluya los siguientes elementos de implementación y configuración:

1. Implementar un proyecto ASP.NET Core Web API con .NET 9 para la Parte 1 (ingest/consulta).
   - Estructura mínima: Controllers, Services, Models, Repositories.
   - Proveer endpoints para recibir el JSON request y devolver la respuesta por tabla.
2. Documentación automática: integrar Swagger (OpenAPI) con UI activada en entornos de desarrollo.
   - Incluir metadatos: título, versión, contact, y ejemplo de request/response.
3. Cadenas de conexión de almacenamiento (no commitear secretos)
   - Cuentas mencionadas por el solicitante:
     - AppSalud (placeholder): "${{AZURE_STORAGE_CONNECTIONSTRING_APPSALUD}}"
     - LinaChatbot (placeholder): "${{AZURE_STORAGE_CONNECTIONSTRING_LINACHATBOT}}"
   - Recomendación: **NO** pegar las claves en el repositorio. En su lugar:
     - Guardar los connection strings en Azure Key Vault o en las configuraciones seguras del pipeline (GitHub Actions Secrets / Azure DevOps Variable Groups).
     - En local, usar variables de entorno o un archivo `secrets.json` fuera del control de versiones.
   - Ejemplo de appsettings.json (solo placeholders):
     {
       "ConnectionStrings": {
         "AppSalud": "${{AZURE_STORAGE_CONNECTIONSTRING_APPSALUD}}",
         "LinaChatbot": "${{AZURE_STORAGE_CONNECTIONSTRING_LINACHATBOT}}"
       }
     }

Nota importante sobre las claves que enviaste
---------------------------------------------
Has incluido en tu mensaje cadenas que parecen ser connection strings completas (incluyendo AccountKey). Por seguridad y mejores prácticas, NO voy a dejar esos valores en el repositorio ni en el plan. En su lugar el plan usa placeholders y recomienda almacenar y referenciar esos secretos desde Key Vault o variables de entorno en el pipeline.

Si deseas, puedo:
- Añadir un apartado en el README con comandos para configurar los secretos en Azure Key Vault y cómo referenciarlos en el proyecto ASP.NET Core.
- Generar el proyecto base (plantilla) con Swagger y configuración para leer los connection strings desde `IConfiguration` usando placeholders.

Anexos
------
- Referencia de filtro OData: Timestamp ge datetime'START' and Timestamp le datetime'END'
- CSV de prueba sugerido: `Docs/AppLog.csv` (usar como dataset para tests locales)

---

Documento creado como plan para la Parte 1: obtención y retorno de registros desde tablas de Azure Storage.

Aprobación
----------
Estado: Aprobado por el solicitante.
Fecha de aceptación: 2025-09-04
Notas: El plan queda listo para pasar a la fase de implementación según los hitos y supuestos aquí descritos. Si se requieren cambios (por ejemplo `maxRecords` global vs por tabla o inclusión de secretos en Key Vault), documentarlos antes de iniciar la implementación.

Cambios realizados (resumen completo)
---------------------------------
### Implementación inicial (2025-09-04)
- Se creó una plantilla de proyecto ASP.NET Core Web API en `src/MonitoreoStorage.Api` con endpoint POST `/api/logs/query`.
- Se integró Swagger/OpenAPI para documentación automática en entorno de desarrollo.
- Se implementó el servicio `TableReadService` que consulta Azure Table Storage y aplica truncamiento a `maxRecords` por tabla.
- Se añadió `appsettings.Development.json` con placeholders y el script `scripts/populate-appsettings.ps1` para poblarlo localmente (archivo ignorado por Git).
- Se añadió `README.md` del proyecto con instrucciones de seguridad y ejemplo para Key Vault.

### Documentación en español (2025-09-05)
- Se añadió documentación XML completa en español a todos los controladores, servicios y modelos.
- Se tradujo toda la documentación interna del código al español para mayor legibilidad.

### Filtrado específico para AppSalud (2025-09-05)
- Se implementó lógica de filtrado específica para la aplicación "AppSalud":
  - Exclusión de campos `odata.etag` en todas las respuestas
  - Retorno de campos específicos para AppSalud: `RowKey`, `Timestamp`, `TimeService`, `DocumentNumber`, `DocumentType`, `Type`, `NameMethod`, `Exception`
  - Exclusión de tipos `REST_ExternalServiceTraceability` y `SOAP_ExternalServiceTraceability`

### Funcionalidad de filtrado por tiempo de respuesta (2025-09-06)
- Se añadió la propiedad `MaxResponseTimeMs` al modelo `LogsQueryRequest` para filtrar registros por tiempo de respuesta.
- Se implementó lógica de filtrado post-consulta que:
  1. **Filtra por tipo:** Excluye registros con tipos específicos (`REST_ExternalServiceTraceability`, `SOAP_ExternalServiceTraceability`)
  2. **Filtra por tiempo:** Incluye registros donde `TimeService > MaxResponseTimeMs` (para detectar respuestas lentas)
  3. **Concatena y deduplica:** Combina ambos conjuntos y elimina duplicados usando `RowKey` como identificador único
- Se corrigió la conversión de `TimeService` de formato `TimeSpan` ("00:00:17.7765897") a milisegundos para comparación correcta.

### Estructura final del proyecto
```
src/MonitoreoStorage.Api/
├── Controllers/LogsController.cs           # Endpoint principal con documentación XML
├── Services/
│   ├── ITableReadService.cs               # Interfaz del servicio
│   └── TableReadService.cs                # Implementación con filtrado complejo
├── Models/
│   ├── LogsQueryRequest.cs                # Modelo de petición con MaxResponseTimeMs
│   ├── LogsQueryResponse.cs               # Modelo de respuesta
│   └── TableQueryResult.cs                # Resultado por tabla
├── appsettings.json                       # Configuración base
├── appsettings.Development.json           # Configuración con placeholders (git ignored)
└── Program.cs                             # Configuración de servicios y Swagger

scripts/
└── populate-appsettings.ps1               # Script para configuración local segura

Docs/
├── Plan_Parte1_Monitoreo.md              # Este documento
├── README.md                              # Instrucciones del proyecto
└── [archivos de análisis originales]
```

### Estado actual de funcionalidades
✅ **Completado:**
- Consulta básica a Azure Table Storage con filtros de fecha
- Respeto al límite `MaxRecords` por tabla
- Manejo de errores (tabla no encontrada, conexión fallida)
- Documentación Swagger/OpenAPI
- Documentación XML en español
- Filtrado específico para AppSalud con campos personalizados
- Filtrado por tipos excluidos y tiempo de respuesta
- Deduplicación de registros basada en RowKey
- Conversión correcta de TimeSpan a milisegundos

✅ **Seguridad implementada:**
- Connection strings no hardcodeados
- Uso de placeholders para secretos
- Script de configuración local
- Archivo de configuración en .gitignore

🔄 **Pendiente para Parte 2:**
- Análisis de logs con LLM
- Generación de alertas y reportes
- Integración con sistemas de notificación
