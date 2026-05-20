# 06 · App móvil para vendedores (Flutter)

> Producto principal: una app que un vendedor en ruta usa **todo el día**, sin enseñarle un manual.
> Promesa: **un pedido capturado en ≤ 90 segundos**, funcionando offline en zonas sin señal.

## Stack móvil

| Componente | Elección | Notas |
|------------|----------|-------|
| Framework | **Flutter 3.x** estable | Una base para iOS + Android + Web preview |
| Estado | **Riverpod 2** | Predecible, testeable, sin context-hell |
| Navegación | **go_router** | Deep links, redirects auth, transitions |
| HTTP | **dio** + interceptores (auth, retry, logging) | — |
| DB local | **drift** sobre SQLite | Tipado, migraciones, streams |
| Sync | propio (`SyncEngine`) | delta + cola |
| Push | **firebase_messaging** (FCM) + **APNs** | — |
| Geo | **geolocator** + **flutter_map** (OpenStreetMap) | OSM evita costos Google Maps |
| Cámara | **camera** + **image_picker** | Fotos de evidencia |
| Voz | **speech_to_text** | Notas dictadas |
| Escáner | **mobile_scanner** | Barcode/QR para productos |
| Firmas | **signature** | Comprobantes de entrega |
| Localización (i18n) | `flutter_localizations` + ARB | Es-MX default |
| Auth secure | **flutter_secure_storage** | JWT en Keychain/Keystore |
| Diseño | tema NS Design System (Material 3 customizado) | tokens compartidos con web |

## Estructura de navegación

```
┌──────────────────────────────────────┐
│  Bottom navigation (5 íconos)        │
│  Inicio · Clientes · ⊕ · Ruta · Más  │
└──────────────────────────────────────┘

⊕ FAB central contextual:
  · En Inicio        → "Nuevo pedido"
  · En Clientes      → "Nuevo cliente"
  · En Ruta          → "Nueva visita"
  · En cliente       → "Nuevo pedido a este cliente"
  · En CRM           → "Nueva actividad"
```

## Pantallas — inventario

### 1. Login
- Logo NS + selector de empresa (multitenancy).
- Email + password + "Recordarme" + biometría (Face ID / huella).
- Recuperar contraseña.
- Modo offline reintenta con cache cifrada del último token + biometría.

### 2. Inicio ("Mi día")
- **Saludo personalizado:** "Buenos días, Carlos · 8 visitas hoy".
- **KPIs personales (3 tiles):** Ventas hoy / Cuota mes / Clientes nuevos.
- **Sección 'Tu día'**:
  - Lista cronológica de visitas + actividades + pedidos a entregar.
  - Cada item: tap → detalle / drag → reordenar.
- **Sección 'Alertas'**:
  - Crédito vencido de clientes, leads sin contacto, pedidos pendientes.
- **Sección 'Sugerencias'**:
  - "Clientes que compran cada 15 días y no han hecho pedido."
  - "Productos nuevos disponibles."

### 3. Clientes
- Búsqueda instantánea (cliente local primero, luego API).
- Chips de filtro: A / B / C, vendedor, ciudad, con saldo vencido, nuevos.
- Vista: tarjetas verticales (`NsCustomerCard`).
- Tap → ficha de cliente.

### 4. Ficha de cliente
- Header: nombre, RFC, dirección, tags.
- KPIs: Saldo, Crédito disp., Ticket prom., Frecuencia.
- Tabs deslizables:
  - **Resumen** (info + contactos + mapa)
  - **Pedidos** (lista cronológica)
  - **Estado de cuenta** (facturas, pagos)
  - **Productos comprados** (top 20 con stepper para recompra)
  - **Actividad** (feed unificado: visitas, llamadas, comentarios)
- Acciones en bottom sheet: ☎ Llamar · ✉ Email · 🗺 Mapa · ⊕ Pedido · ✚ Visita · 📝 Nota.

### 5. Nuevo pedido (vendedor)
**Una sola vista, scroll vertical, sin tabs.**

```
┌──────────────────────────────────────┐
│  ← Nuevo pedido                      │
├──────────────────────────────────────┤
│ Cliente:  [ Buscar / escanear ]      │
│   ► Calzados El Sol (chip)           │
│      Crédito disp: $54,800           │
├──────────────────────────────────────┤
│ Productos                            │
│ ┌──────────────────────────────────┐ │
│ │ 🔍 Buscar producto / SKU / 📷    │ │
│ └──────────────────────────────────┘ │
│ • NS-1200 Bota Industrial   12 × 765 │
│   ───────────────────  [- 12 +]  ⓧ  │
│ • NS-2200 Casco Soldador     5 × 320 │
│   ───────────────────  [- 5 +]   ⓧ  │
├──────────────────────────────────────┤
│ Resumen                              │
│ Subtotal           $ 10,780.00       │
│ Desc. (mayoreo)   - $   500.00       │
│ IVA 16%             $ 1,644.80       │
│ Total              $ 11,924.80       │
├──────────────────────────────────────┤
│ Datos del pedido                     │
│ Almacén:   Matriz GDL                │
│ Entrega:   Recoger / Domicilio       │
│ Fecha:     23 may                    │
│ Forma de pago:  Transferencia        │
│ Uso CFDI:   G03                      │
├──────────────────────────────────────┤
│       [ Guardar borrador ]           │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│        [ Confirmar pedido ]          │
└──────────────────────────────────────┘
```

