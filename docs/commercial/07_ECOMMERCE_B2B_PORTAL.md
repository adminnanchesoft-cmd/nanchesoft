# 07 · Ecommerce B2B y portal de clientes (Blazor + MudBlazor)

> Web autoservicio para mayoristas que ya son clientes: catálogo personalizado, carrito persistente, checkout con listas de precios + crédito + CFDI 4.0, estado de cuenta.

## Premisas

- **No es B2C.** El usuario inicia sesión; ve **sus** precios, **su** crédito, **sus** productos.
- **Multidispositivo.** Mismo Blazor responde escritorio, tablet y móvil; layout cambia, lógica no.
- **Domino propio o subdominio por empresa.** `b2b.{empresa}.com` o `tienda.nanchesoft.mx/{empresa}`.
- **Sin fricción de pago.** Por default el cliente paga con su línea de crédito; pasarelas opcionales para PUE.

## Arquitectura Blazor

- **Blazor Server** (latencia baja LAN/Mx) con prerender estático para SEO de páginas públicas.
- **Render mode `InteractiveServer`** en sesiones autenticadas.
- **MudBlazor 7** como UI kit base, customizado con tokens NS Design System.
- Modo SSR para landing pública (registro, "solicita ser distribuidor").

## Navegación principal

```
┌────────────────────────────────────────────────────────────┐
│  [Logo]   Catálogo  Pedidos  Estado de cuenta  Soporte    │  ←─ Header
│                                            🔍  🔔  👤  🛒  │
└────────────────────────────────────────────────────────────┘
```

En móvil:
- Hamburguesa lateral con: Inicio, Catálogo, Mis pedidos, Estado de cuenta, Documentos, Notificaciones, Soporte, Cerrar sesión.
- Bottom action bar persistente con: Carrito (badge cantidad), Buscar, Inicio.

## Páginas

### 1. Login / registro
- Login con email + password + "recordarme" + MFA opcional.
- Auto-detección de cliente desde subdominio.
- Botón "Solicitar acceso" → formulario que crea `Lead` con flag `b2b_request`.

### 2. Inicio (post-login)
- Banner principal (anuncio del distribuidor o destacado).
- KPIs personales: Saldo, Crédito disponible, Pedidos en curso.
- **Recompra rápida**: top 6 productos comprados últimamente con botón "+ Agregar".
- **Pedido recurrente**: si tiene plantilla recurrente, botón "Generar pedido de mayo".
- **Promociones aplicables**: lista con CTA.
- **Noticias / catálogos publicados**.

### 3. Catálogo
- Layout grid 3-4 columnas escritorio, 2 móvil.
- Sidebar con árbol de categorías + filtros (marca, talla, disponibilidad, precio).
- Search bar con autocompletado.
- Tarjetas con: foto, nombre, SKU, precio (su precio), stock indicativo ("En stock" / "Pocas piezas" / "Bajo pedido"), botón "+ Carrito".
- Quick-add: clic en "+" abre stepper inline sin abandonar la lista.
- "Comparar" hasta 3 productos.

### 4. Detalle de producto
- Galería de fotos + video opcional.
- Variantes (tallas / colores) en chips.
- Tabla de precios por volumen ("Compra ≥ 50, ahorras 8%").
- Stock por almacén (si el rol lo permite).
- Datos técnicos descargables (PDF ficha técnica).
- Sección "Suelen comprar también".
- Botón gigante "+ Agregar al carrito" sticky en móvil.

### 5. Carrito
- Lista de líneas con stepper + comentario + fecha solicitada.
- Resumen pegado a la derecha en escritorio, abajo en móvil.
- Notas globales del pedido.
- Botón "Guardar como plantilla".
- Alertas inline:
  - "Tu pedido excede tu crédito disponible. Puedes pagar la diferencia ahora."
  - "Un producto no tiene stock en GDL — se enviará desde CDMX (+1 día)."

### 6. Checkout (1 sola página)
- **Step 1 – Entrega**
  - Almacén de origen (si aplica), domicilio o sucursal.
  - Fecha de entrega solicitada.
