# 14 · Wireframes ASCII — pantallas clave

> Bocetos textuales para alinear sin necesidad de Figma. Útiles para construir storyboards y pruebas con usuarios.

---

## Móvil · Inicio del vendedor

```
┌────────────────────────────────────────┐
│  ☰  Nanchesoft Ventas         🔍  🔔③  │
├────────────────────────────────────────┤
│                                        │
│  Buenos días, Carlos                   │
│  Martes 20 · 8 visitas hoy             │
│                                        │
│ ┌────────────┐ ┌────────────┐ ┌──────┐ │
│ │ Ventas hoy │ │ Cuota mes  │ │ Nvos │ │
│ │ $182,540   │ │ 62%        │ │  3   │ │
│ │ ▲ 12.4%    │ │ ▓▓▓▓▓▓░░░  │ │      │ │
│ └────────────┘ └────────────┘ └──────┘ │
│                                        │
│  Tu día                  [Ver ruta →]  │
│  ┌──────────────────────────────────┐  │
│  │ 09:00  Calzados El Sol         ● │  │
│  │        Pendiente · 2.1 km        │  │
│  ├──────────────────────────────────┤  │
│  │ 10:30  Don Pepe Maravilla     ✓ │  │
│  │        Visitado · pedido $9,200  │  │
│  ├──────────────────────────────────┤  │
│  │ 12:00  Súper Mty              ⏳ │  │
│  └──────────────────────────────────┘  │
│                                        │
│  Alertas                               │
│  ⚠ 3 saldos vencidos                  │
│  ⚠ Lead "Boticas Cano" sin contacto   │
│                                        │
├────────────────────────────────────────┤
│  ▢      ☷      ⊕      ⌖      ☰        │
│  Inicio Clientes (+) Ruta    Más       │
└────────────────────────────────────────┘
```

---

## Móvil · Editor de pedido (vista única scroll)

```
┌────────────────────────────────────────┐
│  ←  Nuevo pedido                ⋯      │
├────────────────────────────────────────┤
│  Cliente                               │
│  ┌──────────────────────────────────┐  │
│  │ Calzados El Sol         ⓧ        │  │
│  │ Crédito disp: $54,800            │  │
│  │ Lista: Mayoreo GDL               │  │
│  └──────────────────────────────────┘  │
├────────────────────────────────────────┤
│  Productos                  [+ 📷 ]    │
│  ┌──────────────────────────────────┐  │
│  │ 🔍 Buscar producto, SKU…         │  │
│  └──────────────────────────────────┘  │
│                                        │
│  [img] NS-1200 Bota Industrial T-26   │
│         $765 × 12 = $9,180        ⓧ   │
│         [    -   ]   12   [   +   ]   │
│                                        │
│  [img] NS-2200 Casco Soldador          │
│         $320 × 5  = $1,600        ⓧ   │
│         [    -   ]    5   [   +   ]   │
│                                        │
├────────────────────────────────────────┤
│  Resumen                               │
│  Subtotal              $ 10,780.00     │
│  Desc. mayoreo (-10%) - $   500.00     │
│  IVA 16%                $ 1,644.80     │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━     │
│  TOTAL                 $ 11,924.80     │
├────────────────────────────────────────┤
│  Entrega    Recoger | Domicilio        │
│  Fecha      Vie 23 may                 │
│  Pago       Transferencia ▾            │
│  Uso CFDI   G03 ▾                      │
├────────────────────────────────────────┤
│   [ Guardar borrador ]                 │
│   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━    │
│        [ Confirmar pedido ]            │
└────────────────────────────────────────┘
```

---

## Móvil · Ficha de cliente (CRM)

```
┌────────────────────────────────────────┐
│  ←  Calzados El Sol                 ⋯ │
├────────────────────────────────────────┤
│  RFC: CES940312AB1 · Mayoreo · A      │
│  📍 Av. Vallarta 1200, Guadalajara    │
├────────────────────────────────────────┤
│ ┌────────────┐ ┌────────────┐ ┌──────┐ │
│ │ Saldo      │ │ Crédito    │ │ Ticket│ │
│ │ $45,200    │ │ $54,800/   │ │ $8.4k│ │
│ │ vencido $0 │ │   100,000  │ │ prom │ │
│ └────────────┘ └────────────┘ └──────┘ │
│                                        │
│  Resumen  Pedidos  Estado  Productos   │
│  ──────                                │
│  Última visita: ayer (Carlos)          │
│  Frecuencia: 2x semana                 │
│  Tags: [VIP] [Mayoreo] [GDL]           │
│                                        │
│  Actividad                             │
│  ┌──────────────────────────────────┐  │
│  │ 09:42 ✚ Visita – interés alto    │  │
│  │ Ayer  ⊕ Pedido #4521 $11,920     │  │
│  │ Lun   ☎ Llamada 2:12             │  │
│  └──────────────────────────────────┘  │
│                                        │
├────────────────────────────────────────┤
│  ☎ Llamar  ✉ Email  📍 Mapa  ⊕ Pedido │
└────────────────────────────────────────┘
```

