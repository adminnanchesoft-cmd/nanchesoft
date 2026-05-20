# 02 · Visión de diseño y sistema visual

## Principios rectores

1. **Una pantalla, una intención.** Cada vista responde a una pregunta o ejecuta una acción primaria.
2. **El pulgar manda.** En móvil, todo lo accionable vive en el tercio inferior.
3. **Datos antes que cromo.** La interfaz es contenedor; el contenido (números, nombres, estados) es protagonista.
4. **Estados explícitos.** Loading, vacío, error, éxito — cada uno tiene tratamiento intencional, nunca un spinner anónimo.
5. **Reversible por default.** Toda acción destructiva ofrece deshacer o confirmación con consecuencia clara.
6. **Acceso, no profundidad.** ≤ 2 taps para llegar a cualquier acción frecuente.
7. **Multitenencia transparente.** El usuario sabe en qué empresa está, sin que estorbe.
8. **Industrial, no corporativo.** Tipografía robusta, contraste alto, sin gradientes innecesarios.

## NS Design System — fundamentos

### Paleta

```
Marca
  ns-primary       #0E5BA8     (Azul Nanchesoft industrial)
  ns-primary-700   #084078
  ns-primary-300   #5E92C9
  ns-accent        #F58220     (Naranja confianza / acción)
  ns-accent-700    #B85E10

Semánticos
  ns-success       #16A34A
  ns-warn          #D97706
  ns-danger        #DC2626
  ns-info          #0284C7

Neutros (modo claro)
  ns-bg            #F7F8FA
  ns-surface       #FFFFFF
  ns-border        #E2E5EA
  ns-text          #0F1722
  ns-text-muted    #5A6373

Neutros (modo oscuro — industrial / nocturno)
  ns-bg            #0B1220
  ns-surface       #131C2E
  ns-border        #243047
  ns-text          #E8ECF3
  ns-text-muted    #94A0B5
```

### Tipografía

- **Display / títulos:** `Inter` (700, 600) — claridad en pantallas industriales.
- **Cuerpo:** `Inter` (500, 400) — legibilidad óptima.
- **Mono / códigos / SKUs:** `JetBrains Mono` o `IBM Plex Mono`.

Escala (rem):

```
xs   12 / 16    metadatos, chips
sm   14 / 20    cuerpo secundario
md   16 / 24    cuerpo, default móvil
lg   18 / 26    subtítulos
xl   22 / 30    títulos de sección
2xl  28 / 36    headlines de pantalla
3xl  40 / 48    KPI big numbers
```

### Espaciado (8-pt grid)

`4, 8, 12, 16, 20, 24, 32, 40, 56, 72` — exponer como tokens `--ns-space-{n}`.

### Radio de bordes

```
sm  6 px   chips, badges
md  10 px  inputs, botones
lg  16 px  cards
xl  22 px  modales, bottom sheets
```

### Sombras

```
ns-shadow-1: 0 1px 2px rgba(15,23,34,.06)
ns-shadow-2: 0 4px 10px rgba(15,23,34,.08)
ns-shadow-3: 0 10px 25px rgba(15,23,34,.12)
ns-shadow-pop: 0 20px 45px rgba(15,23,34,.18)
```

### Iconos

- Set: **Lucide** (consistencia con Flutter via `lucide_icons` y Blazor via `MudBlazor` + custom mappings).
- Tamaños: 16, 20, 24, 32 px.
- Stroke 2 px default, 1.5 px en tamaños grandes.

### Motion

- Duraciones: `quick 120 ms`, `base 200 ms`, `slow 320 ms`.
- Curvas: `easeOutQuart` para entradas; `easeInOutCubic` para transiciones de página; `spring` (Flutter) para bottom sheets.
- Página entrante: slide-up 8 px + fade 200 ms.
- Tap feedback: scale 0.97 → 1.0 en 150 ms.

### Tactil — densidad

| Densidad | Min target | Uso |
|----------|------------|-----|
| Compact  | 36 px | Solo escritorio, tablas administrativas |
| Default  | 44 px | Web default |
| Touch    | 48–56 px | Móvil / tablet / bodega |
| Glove    | 64 px | Modo guantes (operadores) |

## Layout primario

### Móvil (Flutter)