- **Step 2 – Facturación**
  - RFC, Uso CFDI, Régimen fiscal (preseleccionado).
  - Forma de pago, Método de pago (PUE/PPD).
  - Orden de compra del cliente (opcional).
- **Step 3 – Pago**
  - Cargar a crédito (default).
  - Transferencia SPEI con referencia única.
  - Pasarela (Stripe / Mercado Pago) si la empresa lo activa.
- Resumen sticky con totales, IVA, descuentos, IEPS si aplica.
- Botón **"Confirmar pedido"** grande; tras click, mensaje de confirmación + número de pedido + opción a "Ver seguimiento".

### 7. Mis pedidos
- Tabla / cards con: número, fecha, total, estado (chip), tracking si aplica.
- Filtros y búsqueda.
- Acciones por fila: ver detalle, recomprar, descargar factura, solicitar cancelación.

### 8. Detalle de pedido
- Timeline visual (`NsOrderStateTimeline`).
- Mapa con ubicación de la entrega (si embarcado).
- Líneas del pedido con cantidades y precios.
- Adjuntos: factura, evidencia de entrega.
- Sección de comentarios (chatter) público con el vendedor.
- Botón "Recomprar" → regenera carrito.

### 9. Estado de cuenta
- Resumen: saldo total, vencido, próximo vencimiento, crédito disponible.
- Tabla de facturas: número, fecha, monto, días vencidos, status.
- Acción "Pagar" por factura o por selección múltiple.
- Histórico de pagos.
- Export a Excel / PDF.

### 10. Documentos
- Listados de PDFs publicados por la empresa: catálogo, listas de precios, certificados, políticas.

### 11. Soporte
- Centro de ayuda (artículos).
- Crear ticket (`Activity` interna asignada al vendedor).
- Chat en vivo (SignalR hub `SupportHub`).

### 12. Perfil / ajustes
- Datos fiscales, contactos, direcciones de envío.
- Usuarios del B2BAccount (invitar comprador, aprobador).
- Notificaciones (qué quiero recibir y por dónde).

## Componentes Blazor / MudBlazor clave

| Componente | Base | Notas |
|------------|------|-------|
| `NsHeader` | `MudAppBar` | Logo + búsqueda + carrito |
| `NsProductCard` | `MudCard` + slots | Foto / nombre / SKU / precio / stock chip |
| `NsCartLine` | `MudListItem` | Stepper, comentario, total |
| `NsCheckoutSteps` | `MudStepper` | Único stepper visible en móvil compacto |
| `NsKpiTile` | `MudPaper` | Reutilizable en dashboards |
| `NsTimeline` | `MudTimeline` | Estados de pedido |
| `NsAddressPicker` | combinación | Sugerencias de Google/OSM, captura manual |

## Estados vacíos memorables

- Catálogo sin productos: ilustración de caja vacía + "Aún no hay productos en este catálogo. Habla con tu ejecutivo." + botón "Llamar".
- Sin pedidos: ilustración carrito vacío + "Aún no haces tu primer pedido aquí. Empieza con los productos más vendidos." + lista rápida.
- Sin facturas: "No tienes facturas pendientes. ¡Bien!"

## SEO y marketing

- Páginas públicas estáticas para landing por subdominio.
- Sitemap, OG tags, JSON-LD `Product` para productos públicos opcionales.
- Velocidad: Lighthouse ≥ 90 mobile, ≥ 95 desktop.

## Internacionalización

- `IStringLocalizer` con archivos `.resx` por cultura.
- `es-MX` default. Listos `es`, `en` próximas fases.

## Rendimiento Blazor

- `@key` consistente en listas para evitar diff costoso.
- Streams de búsqueda con debounce 250 ms.
- Virtualización de listas (`MudList Virtualize`).
- `[StreamRendering]` en páginas pesadas (Blazor 8+).
- Cache distribuido (Redis) para catálogo público y precios.
