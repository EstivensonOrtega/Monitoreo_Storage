# Script para configurar variables de entorno para la Parte 2 (Análisis LLM)
# Ejecutar este script después de populate-appsettings.ps1

param(
    [Parameter(Mandatory=$false)]
    [string]$AzureOpenAIEndpoint = "https://orquestador-foundry.openai.azure.com",
    
    [Parameter(Mandatory=$false)]
    [string]$AzureOpenAIDeployment = "gpt-5-mini",
    
    [Parameter(Mandatory=$false)]
    [string]$AzureOpenAIApiVersion = "2025-03-01-preview"
)

Write-Host "=== Configuración de variables de entorno para Análisis LLM (Parte 2) ===" -ForegroundColor Green

# Verificar si ya existen las variables de Azure Storage
$appSaludExists = [Environment]::GetEnvironmentVariable("AZURE_STORAGE_CONNECTIONSTRING_APPSALUD", "User")
$linaChatbotExists = [Environment]::GetEnvironmentVariable("AZURE_STORAGE_CONNECTIONSTRING_LINACHATBOT", "User")

if (-not $appSaludExists -or -not $linaChatbotExists) {
    Write-Warning "Las variables de Azure Storage no están configuradas."
    Write-Host "Ejecuta primero: ./populate-appsettings.ps1" -ForegroundColor Yellow
    Write-Host ""
}

# Configurar variables de Azure OpenAI
Write-Host "Configurando variables de Azure OpenAI..." -ForegroundColor Blue

# Endpoint (ya conocido)
[Environment]::SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", $AzureOpenAIEndpoint, "User")
Write-Host "✓ AZURE_OPENAI_ENDPOINT = $AzureOpenAIEndpoint" -ForegroundColor Green

# API Key (solicitar al usuario)
$currentApiKey = [Environment]::GetEnvironmentVariable("AZURE_OPENAI_API_KEY", "User")
if ($currentApiKey) {
    $maskedKey = $currentApiKey.Substring(0, [Math]::Min(8, $currentApiKey.Length)) + "***"
    Write-Host "API Key actual: $maskedKey" -ForegroundColor Yellow
    $updateKey = Read-Host "¿Actualizar API Key? (y/N)"
    if ($updateKey -eq "y" -or $updateKey -eq "Y") {
        $apiKey = Read-Host "Ingresa la API Key de Azure OpenAI" -AsSecureString
        $apiKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))
        [Environment]::SetEnvironmentVariable("AZURE_OPENAI_API_KEY", $apiKeyPlain, "User")
        Write-Host "✓ AZURE_OPENAI_API_KEY actualizada" -ForegroundColor Green
    } else {
        Write-Host "✓ AZURE_OPENAI_API_KEY mantenida" -ForegroundColor Green
    }
} else {
    $apiKey = Read-Host "Ingresa la API Key de Azure OpenAI" -AsSecureString
    $apiKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))
    [Environment]::SetEnvironmentVariable("AZURE_OPENAI_API_KEY", $apiKeyPlain, "User")
    Write-Host "✓ AZURE_OPENAI_API_KEY configurada" -ForegroundColor Green
}

# Deployment name
[Environment]::SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", $AzureOpenAIDeployment, "User")
Write-Host "✓ AZURE_OPENAI_DEPLOYMENT = $AzureOpenAIDeployment" -ForegroundColor Green

# API Version
[Environment]::SetEnvironmentVariable("AZURE_OPENAI_API_VERSION", $AzureOpenAIApiVersion, "User")
Write-Host "✓ AZURE_OPENAI_API_VERSION = $AzureOpenAIApiVersion" -ForegroundColor Green

# Analysis config path
$configPath = "./Configuration/analysis-thresholds.json"
[Environment]::SetEnvironmentVariable("ANALYSIS_CONFIG_PATH", $configPath, "User")
Write-Host "✓ ANALYSIS_CONFIG_PATH = $configPath" -ForegroundColor Green

Write-Host ""
Write-Host "=== Configuración completada ===" -ForegroundColor Green
Write-Host ""
Write-Host "Variables configuradas:" -ForegroundColor Blue
Write-Host "- AZURE_OPENAI_ENDPOINT: $AzureOpenAIEndpoint"
Write-Host "- AZURE_OPENAI_DEPLOYMENT: $AzureOpenAIDeployment"
Write-Host "- AZURE_OPENAI_API_VERSION: $AzureOpenAIApiVersion"
Write-Host "- ANALYSIS_CONFIG_PATH: $configPath"
Write-Host "- AZURE_OPENAI_API_KEY: [CONFIGURADA]"
Write-Host ""
Write-Host "IMPORTANTE:" -ForegroundColor Red
Write-Host "1. Reinicia VS Code para que tome las nuevas variables de entorno"
Write-Host "2. Verifica que el deployment 'gpt-5-mini' esté disponible en tu Azure OpenAI"
Write-Host "3. Ejecuta el endpoint GET /api/analysis/status para verificar conectividad"
Write-Host ""
Write-Host "Comandos para probar:" -ForegroundColor Yellow
Write-Host "dotnet build"
Write-Host "dotnet run"
Write-Host ""

# Mostrar estado actual de todas las variables
Write-Host "=== Estado de todas las variables de entorno ===" -ForegroundColor Blue
$allVars = @(
    "AZURE_STORAGE_CONNECTIONSTRING_APPSALUD",
    "AZURE_STORAGE_CONNECTIONSTRING_LINACHATBOT", 
    "AZURE_OPENAI_ENDPOINT",
    "AZURE_OPENAI_API_KEY",
    "AZURE_OPENAI_DEPLOYMENT",
    "AZURE_OPENAI_API_VERSION",
    "ANALYSIS_CONFIG_PATH"
)

foreach ($var in $allVars) {
    $value = [Environment]::GetEnvironmentVariable($var, "User")
    if ($value) {
        if ($var -like "*KEY*" -or $var -like "*CONNECTION*") {
            $maskedValue = $value.Substring(0, [Math]::Min(8, $value.Length)) + "***"
            Write-Host "✓ $var = $maskedValue" -ForegroundColor Green
        } else {
            Write-Host "✓ $var = $value" -ForegroundColor Green
        }
    } else {
        Write-Host "✗ $var = [NO CONFIGURADA]" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Script completado. ¡Listo para usar análisis inteligente!" -ForegroundColor Green
