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

Seguridad y manejo de secrets
----------------------------
- Nunca commitees archivos que contengan claves o connection strings reales. El archivo `appsettings.Development.json` es útil localmente pero **no** debe subirse al repositorio.
- Para desarrollo local, usa variables de entorno (PowerShell ejemplo arriba) o un archivo `appsettings.Development.json` fuera del control de versiones.
- Para producción, almacena los connection strings en Azure Key Vault y configura tu pipeline (GitHub Actions, Azure DevOps) para inyectar esos secretos en la configuración de la app.

Ejemplo rápido: cómo guardar un secret en Azure Key Vault y otorgar acceso a la app (resumen)
1. Crear Key Vault y añadir secret:
	- az keyvault create -n MyVault -g MyResourceGroup -l eastus
	- az keyvault secret set --vault-name MyVault -n "AZURE_STORAGE_CONNECTIONSTRING_APPSALUD" --value "<your-connection-string>"
2. Conceder acceso a la identidad que ejecuta la app o pipeline (Managed Identity o Service Principal).
3. Configurar la aplicación para leer el secret desde Key Vault (Azure SDK o configuración en Azure App Service).

Si quieres, genero un `docs/KEYVAULT_SETUP.md` con comandos concretos y pasos para CI/CD.
