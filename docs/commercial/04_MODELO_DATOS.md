# 04 · Modelo de datos comercial

> Se conservan las entidades actuales (`Customer`, `SalesOrder`, `SalesQuote`, `ItemPriceList`, etc.).
> Esta sección detalla las **nuevas entidades** para CRM, ecommerce B2B, portal de clientes y sync móvil.
> Todas las entidades incluyen los campos base: `Id (long)`, `CompanyId (long)`, `CreatedAt`, `CreatedById`, `UpdatedAt`, `UpdatedById`, `RowVersion`.

---

## A. Módulo CRM

### `Lead`
- `Id`, `CompanyId`
- `Source` (web, llamada, referido, evento, importado) — enum
- `Status` (`new`, `qualifying`, `qualified`, `disqualified`, `converted`)
- `CompanyName`, `ContactName`, `Phone`, `Email`, `Rfc?`
- `City`, `StateRegion`, `EstimatedRevenue?`
- `OwnerUserId` (vendedor asignado)
- `Score` (0–100)
- `CustomerId?` (cuando se convierte)
- `Tags` (M:N → `Tag`)

### `Opportunity`
- `Id`, `CompanyId`, `LeadId?`, `CustomerId?`
- `Name`, `Stage` (`prospect`, `qualified`, `proposal`, `negotiation`, `won`, `lost`)
- `ExpectedAmount`, `Currency`
- `Probability` (0–100), `ForecastCategory`
- `ExpectedCloseDate`
- `OwnerUserId`, `Tags`
- `LostReason?`

### `Activity`
- `Id`, `CompanyId`, `Subject`, `Type` (`visit`, `call`, `meeting`, `email`, `task`, `note`)
- `RelatedEntity` polimórfico → (`Lead`, `Opportunity`, `Customer`, `SalesOrder`)
- `ScheduledAt`, `CompletedAt?`, `DurationMin?`
- `OwnerUserId`, `Status` (`planned`, `in_progress`, `done`, `cancelled`)
- `GeoLat?`, `GeoLng?` (check-in)
- `VoiceNoteUrl?`, `PhotoUrls[]`, `NoteRich` (markdown)
- `OutcomeTag` (interés alto / medio / bajo / no interesado / cerrado)

### `Tag` + `TaggedEntity`
- `Tag(Id, CompanyId, Name, Color, Group)`
- `TaggedEntity(TagId, EntityType, EntityId)` — polimórfico genérico.

### `Pipeline` y `PipelineStage`
- Múltiples pipelines por empresa (ventas, posventa, distribución).
- `PipelineStage(PipelineId, Order, Name, ProbabilityDefault, ColorHex, IsWon, IsLost)`.

### `LeadScoreRule`
- Reglas declarativas: campo + operador + valor → puntos.
- Ej.: `EstimatedRevenue > 100000 → +30`.

---

## B. Ecommerce B2B / portal de clientes

### `B2BAccount`
- `Id`, `CompanyId`, `CustomerId` (1:1 con `Customer`)
- `Status` (`pendingApproval`, `active`, `suspended`)
- `CreditLine`, `CreditUsed`, `CreditAvailableComputed`
- `DefaultPriceListId`, `DefaultWarehouseId`
- `PreferredPaymentMethodId`, `PreferredCfdiUse`, `RequiresPo` (boolean)
- `AllowSelfCheckout` (boolean)

### `B2BUser`
- `Id`, `CompanyId`, `B2BAccountId`
- `Email`, `PasswordHash`, `Phone`, `Name`, `Role` (`buyer`, `approver`, `admin`)
- `IsActive`, `LastLoginAt`, `Locale`, `Timezone`
- `MfaEnabled`, `MfaSecret?`

### `B2BInvitation`
- Token de invitación, expiración, rol.

### `CartSession`
- `Id (uuid)`, `CompanyId`, `B2BAccountId?`, `B2BUserId?`, `SellerUserId?`
- `Channel` (`b2b_web`, `mobile_seller`, `pos`, `whatsapp`)
- `WarehouseId`, `PriceListId`, `Currency`
- `Status` (`active`, `checkingOut`, `converted`, `abandoned`)
- `ConvertedSalesOrderId?`
- `SubTotal`, `Discount`, `Tax`, `Total` (denormalizados, recalculados)
- `ExpiresAt` (TTL 14 días)
- `LastEventAt`

### `CartLine`
- `Id`, `CartSessionId`
- `ProductId`, `VariantId?` (SKU específico talla/color)
- `Qty`, `UnitPrice`, `DiscountPct`, `LineTotal`
- `PricingMetadata` (jsonb: reglas aplicadas)
- `Comment?`, `RequestedDeliveryDate?`

### `CartCoupon`
- Cupones aplicables (futuro).

### `CustomerPortalUser` (puente con `B2BUser` si lo deseamos unificar)
- Mantener separado permite portal sin necesidad de ecommerce.

### `PortalNotification`
- `Id`, `CompanyId`, `RecipientUserId`, `Type`, `Title`, `Body`, `LinkUrl`, `ReadAt?`, `Channel` (`web`, `push`, `email`).

### `PortalDocument`
- Documentos publicados al cliente: catálogos PDF, certificados, listas de precios.

### `Wishlist`
- Listas de favoritos por cliente / comprador.

---

## C. Catálogo / precios — extensiones

### `ProductDigitalAsset`
- `ProductId`, `Type` (`image`, `video`, `pdf`, `dataSheet`)
- `Url`, `OrderIndex`, `Locale`, `AltText`.