---

## Móvil · Pipeline CRM (kanban horizontal)

```
┌────────────────────────────────────────┐
│  Pipeline · Mayo 2026         🔍 ⚙    │
├────────────────────────────────────────┤
│                                        │
│  ◀ Prospecto │ Calificado │ Propu... ▶ │
│  ┌──────────┐ ┌──────────┐ ┌─────────┐ │
│  │ El Sol   │ │ Maravilla│ │ Don P.  │ │
│  │ $45,000  │ │ $80,000  │ │ $32,000 │ │
│  │ 20%      │ │ 35%      │ │ 55%     │ │
│  │ Carlos R │ │ Andrea V │ │ Carlos R│ │
│  │ Hoy 4pm  │ │ Mar 21   │ │ Mié 22  │ │
│  └──────────┘ └──────────┘ └─────────┘ │
│  ┌──────────┐ ┌──────────┐ ┌─────────┐ │
│  │ Cano     │ │ Boticas  │ │         │ │
│  │ $12,000  │ │ $25,000  │ │  + Nva │ │
│  │ 25%      │ │ 30%      │ │ oport. │ │
│  └──────────┘ └──────────┘ └─────────┘ │
│  ┌──────────┐                          │
│  │ + Nva    │                          │
│  └──────────┘                          │
│                                        │
└────────────────────────────────────────┘
```

---

## Web B2B · Catálogo

```
┌──────────────────────────────────────────────────────────────┐
│ LOGO    Catálogo  Pedidos  Estado cta  Soporte  🔍 🔔 👤 🛒 4│
├────────────┬─────────────────────────────────────────────────┤
│ Categorías │  Botas Industriales (124)            ⊞ ☰ ↕ Mejor│
│ ▾ Calzado  │  ┌────────────────────────────────────────────┐ │
│   Botas    │  │  [imagen]   NS-1200 Bota Ind. T-25-28      │ │
│   Tenis    │  │             $765/par  ·  En stock          │ │
│ ▾ EPP      │  │             ▢ Comparar    [ + Carrito ]    │ │
│   Cascos   │  ├────────────────────────────────────────────┤ │
│   Guantes  │  │  [imagen]   NS-1300 Bota Soldador          │ │
│            │  │             $820/par  ·  Pocas piezas      │ │
│ Marcas     │  │             ▢ Comparar    [ + Carrito ]    │ │
│ ☐ Pegaso   │  ├────────────────────────────────────────────┤ │
│ ☐ Nano     │  │   ... (24 productos por página)             │ │
│            │  └────────────────────────────────────────────┘ │
│ Precio     │                                                 │
│ [─o──────] │                                                 │
└────────────┴─────────────────────────────────────────────────┘
```

---

## Web B2B · Checkout

```
┌──────────────────────────────────────────────────────────────┐
│  Checkout                                          Paso 3 / 3│
├──────────────────────────────────┬───────────────────────────┤
│  Entrega                          │  Resumen del pedido       │
│  ◉ Domicilio   ○ Recoger          │  Subtotal      $10,780.00 │
│  Dirección: Av. Vallarta 1200…    │  Desc.        -$   500.00 │
│  Fecha solicitada: 23 may         │  IVA            $1,644.80 │
│                                   │  ────────────────────────  │
│  Facturación                      │  TOTAL         $11,924.80 │
│  RFC: CES940312AB1                │                           │
│  Uso CFDI: G03 ▾                  │  3 productos              │
│  Régimen: 601                     │  Almacén: Matriz GDL      │
│  Forma de pago: 03 Transferencia▾ │                           │
│  Método: PUE ▾                    │                           │
│  OC del cliente: ____________     │                           │
│                                   │                           │
│  Pago                             │                           │
│  ◉ Cargar a crédito ($54,800)    │                           │
│  ○ SPEI con referencia            │                           │
│  ○ Tarjeta (Stripe)              │                           │
│                                   │  [ Confirmar pedido ]     │
│  ☐ Acepto términos y condiciones  │                           │
└──────────────────────────────────┴───────────────────────────┘
```

---

