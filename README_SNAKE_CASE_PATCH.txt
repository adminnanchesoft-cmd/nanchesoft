Nanchesoft Cloud - Patch snake_case + corridas enterprise
=========================================================

Este ZIP está pensado para descomprimirlo encima de la raíz de tu solución Nanchesoft.

Qué corrige:
1) El proyecto deja de generar columnas PostgreSQL como "TenantId", "CreatedAt", "CompanyId".
2) Entity Framework queda forzado a usar snake_case: tenant_id, created_at, company_id.
3) Se conserva el uso de schemas reales: core, product, catalog, auth, sales, etc.
4) Corrige el SQL de corridas para que use product.product_size_runs, core.tenants, core.companies, etc.
5) Agrega script para convertir una base existente con columnas PascalCase a snake_case.

Archivos principales:
- src/Nanchesoft.Persistence/Context/ModelBuilderSnakeCaseExtensions.cs
- src/Nanchesoft.Persistence/Context/NanchesoftDbContext.cs
- database/20260513_convert_existing_database_to_snake_case.sql
- database/20260513_product_size_runs_enterprise.sql

Orden recomendado:
1) RESPALDA tu base de datos.
2) Descomprime este ZIP sobre tu carpeta Nanchesoft.
3) Compila el proyecto.
4) En pgAdmin ejecuta:
   database/20260513_convert_existing_database_to_snake_case.sql
5) Después prueba abrir el sistema.

Nota importante:
No pude compilar aquí porque este entorno no tiene instalado dotnet, pero el cambio está hecho a nivel de archivos fuente y SQL.

Si tienes migraciones EF anteriores generadas con "Id", "TenantId", etc., lo ideal es no seguirlas usando en producción.
A partir de este patch, las nuevas tablas/columnas deben salir en snake_case.
