HOTFIX 404 CATÁLOGOS ORANGE / SILVASOFT

El error de pantalla:
Response status code does not indicate success: 404 (Not Found)

significa que la pantalla Web sí existe, pero el API publicado todavía no tiene los endpoints nuevos.
Se deben desplegar API y Web, no solo Web.

Catálogos incluidos y verificados en código:
- /products/colors -> GET /api/products/colors
- /products/manufacturing-types -> GET /api/products/manufacturing-types
- /products/toe-caps -> GET /api/products/toe-caps
- /products/sole-colors -> GET /api/products/sole-colors
- /products/dies -> GET /api/products/dies
- /products/quality-control-dies -> GET /api/products/quality-control-dies
- /products/folio-patterns -> GET /api/products/folio-patterns

Orden correcto:
1. Copiar este patch sobre la raíz del proyecto Nanchesoft.
2. Ejecutar database/20260513_orange_product_capture_catalogs.sql en PostgreSQL.
3. Publicar API:
   scripts/deploy-api-after-orange-catalogs.ps1
4. Publicar Web:
   scripts/deploy-web-after-orange-catalogs.ps1
5. Verificar rutas:
   scripts/verify-orange-catalog-endpoints.ps1

IMPORTANTE:
Si /api/products/colors sigue regresando 404, el API no fue desplegado o el Program.cs publicado no trae:
app.MapProductOrangeCatalogEndpoints();

Este patch normaliza los catálogos operativos de Orange/Silvasoft sin copiar tal cual el modelo viejo.