## Web B2B · Estado de cuenta

```
┌──────────────────────────────────────────────────────────────┐
│  Estado de cuenta — Calzados El Sol                          │
├──────────────────────────────────────────────────────────────┤
│ ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐           │
│ │ Saldo   │  │ Vencido │  │ Crédito │  │ Próx.   │           │
│ │ $45,200 │  │ $0      │  │ $54,800 │  │ vencim. │           │
│ │         │  │         │  │ /100k   │  │ 27 may  │           │
│ └─────────┘  └─────────┘  └─────────┘  └─────────┘           │
│                                                              │
│  Facturas               [ Filtrar ▾ ]    [ Pagar selección ]│
│  ┌─────────┬──────────┬─────────┬─────────┬──────────┬──────┐│
│  │ ☐ Folio │ Fecha    │ Total   │ Vence   │ Estado   │ Acci.││
│  ├─────────┼──────────┼─────────┼─────────┼──────────┼──────┤│
│  │ ☐ A-983 │ 15/05/26 │$22,400  │ 27/05/26│ Pendiente│ … 📄 ││
│  │ ☐ A-991 │ 17/05/26 │$11,924  │ 30/05/26│ Pendiente│ … 📄 ││
│  │ ☐ A-984 │ 14/05/26 │$10,876  │ 25/05/26│ Pagada ✓│ … 📄 ││
│  └─────────┴──────────┴─────────┴─────────┴──────────┴──────┘│
│                                                              │
│  [ Exportar Excel ]    [ Imprimir PDF ]                      │
└──────────────────────────────────────────────────────────────┘
```

---

## Web · Dashboard del director

```
┌──────────────────────────────────────────────────────────────┐
│  Dashboard Director · Acme S.A.       Hoy ▾  Modo TV  ✎     │
├──────────────────────────────────────────────────────────────┤
│ ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐ │
│ │ Ventas MTD │ │ Margen MTD │ │ Cartera ven│ │ Pedidos pen│ │
│ │ $2.4M      │ │ 32.1%      │ │ $187k ⚠   │ │ 42         │ │
│ │ ▲ 14% vs ay│ │ ▼ -0.4 pp  │ │ 23 clientes│ │ 7 vencidos │ │
│ │ ───sparkl──│ │ ───sparkl──│ │ aging chart│ │ ↘ baja 2   │ │
│ └────────────┘ └────────────┘ └────────────┘ └────────────┘ │
│                                                              │
│ ┌──────────────────────────┐ ┌─────────────────────────────┐│
│ │ Cuotas equipo de ventas  │ │ Top 10 clientes (MTD)        ││
│ │ Carlos R   ▓▓▓▓▓▓▓░ 78%  │ │ 1. El Sol         $182k     ││
│ │ Andrea V   ▓▓▓▓▓░░░ 51%  │ │ 2. Maravilla       $94k     ││
│ │ Luis P     ▓▓▓▓▓▓▓▓ 91%  │ │ 3. Don Pepe        $72k     ││
│ │ Sandra T   ▓▓▓░░░░░ 32%  │ │ 4. Súper Mty       $64k     ││
│ │ ...                       │ │ ...                         ││
│ └──────────────────────────┘ └─────────────────────────────┘│
│                                                              │
│ ┌──────────────────────────────────────────────────────────┐│
│ │ Flujo proyectado 30 días                                 ││
│ │ ─── line chart ingresos vs egresos ───                   ││
│ └──────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────┘
```

---

## Móvil · Detalle de pedido con timeline

```
┌────────────────────────────────────────┐
│  ←  Pedido #4521                  ⋯   │
├────────────────────────────────────────┤
│  Calzados El Sol                       │
│  20 may · $11,924.80 · Confirmado     │
├────────────────────────────────────────┤
│  Estado                                │
│  ●────●────○────○────○                 │
│  Borr  Conf Surt Emba  Entr            │
│                                        │
│  Líneas (2)                            │
│  • NS-1200 ×12   $9,180                │
│  • NS-2200 ×5    $1,600                │
│                                        │
│  Tracking en vivo  ─→ [ Ver mapa ]    │
│                                        │
│  Timeline                              │
│  20 may 11:42  ⊕ Pedido creado         │
│                por Carlos R.           │
│  20 may 11:43  ✓ Confirmado            │
│  20 may 14:15  📦 Iniciado surtido    │
│                Roberto M. (Almacén)    │
│  …                                     │
│                                        │
├────────────────────────────────────────┤
│ ☎ Llamar cliente  📝 Nota  ↻ Recomprar│
└────────────────────────────────────────┘
```
