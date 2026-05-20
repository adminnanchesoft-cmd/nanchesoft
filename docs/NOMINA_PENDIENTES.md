# Nomina - pendientes derivados de NOMINAS_Elemental.pdf

Referencia principal: `docs/NOMINAS_Elemental.pdf`.

Estado base validado:

- Captura individual de recibo: implementado.
- Captura matricial: implementado.
- Nomina periodica basica: implementado.
- Prestamos y deducciones: implementado.
- Importacion de checadas CSV/Silvasoft: implementado.
- Retardos y faltas configurables: implementado como base operativa en proceso de nomina.
- Prenomina configurable (config columnas persistida por usuario+periodo, exportar CSV): implementado (fase 2A).
- Movimientos globales (lote multiempleado con filtros, preview, aplicacion masiva, opcion permanente): implementado (fase 2A).
- Dias y horas tipo CONTPAQi (catalogo de claves FINJ/RET/HE1..HE5/INCAP/VAC, grid empleado×dia, consolidacion a prenomina): implementado (fase 2B).

## Prioridad siguiente

1. Prenomina configurable [HECHO]
   - Configurar columnas por periodo/usuario. (OK)
   - Elegir conceptos de percepcion, deduccion y obligacion que aparecen en matriz. (OK)
   - Guardar configuracion base y permitir configuracion especial por periodo. (OK)
   - Copiar importes a toda una columna. (OK)
   - Importar/exportar movimientos de prenomina. (OK)

2. Dias y horas tipo CONTPAQi [HECHO]
   - Captura diaria por empleado y dia del periodo en grid matricial. (OK)
   - Claves/mnemonicos: FINJ, FJUS, RET, HE1..HE5, PERM, PERMSG, INCAP, VAC, PRIMD, CASTG, DT. (OK)
   - Catalogo configurable por empresa (color, multiplicador, concepto destino, afecta nomina). (OK)
   - Consolidacion a PrePayrollAdjustment con prorrateo por unidades y multiplicador. (OK)

3. Captura por tipo de incidencia
   - Seleccionar una incidencia, fecha y unidades por omision.
   - Agregar multiples empleados en una sola captura.
   - Permitir ajustar unidades/fecha por empleado antes de guardar.

4. Movimientos globales [HECHO]
   - Filtros por departamento, puesto, sucursal, salario, empleados incluidos/excluidos. (OK)
   - Aplicar conceptos a grupos de empleados con preview. (OK)
   - Fecha inicio/fin, veces a aplicar, monto limite, acumulado, estado, numero de control. (OK)
   - Marcado opcional como permanente -> crea PayrollRecurringMovement por empleado. (OK)
   - Pendiente: ampliar filtros para registro patronal y turno cuando Employee tenga esas FK.

5. Conceptos avanzados y validacion fiscal
   - Agregar configuracion de formulas por concepto: importe total, gravado ISR, exento ISR e IMSS.
   - Agregar banderas operativas: automatico global, automatico liquidacion, especie e imprimir.
   - Validar clave agrupadora SAT para percepciones/deducciones que integran el recibo.

6. Reporte de verificacion SAT
   - Conceptos sin clave agrupadora SAT.
   - Empleados sin RFC, CURP, regimen, NSS o registro patronal requerido.
   - Usar el reporte como prerequisito de autorizacion/timbrado.

7. Calculo invertido
   - Capturar neto deseado por empleado.
   - Ajustar contra un concepto configurado para calculo invertido.
   - Recalcular percepciones/deducciones del recibo individual.

8. Finiquito
   - Registrar baja desde proceso de nomina.
   - Calcular separacion, vacaciones, prima vacacional, aguinaldo proporcional e indemnizaciones.
   - Excluir al empleado de periodos normales posteriores.

9. Autorizacion y cierre de nomina
   - Validar que todos los empleados del periodo esten calculados.
   - Bloquear cambios en periodos autorizados.
   - Acumular importes por empleado/concepto.
   - Avanzar periodo vigente.
   - Sugerir obligaciones/reportes fiscales cuando aplique fin de mes, bimestre o ejercicio.

10. Lista de raya y reportes exportables
    - Reporte por empleado, departamento y concepto.
    - Filtros por registro patronal, ejercicio, tipo de periodo, periodo, departamento y empleado.
    - Exportar a Excel, PDF y HTML.

11. CFDI nomina completo
    - Validar datos obligatorios antes de emitir.
    - Generar XML de nomina.
    - Integrar timbrado/PAC.
    - Estados: sin sellar, pendiente y sellado.
    - Bitacora de errores.
    - Envio masivo por correo.
    - Cancelacion de recibos.

## Orden recomendado de implementacion

1. Prenomina configurable + dias y horas.
2. Captura por tipo de incidencia + movimientos globales.
3. Validacion SAT + reporte de verificacion.
4. Autorizacion/cierre con acumulados.
5. Lista de raya/exportaciones.
6. Calculo invertido y finiquito.
7. CFDI nomina completo.