### `ProductSeo`
- `ProductId`, `Slug`, `MetaTitle`, `MetaDescription`, `OgImageUrl`.

### `PriceListRule`
- Reglas ya existentes (revisar `ItemPriceListDetail`).
- Extender con: `MinQty`, `MaxQty`, `ValidFrom`, `ValidTo`, `CustomerGroupId?`, `CategoryId?`.

### `ProductInventoryAvailability` (vista materializada)
- `ProductId`, `VariantId`, `WarehouseId`, `OnHand`, `Reserved`, `Available`, `ReorderPoint`.
- Refresh con triggers o job cada 30 s.

---

## D. Fulfillment / seguimiento

### `OrderStateLog`
- `Id`, `SalesOrderId`, `FromState`, `ToState`, `ChangedAt`, `ChangedByUserId`, `Reason?`.

### `ShipmentTracking`
- `Id`, `SalesShipmentId`, `Carrier`, `TrackingNumber`, `LastStatus`, `LastLocation`, `EtaAt`.

### `DeliveryRoute`
- Para reparto local: vehículo, conductor, lista ordenada de paradas.

### `DeliveryStop`
- `RouteId`, `OrderShipmentId`, `Sequence`, `EtaAt`, `ArrivedAt?`, `DeliveredAt?`, `ProofPhotoUrl?`, `SignatureUrl?`.

---

## E. Vendedor / ruta

### `SellerRoute`
- `Id`, `CompanyId`, `SellerUserId`, `DayDate`, `Status` (`planned`, `in_progress`, `closed`)
- `StartLat`, `StartLng`, `EndLat`, `EndLng`, `OdometerStart`, `OdometerEnd`.

### `SellerVisit`
- `Id`, `SellerRouteId`, `CustomerId`, `PlannedAt`, `ArrivedAt?`, `LeftAt?`
- `GeoLat`, `GeoLng`, `CheckInMethod` (`gps`, `manual`, `qr`)
- `ActivityId` (link 1:1 con `Activity`).

### `SellerTarget`
- Cuotas mensuales por vendedor: monto, unidades, nuevos clientes.

---

## F. Sync móvil

### `SyncCursor`
- `Id`, `UserId`, `DeviceId`, `LastServerTimeUtc`, `EntityTypeFilter?`.

### `PendingOperation` (solo en cliente — SQLite Drift)
- `ClientOpId (uuid)`, `OpType`, `EntityType`, `Payload (jsonb)`, `Status` (`pending`, `sending`, `failed`, `applied`), `Attempts`, `LastError`.

### `EntityChangeLog` (servidor)
- `Id`, `CompanyId`, `EntityType`, `EntityId`, `Operation` (`upsert`/`delete`), `ChangedAt`, `ChangedByUserId`, `Payload (jsonb, snapshot mínimo)`.
- Particionado por mes (Postgres declarative partitioning).
- Purga > 90 días con tombstones livianos retenidos 180 días.

---

## G. Notificaciones / preferencias

### `NotificationPreference`
- `UserId`, `EventType`, `Channels[]` (`push`, `email`, `whatsapp`, `inApp`), `QuietHoursStart`, `QuietHoursEnd`.

### `DevicePushToken`
- `UserId`, `Platform` (`ios`, `android`, `web`), `Token`, `LastSeenAt`, `Active`.

---

## H. Multiempresa y datos compartidos

### `CompanyCommerceSetting`
- `CompanyId` (PK), `B2BEnabled`, `B2BLogoUrl`, `B2BDomain`, `B2BPrimaryColor`, `B2BWelcomeMessageMd`.
- `DefaultPriceListId`, `DefaultWarehouseId`, `MinOrderAmount`, `AllowsBackorders`.
- `ShippingPolicyMd`, `ReturnPolicyMd`, `PrivacyPolicyMd`.
- `CfdiUsoDefault`, `MetodoPagoDefault`, `FormaPagoDefault`.

---

## Índices y particiones críticas

```sql
-- Búsqueda rápida de productos
CREATE INDEX ix_product_search ON product
  USING gin (to_tsvector('spanish', coalesce(name,'') || ' ' || coalesce(sku,'')));

-- Pedidos por cliente y fecha (drill-down rápido en CRM)
CREATE INDEX ix_so_customer_date ON sales_order (company_id, customer_id, order_date DESC);

-- Carrito activo
CREATE INDEX ix_cart_active ON cart_session (company_id, b2b_account_id)
  WHERE status = 'active';

-- Sync cursors
CREATE INDEX ix_change_log_company_time ON entity_change_log (company_id, changed_at);

-- RLS
ALTER TABLE sales_order ENABLE ROW LEVEL SECURITY;
CREATE POLICY sales_order_tenant ON sales_order
  USING (company_id = current_setting('app.company_id', true)::bigint);
```

## Diagrama relacional resumido (CRM + Commerce)

```
Lead ──converts──> Customer ──has──> B2BAccount ──has──> B2BUser
  │                    │                    │
  │                    │                    └──opens──> CartSession ──has──> CartLine
  │                    │                                      │
  ▼                    │                                      └──converts──> SalesOrder
Opportunity ◄──────────┘                                                          │
  │                                                                                ▼
  └──has──> Activity                                                          OrderStateLog
                                                                              ShipmentTracking
SellerRoute ──has──> SellerVisit ──linksTo──> Customer
                          │
                          └──linksTo──> Activity
```
