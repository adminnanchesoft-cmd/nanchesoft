# Ejecutar desde la carpeta raíz Nanchesoft
cd src\Nanchesoft.Api
Remove-Item -Recurse -Force .\artifacts -ErrorAction SilentlyContinue
Remove-Item -Force .\deploy-api.zip -ErrorAction SilentlyContinue

dotnet publish -c Release -o .\artifacts\publish
Compress-Archive -Path .\artifacts\publish\* -DestinationPath .\deploy-api.zip -Force

az webapp deploy `
  --resource-group rg-nanchesoft-dev `
  --name api-nanchesoft-prod `
  --src-path .\deploy-api.zip `
  --type zip
