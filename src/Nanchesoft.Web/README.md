# Nanchesoft Shell V3 — Rediseño profesional Azure-like

Rediseño completo del shell global (TopBar + SideMenu + Command Palette) inspirado
en Azure Portal y Hikvision (HikCentral), manteniendo intacta toda la lógica .NET
de tu Blazor Server: rutas, NavigationService, AuthState, ShellMemoryStore, etc.

## Qué cambió

- **TopBar** (`Shared/TopBar.razor`)
  - Gradiente real Azure (no plano).
  - Iconografía SVG profesional inline (waffle, lupa, campana con badge,
    ayuda, configuración, caret) — ya no usa `☰` ni `⌕` como caracteres.
  - Marca con logo SVG real (no sólo "NS").
  - Búsqueda global con dropdown grande, secciones (Resultados / Recientes)
    y atajo `Ctrl + K` visible en pill blanco.
  - Cluster de iconos (notificaciones, ayuda, configuración) con separadores
    verticales sutiles entre zonas funcionales.
  - Chips de contexto con eyebrow + valor (Tenant / Empresa / Sucursal).
  - Pill de usuario con avatar, nombre, rol y caret — abre dropdown rico
    (Mi perfil / Cambiar contexto / Cerrar sesión) con animación.

- **SideMenu** (`Shared/SideMenu.razor`)
  - Header con logo + título + subtítulo.
  - Tarjeta destacada **"Módulo actual"** con pill de código del módulo.
  - Buscador prominente (DevExpress) con focus ring azul Azure.
  - TreeView (DevExpress) con:
    - **Indicador de selección lateral azul** (estilo VSCode/Azure).
    - Iconos por módulo con gradiente.
    - Chevron SVG que rota al expandir.
    - Hover, focus y selected con estados claros y consistentes.
  - Footer con **Favoritos** y **Recientes** como listas reales (con
    código de módulo, título y subtítulo) — no chips dispersos.
  - Empty state con ilustración SVG de lupa.

- **Command Palette** (`Shared/ShellCommandPalette.razor`)
  - Sin cambios funcionales (el JS sigue manejándolo).
  - El CSS v3 aplica acabados de borde y radio coherentes.

- **MainLayout** (`Layouts/MainLayout.razor`)
  - Agrega la clase contenedora `ns-shell-v3` que activa todos los estilos
    nuevos. Si quieres revertir el rediseño, simplemente quita esa clase.

- **CSS** (`wwwroot/app.css`)
  - Se anexó un bloque grande nuevo al final (~580 líneas) bajo el comentario
    `NANCHESOFT SHELL V3 · 2026-04-30`. **No se removió nada del CSS original**:
    todo el código previo sigue ahí intacto. Las nuevas reglas están scopeadas
    bajo `.ns-shell-v3 ...` para no afectar otras pantallas.
  - Tokens propios: `--v3-azure-1..4`, `--v3-ink-...`, `--v3-line`, `--v3-radius-...`,
    `--v3-shadow-...`. Si necesitas ajustar paleta, está en un solo lugar.

- **JS** (`wwwroot/js/ns-navigation.js`)
  - `placeholder` del buscador del sidebar cambió a "Buscar pantalla, módulo o atajo...".
  - El render de cada item del tree ahora incluye un `__indicator` (la barra
    lateral azul de seleccionado) y el chevron es un SVG que rota al expandir.
  - El comportamiento (init, search, navigate, sync) **no cambió**.

## Cómo instalar

Copia los siguientes archivos a tu proyecto, sobrescribiendo los actuales:

```
Layouts/MainLayout.razor          → src/Nanchesoft.Web/Layouts/MainLayout.razor
Shared/TopBar.razor               → src/Nanchesoft.Web/Shared/TopBar.razor
Shared/SideMenu.razor             → src/Nanchesoft.Web/Shared/SideMenu.razor
wwwroot/app.css                   → src/Nanchesoft.Web/wwwroot/app.css
wwwroot/js/ns-navigation.js       → src/Nanchesoft.Web/wwwroot/js/ns-navigation.js
```

> No se modificó ningún archivo `.cs` ni el `App.razor`. Tampoco se agregaron
> dependencias NuGet. Reinicia la app y haz hard-reload en el navegador
> (Ctrl + F5) para que los assets bustean cache.

## Cómo revertir

Tienes dos formas de revertir parcial o totalmente:

1. **Apagar el rediseño** sin tocar nada más: en `MainLayout.razor`, quita
   la clase `ns-shell-v3` del div raíz. El shell vuelve al estado anterior
   instantáneamente porque las reglas v3 sólo aplican bajo esa clase.

2. **Volver a la versión anterior**: restaurar los archivos originales que
   ya tienes en git.

## Detalles que vale la pena notar

- **Búsqueda en topbar**: si el input está vacío y enfocado, el dropdown muestra
  los **Recientes** (vienen de `ShellMemoryStore.GetRecents()`). Al escribir,
  reemplaza por resultados de `NavigationService.Search()`. Esto da la sensación
  Azure: "antes de buscar, te muestro lo que estabas haciendo".

- **Atajo Ctrl + K**: ya estaba en `nsShell.js` (no se tocó). Sigue funcionando
  igual; el pill blanco es ahora un trigger explícito y visible.

- **Notificaciones**: el badge rojo del icono de campana se renderiza si
  `NotificationCount > 0`. Ahora mismo el `0` está hardcodeado en el code-behind
  porque no encontré un servicio de notificaciones; cuando lo conectes, basta
  con cambiar `private int NotificationCount { get; set; } = 0;` por una propiedad
  derivada de tu servicio.

- **Dropdown del usuario**: las acciones "Mi perfil" y "Cambiar contexto" navegan
  a `/security/profile` y `/dashboard` por ahora. Ajústalas a tus rutas reales
  si tienes pantallas dedicadas.

- **Responsive**: a < 1180px se ocultan los chips de contexto. A < 980px el
  TopBar pasa a 2 filas (búsqueda baja a la segunda fila) y el sidebar deja
  de ser sticky para volverse un bloque normal.

- **Accesibilidad**: todos los botones de iconos tienen `title` y/o `aria-label`,
  el dropdown del user usa `animation: ns-tb3-dropin`, y los focus visibles
  tienen outline blanco/azul claro sobre el header oscuro.

## Compatibilidad

- DevExpress 25.1.6 (la versión que ya tienes en `App.razor`). El TreeView y
  el TextBox del buscador del sidebar siguen siendo de DevExpress; sólo cambian
  los estilos.
- Bootstrap 5 (no se removió, no se necesita pero no estorba).
- Blazor Server / .NET 8 / 9.

## Notas finales

- El shell v3 NO toca pantallas de contenido. Las grids, formularios,
  print-centers, etc. siguen idénticas. Si en algún punto se ven raras es
  porque el `ns-shell2__pageframe` tiene radio ligeramente distinto;
  ajustable en una variable.
- Si te gusta más el azul más oscuro o más vibrante del header, edita
  el gradiente de `.ns-shell-v3 .ns-tb3 { background: ... }` en `app.css`
  (los colores son `#062b6e → #0a3d8f → #0f5ea8 → #1474c4`).
