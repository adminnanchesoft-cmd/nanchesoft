namespace Nanchesoft.Api.Ai.KnowledgeBase;

public sealed class TrainingTopic
{
    public string Id { get; init; } = string.Empty;
    public string Module { get; init; } = "hr_payroll";
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
    public string Answer { get; init; } = string.Empty;
    public string? Route { get; init; }
}

public static class TrainingKnowledgeBase
{
    public static readonly IReadOnlyList<TrainingTopic> Topics = new[]
    {
        new TrainingTopic
        {
            Id = "alta_colaborador",
            Module = "hr",
            Title = "Cómo dar de alta un colaborador",
            Route = "/human-resources/employees",
            Keywords = new[]
            {
                "alta empleado","alta colaborador","capturar colaborador","capturar empleado",
                "nuevo trabajador","alta personal","registrar empleado","agregar personal",
                "agregar empleado","como capturo un colaborador","como capturo un empleado",
                "como doy de alta un empleado","como agrego personal","como registro un trabajador",
                "nuevo empleado","crear empleado","crear colaborador","nuevo colaborador"
            },
            Answer = """
Para dar de alta un colaborador en Nanchesoft:

1. Ve a **Recursos Humanos → Colaboradores**.
2. Presiona **Nuevo**.
3. Captura los datos generales:
   • Nombre, apellido paterno y apellido materno
   • RFC, CURP, NSS
   • Fecha de ingreso
   • Departamento y Puesto
   • Sucursal
4. Captura los datos de nómina:
   • Sueldo del periodo
   • Tipo de periodo (semanal, quincenal, mensual)
   • Tipo de contrato y régimen fiscal
5. Captura datos bancarios (CLABE, banco, cuenta) si aplicará dispersión.
6. Presiona **Guardar**.

Si algún catálogo (departamento, puesto, banco) no existe, usa el botón **+** para crearlo en línea.
"""
        },
        new TrainingTopic
        {
            Id = "capturar_incidencias",
            Module = "payroll",
            Title = "Cómo capturar incidencias",
            Route = "/human-resources/incidents",
            Keywords = new[]
            {
                "capturar incidencia","capturar incidencias","registrar incidencias","como capturo incidencias",
                "registrar faltas","capturar faltas","registrar permisos","capturar horas extra",
                "incidencia empleado","alta incidencia","agregar incidencia"
            },
            Answer = """
Para capturar incidencias de un empleado:

1. Ve a **Recursos Humanos → Incidencias** (o **Nómina → Captura matricial** para captura masiva).
2. Selecciona el **periodo de nómina** abierto.
3. Presiona **Nueva incidencia** y elige:
   • Empleado
   • Tipo de incidencia (Falta, Permiso con goce, Horas extra dobles, Vacaciones, etc.)
   • Fecha del evento
   • Cantidad (días u horas)
4. Si aplica, captura un importe manual o deja que se calcule según el catálogo de tipos.
5. Agrega un comentario y **Guarda**.

Tip: para incidencias recurrentes (ej. bono de transporte mensual), usa **Incidencias recurrentes** — se aplican automáticamente en cada periodo.
"""
        },
        new TrainingTopic
        {
            Id = "crear_conceptos",
            Module = "payroll",
            Title = "Cómo crear conceptos de nómina",
            Route = "/payroll/concepts",
            Keywords = new[]
            {
                "crear concepto","crear conceptos","alta concepto","nuevo concepto",
                "como crear conceptos","agregar concepto","capturar concepto","percepcion","deduccion"
            },
            Answer = """
Para crear un concepto de nómina:

1. Ve a **Nómina → Conceptos**.
2. Presiona **Nuevo**.
3. Captura:
   • Código y nombre (ej. P001 — Sueldo base)
   • Tipo de concepto: Percepción, Deducción u Otro pago
   • Clave SAT y agrupador SAT (para CFDI)
   • Tipo gravado (gravado, exento, mixto) y porcentajes
   • Fórmula de cálculo (si es automático)
4. Marca los flags relevantes:
   • Afecta IMSS / ISR / Acumuladores
   • Requiere timbrado
   • Imprimir en recibo
5. **Guarda** el concepto.

Una vez creado podrás usarlo en captura de incidencias, prenómina y cálculos automáticos.
"""
        },
        new TrainingTopic
        {
            Id = "abrir_periodo",
            Module = "payroll",
            Title = "Cómo abrir un periodo de nómina",
            Route = "/payroll/periods",
            Keywords = new[]
            {
                "abrir periodo","crear periodo","nuevo periodo","alta periodo nomina",
                "como abrir un periodo","como crear periodo","periodo de nomina","abrir nomina"
            },
            Answer = """
Para abrir un periodo de nómina:

1. Ve a **Nómina → Periodos**.
2. Presiona **Nuevo**.
3. Selecciona el **tipo de periodo** (semanal, quincenal, mensual, etc.).
4. Captura:
   • Código del periodo (ej. SEM-2026-20)
   • Fecha de inicio y fecha fin
   • Fecha de pago
   • Marca **¿Asegurado IMSS?** según aplique
5. Deja **IsClosed = false** para mantenerlo abierto.
6. **Guarda** el periodo.

Después podrás capturar incidencias, ejecutar prenómina y procesar la nómina dentro de ese periodo.
"""
        },
        new TrainingTopic
        {
            Id = "procesar_nomina",
            Module = "payroll",
            Title = "Cómo procesar (calcular) la nómina",
            Route = "/payroll/procesar",
            Keywords = new[]
            {
                "procesar nomina","calcular nomina","correr nomina","ejecutar nomina",
                "como procesar nomina","como calcular nomina","run de nomina","cierre nomina"
            },
            Answer = """
Para procesar la nómina del periodo abierto:

1. Ve a **Nómina → ✦ Procesar nómina**.
2. Selecciona el **periodo** y, si aplica, la **sucursal**.
3. Revisa el resumen de empleados a procesar y sus incidencias.
4. Presiona **Calcular** — el sistema generará percepciones automáticas (sueldo, séptimo día, ISR, IMSS, etc.).
5. Verifica el cálculo en **Detalle de nómina** o **Reporte de nómina**.
6. Si todo está correcto, presiona **Cerrar nómina** para fijar los importes y habilitar timbrado y dispersión.

Si encuentras un error, regresa a captura de incidencias o prenómina, ajusta y vuelve a calcular.
"""
        },
        new TrainingTopic
        {
            Id = "revisar_nomina",
            Module = "payroll",
            Title = "Cómo revisar una nómina",
            Route = "/payroll/reporte",
            Keywords = new[]
            {
                "revisar nomina","ver reporte nomina","auditar nomina","como revisar nomina",
                "validar nomina","reporte de nomina","detalle nomina"
            },
            Answer = """
Para revisar una nómina calculada:

1. Ve a **Nómina → Reporte de nómina** y selecciona el periodo.
2. Verifica los totales globales:
   • Percepciones, deducciones, neto a pagar
   • Número de empleados procesados
3. Abre **Nómina → Detalle nómina** para ver cada empleado.
4. Filtra por departamento o sucursal si necesitas focalizar.
5. Para auditar conceptos, abre **Percepciones y deducciones** del empleado.
6. Si detectas diferencias, regresa a la captura de incidencias o ajusta los conceptos involucrados y vuelve a procesar.
"""
        },
        new TrainingTopic
        {
            Id = "corregir_incidencias",
            Module = "payroll",
            Title = "Cómo corregir incidencias",
            Route = "/human-resources/incidents",
            Keywords = new[]
            {
                "corregir incidencia","editar incidencia","modificar incidencia",
                "borrar incidencia","eliminar incidencia","cancelar incidencia","como corregir incidencias"
            },
            Answer = """
Para corregir una incidencia ya capturada:

1. Ve a **Recursos Humanos → Incidencias**.
2. Filtra por **empleado** o por **periodo** para ubicarla.
3. Selecciona el registro y presiona **Editar**.
4. Modifica los campos necesarios (fecha, cantidad, importe, comentario).
5. **Guarda** los cambios.

Si la nómina del periodo ya fue calculada, vuelve a **Procesar nómina** para recalcular con los datos corregidos.
Si la incidencia ya no aplica, presiona **Eliminar** — se marcará como inactiva y no afectará el cálculo.
"""
        },
        new TrainingTopic
        {
            Id = "alta_departamento",
            Module = "hr",
            Title = "Cómo dar de alta departamentos",
            Route = "/human-resources/departments",
            Keywords = new[]
            {
                "alta departamento","crear departamento","nuevo departamento",
                "como dar de alta departamentos","departamentos","agregar departamento"
            },
            Answer = """
Para dar de alta un departamento:

1. Ve a **Recursos Humanos → Departamentos**.
2. Presiona **Nuevo**.
3. Captura:
   • Código (ej. PROD, ADM, VTA)
   • Nombre del departamento
   • Sucursal (si aplica)
   • Centro de costo (si tu nómina lo usa)
4. **Guarda**.

Una vez creado podrás asignarlo a colaboradores desde la pantalla **Colaboradores**.
"""
        },
        new TrainingTopic
        {
            Id = "capturar_prestamos",
            Module = "payroll",
            Title = "Cómo capturar préstamos a empleados",
            Route = "/payroll/loans",
            Keywords = new[]
            {
                "capturar prestamo","alta prestamo","nuevo prestamo","como capturar prestamos",
                "prestamo empleado","registrar prestamo","prestamo nomina"
            },
            Answer = """
Para registrar un préstamo a un empleado:

1. Ve a **Nómina → Préstamos**.
2. Presiona **Nuevo**.
3. Captura:
   • Empleado
   • Concepto de nómina con el que se descontará
   • Importe principal y número de quincenas/semanas (parcialidades)
   • Fecha de inicio del descuento
4. **Guarda** — el sistema calcula el importe por parcialidad.

Cada vez que proceses una nómina, el descuento se aplicará automáticamente desde **Nómina → Descuentos préstamos** hasta liquidar el saldo.
"""
        },
        new TrainingTopic
        {
            Id = "dispersion_nomina",
            Module = "payroll",
            Title = "Cómo dispersar la nómina al banco",
            Route = "/payroll/dispersion-batches",
            Keywords = new[]
            {
                "dispersar nomina","dispersion bancaria","pagar nomina banco","como dispersar nomina",
                "layout banco","archivo dispersion","banco dispersion"
            },
            Answer = """
Para dispersar la nómina al banco:

1. Asegúrate de que la nómina del periodo esté **cerrada**.
2. Ve a **Nómina → Dispersiones bancarias**.
3. Presiona **Nuevo lote** y selecciona el periodo y la cuenta bancaria emisora.
4. Marca a los empleados a incluir (por defecto se traen los del run cerrado).
5. Genera el **layout** del banco (BBVA, Banamex, Santander, etc.).
6. Sube el archivo en tu banca electrónica y, una vez confirmado, marca el lote como **Aplicado** en Nanchesoft.
"""
        },
        new TrainingTopic
        {
            Id = "agregar_concepto_recibo",
            Module = "payroll",
            Title = "Cómo agregar un concepto al recibo",
            Route = "/payroll/captura-matricial",
            Keywords = new[]
            {
                "agregar concepto recibo","agregar concepto a empleado","aplicar concepto",
                "concepto manual","captura matricial concepto"
            },
            Answer = """
Para aplicar un concepto adicional al recibo de un empleado:

1. Ve a **Nómina → Captura matricial** (o **Captura por recibo**).
2. Selecciona el **periodo** abierto.
3. Filtra el **empleado**.
4. Localiza la columna del **concepto** (percepción o deducción) y captura la cantidad o el importe.
5. **Guarda** — al recalcular la nómina se sumará/restará automáticamente.

Si el concepto no aparece en la cuadrícula, agrégalo primero en **Nómina → Conceptos** y luego habilítalo en las preferencias de columnas.
"""
        }
    };
}