```
┌──────────────────────────────┐
│  AppBar  (logo, búsqueda, 🔔) │  56 px
├──────────────────────────────┤
│                              │
│        CONTENIDO             │
│        (scroll vertical)     │
│                              │
├──────────────────────────────┤
│  ▢   ⨉   ⊕   ⌖   ☰          │  64 px (bottom nav)
└──────────────────────────────┘
  Inicio Clientes (FAB) Ruta  Más

FAB central elevado: "+ Nuevo pedido / Nueva visita" (contextual).
```

### Web Blazor — escritorio

```
┌─────────────┬───────────────────────────────────────┐
│  Sidebar    │  Topbar (búsqueda, empresa, perfil)   │
│  colapsable │───────────────────────────────────────│
│  240/72 px  │                                       │
│             │   Contenido                           │
│             │   • Breadcrumb                        │
│             │   • Acciones primarias (≤ 3)          │
│             │   • Filtros / chips                   │
│             │   • Vista (lista / kanban / etc.)     │
│             │                                       │
│             │                                       │
└─────────────┴───────────────────────────────────────┘
```

### Web Blazor — móvil/tablet

- Sidebar se convierte en `Drawer` lateral.
- Topbar mantiene búsqueda y empresa.
- Bottom action bar para acciones primarias (sticky).

## Patrones recurrentes

### `<NsKpiTile>` — tile de dashboard

```
┌─────────────────────────┐
│  Ventas del día         │
│  $182,540   ▲ 12.4%    │
│  ─── 7d sparkline ───   │
│  vs ayer  $162,310      │
└─────────────────────────┘
```

Props: `label`, `value`, `period`, `trend`, `sparkline`, `onClick → drilldown`.

### `<NsCustomerCard>` — tarjeta de cliente

```
┌─────────────────────────────────┐
│  ⚪ Calzados El Sol             │
│  RFC: ABC123456789              │
│  📍 Guadalajara, JAL            │
│  ────────────────────────────── │
│  Saldo:        $45,200          │
│  Crédito disp: $54,800 / 100k   │
│  Últ. pedido:  hace 3 días      │
│  ⊕ Pedido   ☎ Llamar  📍 Mapa  │
└─────────────────────────────────┘
```

### `<NsOrderStateTimeline>`

```
●──●──●──○──○
Borrador → Confirmado → Surtido → Embarcado → Entregado
```

Cada nodo: ícono + label + timestamp + actor.

### `<NsCartLine>` — línea de carrito B2B

- Avatar producto 56 px.
- Nombre + SKU + variante.
- Stepper +/– (touch 48 px).
- Precio unit. tachado si hay lista de precios aplicada.
- Subtotal alineado a la derecha.

### `<NsPriceList>` — precio con reglas

Muestra: precio base · precio aplicado · regla origen (chip) · ahorros.

### `<NsEmpty>` — estado vacío con CTA

Ilustración SVG (lineart, 1 tinta) + título + texto + 1-2 botones de acción.

### `<NsBottomSheet>` — hoja inferior

Para acciones contextuales móviles. Drag handle, alturas 30 % / 65 % / 92 %.

### `<NsCommandBar>` — barra de comandos (multi-selección)

Aparece sticky bajo el header cuando hay items seleccionados. Acciones masivas + contador + "Limpiar".

## Accesibilidad

- Contraste AA mínimo, AAA para texto en KPIs grandes.
- Foco visible siempre (`outline: 2px solid var(--ns-accent)`).
- Soporte para lector de pantalla en formularios.
- Modo alto contraste alterno.
- Targets ≥ 44 px (WCAG 2.5.5).
- Reduce-motion respetado.

## Modo oscuro

- Automático por preferencia del sistema.
- Toggle persistente por usuario.
- Modo "bodega" — oscuro + tipografía un step más grande + targets densidad `Glove`.

## Personalidad de marca

- **Voz:** directa, mexicana, profesional sin frialdad. "Listo", "Guardado", "Vamos", "Falta capturar X".
- **Errores:** explican qué pasó + qué hacer. "No se pudo guardar: el cliente no tiene crédito disponible. Solicita autorización a un supervisor."
- **Microcopy:** verbos en infinitivo en botones ("Crear pedido", no "Crear un nuevo pedido aquí").
