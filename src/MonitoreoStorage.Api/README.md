MonitoreoStorage.Api - Template

Requisitos
- .NET 10 SDK instalado

Ejecutar localmente (PowerShell)
```powershell
# Establecer variables de entorno (ejemplo placeholders)
$env:AZURE_STORAGE_CONNECTIONSTRING_APPSALUD = "UseDevelopmentStorage=true"
$env:AZURE_STORAGE_CONNECTIONSTRING_LINACHATBOT = "UseDevelopmentStorage=true"

# Ejecutar
dotnet run --project src/MonitoreoStorage.Api/MonitoreoStorage.Api.csproj
```

Notas
- Las connection strings reales deben guardarse en Key Vault o secrets del pipeline.
- El proyecto usa Azure.Data.Tables para consultar las tablas. Si usas Azure Table Storage del emulador, `UseDevelopmentStorage=true` funciona para pruebas locales.
