# Ejecutar desde la carpeta raíz Nanchesoft
cd src\Nanchesoft.Web
Remove-Item -Recurse -Force .\artifacts -ErrorAction SilentlyContinue
Remove-Item -Force .\deploy-web.zip -ErrorAction SilentlyContinue

dotnet publish -c Release -o .\artifacts\publish
Compress-Archive -Path .\artifacts\publish\* -DestinationPath .\deploy-web.zip -Force

az webapp deploy `
  --resource-group rg-nanchesoft-dev `
  --name web-nanchesoft-prod `
  --src-path .\deploy-web.zip `
  --type zip
