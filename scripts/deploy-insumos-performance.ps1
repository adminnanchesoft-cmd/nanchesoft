$ErrorActionPreference = "Stop"

$resourceGroup = "rg-nanchesoft-dev"
$apiApp = "api-nanchesoft-prod"
$webApp = "web-nanchesoft-prod"

$root = Split-Path -Parent $PSScriptRoot
$apiZip = Join-Path $root "src/Nanchesoft.Api/deploy-api.zip"
$webZip = Join-Path $root "src/Nanchesoft.Web/deploy-web.zip"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI no está instalado o no está en PATH."
}

if (-not (Test-Path $apiZip)) {
    throw "No existe el paquete API: $apiZip"
}

if (-not (Test-Path $webZip)) {
    throw "No existe el paquete Web: $webZip"
}

Write-Host "Deploy API: $apiZip"
az webapp deploy `
  --resource-group $resourceGroup `
  --name $apiApp `
  --src-path $apiZip `
  --type zip

Write-Host "Restart API: $apiApp"
az webapp restart `
  --resource-group $resourceGroup `
  --name $apiApp

Write-Host "Deploy Web: $webZip"
az webapp deploy `
  --resource-group $resourceGroup `
  --name $webApp `
  --src-path $webZip `
  --type zip

Write-Host "Restart Web: $webApp"
az webapp restart `
  --resource-group $resourceGroup `
  --name $webApp

Write-Host "Deploy terminado. Abre una pestaña nueva o limpia cache para verificar la marca Build insumos."
