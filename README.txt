Nanchesoft - FIX real CS0509

Copia la carpeta src encima de:
C:\NancheSoft\Nanchesoft\

Debe reemplazar este archivo:
src/Nanchesoft.Api/Endpoints/ProductSizeRunEnterpriseEndpoints.cs

Cambio aplicado:
- ProductSizeRunEnterpriseRequest ya NO es sealed
- ProductSizeRunSizeRequest ya NO es sealed

Después ejecuta:
dotnet build -c Release