- Buscador de productos inline con sugerencias top-3.
- Escaneo de código de barras / QR → agrega directo al pedido.
- Bottom sheet de variantes (tallas) si aplica — grid táctil 4×n con stepper.
- Si excede crédito → modal con opciones: solicitar autorización, cambiar forma de pago, dividir pedido.
- Modo offline: se guarda en cola; el botón "Confirmar pedido" cambia a "Confirmar pedido (se subirá al reconectar)".

### 6. Pedidos
- Lista filtrable: estados (chips de colores), fecha, cliente, monto.
- Tap → detalle con timeline de estados, líneas, fotos, comentarios.
- Acciones según rol: cancelar, agregar nota, reimprimir, compartir.

### 7. Ruta
- Mapa con paradas del día (numeradas).
- Lista deslizable inferior con sus visitas.
- Botón "Iniciar ruta" → activa GPS background y guarda kilometraje.
- Optimización automática del orden (TSP heurístico + ventana horaria).
- Tap parada → check-in (geofence), abrir cliente.

### 8. Visitas / actividades
- Calendario semanal arriba; lista agrupada por día abajo.
- Crear actividad: tipo (visita, llamada, tarea), cliente, fecha, recordatorio.
- Notas de voz con transcripción.
- Marcar como hecha con outcome (interés alto/medio/bajo, "necesita cotización", "no interesado").

### 9. CRM (pipeline)
- Vista kanban deslizable horizontalmente (etapas).
- Cada tarjeta: cliente + monto + probabilidad + próximo paso.
- Drag-and-drop entre etapas (toque largo → arrastrar).
- Filtros: solo míos, este mes, tag.

### 10. Catálogo de productos (cuando hay tiempo)
- Grid 2 columnas con foto + nombre + precio.
- Filtros: categoría, disponibilidad, marca.
- Detalle con galería, variantes, "Agregar a pedido del cliente".

### 11. Más
- Cobranza (lista de saldos vencidos del vendedor)
- Notificaciones
- Cotizaciones
- Reportes (mi desempeño)
- Cambiar empresa
- Ajustes (idioma, modo oscuro, sincronizar, vaciar caché)
- Cerrar sesión

## Modo offline

- **Catálogos** (clientes, productos, precios) caché completo, sincronización delta cada 5 min en foreground / 30 min en background.
- **Pedidos creados offline:** quedan en cola con badge ⚠️ "Pendiente de subir". Reintento exponencial al recuperar señal.
- **Conflictos:** banner persistente "1 conflicto requiere revisión" → pantalla dedicada con diff y resolución.
- **Indicador de conectividad** en topbar (pequeño punto verde / ámbar / rojo).

## Diseño táctil

- Targets 48–56 px.
- Stepper de cantidad como botón grande:
  ```
  [   -   ]   12   [   +   ]
  ```
- Drag-and-drop con haptic feedback.
- Swipe en líneas de pedido: izq. → editar, der. → borrar (con undo).
- Pull-to-refresh estándar.
- Long press → menú contextual.

## Performance

- **Cold start objetivo:** ≤ 1.5 s en gama media.
- Skeleton screens en todas las listas.
- Lazy loading de imágenes (`cached_network_image`).
- Sprites locales para los iconos más usados.
- Precompilación AOT en release; tree-shaking de ARB no usados.

## Seguridad móvil

- JWT en `flutter_secure_storage` (Keychain/Keystore).
- PIN local opcional al abrir.
- Biometric unlock.
- Pinning de certificados en producción.
- Wipe local al cambiar de empresa o cerrar sesión.

## Telemetría

- Crashlytics (Firebase).
- Eventos clave: `order.created`, `order.captured.duration`, `cart.abandoned`, `visit.checkin.distance_from_customer`.
- Sesiones, retención, funnels.

## Lanzamiento

- Beta interna (Play Internal Testing + TestFlight) con 10-15 vendedores piloto.
- Beta cerrada (40-60 vendedores) durante 2 semanas.
- Lanzamiento general por empresa, con onboarding guiado in-app (3 tooltips máximo).
