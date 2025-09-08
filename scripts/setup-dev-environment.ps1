# Script para configurar variables de entorno para desarrollo
# Ejecutar: .\Scripts\setup-dev-environment.ps1

Write-Host "Configurando variables de entorno para desarrollo..." -ForegroundColor Green

# Azure OpenAI Configuration
Write-Host "Configurando Azure OpenAI..." -ForegroundColor Yellow
$env:AZURE_OPENAI_ENDPOINT = "https://orquestador-foundry.openai.azure.com"
$env:AZURE_OPENAI_DEPLOYMENT = "gpt-5-mini"
$env:AZURE_OPENAI_API_VERSION = "2024-02-15-preview"

# Solicitar la API Key de forma segura
Write-Host "Por favor, ingrese su Azure OpenAI API Key:" -ForegroundColor Cyan
$apiKey = Read-Host -AsSecureString
$env:AZURE_OPENAI_API_KEY = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))

# Azure Storage Configuration (si no está configurado)
if (-not $env:AzureStorageAccount__ConnectionString) {
    Write-Host "Por favor, ingrese su Azure Storage Connection String:" -ForegroundColor Cyan
    $connectionString = Read-Host
    $env:AzureStorageAccount__ConnectionString = $connectionString
}

Write-Host "Variables de entorno configuradas correctamente!" -ForegroundColor Green
Write-Host "Variables configuradas:" -ForegroundColor Yellow
Write-Host "- AZURE_OPENAI_ENDPOINT: $env:AZURE_OPENAI_ENDPOINT"
Write-Host "- AZURE_OPENAI_DEPLOYMENT: $env:AZURE_OPENAI_DEPLOYMENT"
Write-Host "- AZURE_OPENAI_API_VERSION: $env:AZURE_OPENAI_API_VERSION"
Write-Host "- AZURE_OPENAI_API_KEY: [CONFIGURADO]"

if ($env:AzureStorageAccount__ConnectionString) {
    Write-Host "- AzureStorageAccount__ConnectionString: [CONFIGURADO]"
}

Write-Host ""
Write-Host "Ahora puede ejecutar la aplicación con: dotnet run" -ForegroundColor Green
