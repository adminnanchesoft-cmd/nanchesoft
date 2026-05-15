PARCHE: Corridas normalizadas para Nanchesoft Cloud

Qué agrega:
1) Corridas con detalle normalizado: ya no T1..T30/M1..M30 físicos en BD.
2) Campos equivalentes de Orange:
   - T = SizeCode
   - M = DisplayLabel
   - N = BarcodeLabel
   - F = FactorLabel
   - P = Proportion
   - Clave = LegacyKey
   - Clave2 = SecondaryKey
   - Consumos = ConsumptionMode
   - PuntoMedio = MiddlePoint
3) Endpoint CRUD nuevo: /api/products/size-runs-enterprise
4) Generación de variantes/SKU por talla: POST /api/products/size-runs-enterprise/{id}/generate-variants

Cómo instalar:
1) Descomprime este ZIP sobre la raíz de tu solución Nanchesoft.
2) Deja que Windows reemplace los archivos existentes.
3) Ejecuta en PostgreSQL:
   database/20260513_product_size_runs_enterprise.sql
4) Compila la solución.

Archivos incluidos con ruta exacta:
- src/Nanchesoft.Domain/Entities/ProductSizeRun.cs
- src/Nanchesoft.Domain/Entities/ProductSizeRunSize.cs
- src/Nanchesoft.Domain/Entities/ProductVariant.cs
- src/Nanchesoft.Persistence/Configurations/ProductSizeRunConfiguration.cs
- src/Nanchesoft.Persistence/Configurations/ProductSizeRunSizeConfiguration.cs
- src/Nanchesoft.Persistence/Configurations/ProductVariantConfiguration.cs
- src/Nanchesoft.Persistence/Context/NanchesoftDbContext.cs
- src/Nanchesoft.Api/Endpoints/ProductSizeRunEnterpriseEndpoints.cs
- src/Nanchesoft.Api/Program.cs
- database/20260513_product_size_runs_enterprise.sql

Nota:
No quité tu endpoint viejo /api/products/size-runs para no romper pantallas existentes.
El nuevo endpoint enterprise permite capturar corrida completa con posiciones horizontales.
