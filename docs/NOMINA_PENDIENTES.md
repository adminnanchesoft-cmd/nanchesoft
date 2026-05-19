# Nomina - pendientes derivados de NOMINAS_Elemental.pdf

Referencia principal: `docs/NOMINAS_Elemental.pdf`.

Estado base validado:

- Captura individual de recibo: implementado.
- Captura matricial: implementado.
- Nomina periodica basica: implementado.
- Prestamos y deducciones: implementado.
- Importacion de checadas CSV/Silvasoft: implementado.
- Retardos y faltas configurables: implementado como base operativa en proceso de nomina.

## Prioridad siguiente

1. Prenomina configurable
   - Configurar columnas por periodo/usuario.
   - Elegir conceptos de percepcion, deduccion y obligacion que aparecen en matriz.
   - Guardar configuracion base y permitir configuracion especial por periodo.
   - Copiar importes a toda una columna.
   - Importar/exportar movimientos de prenomina.

2. Dias y horas tipo CONTPAQi
   - Captura diaria por empleado y dia del periodo.
   - Soportar claves/mnemonicos: `FINJ`, `RET`, `HE1..HE5`, permisos, incapacidades, castigos y dias trabajados.
   - Integrar estas claves con incidencias y calculo de nomina.

3. Captura por tipo de incidencia
   - Seleccionar una incidencia, fecha y unidades por omision.
   - Agregar multiples empleados en una sola captura.
   - Permitir ajustar unidades/fecha por empleado antes de guardar.

4. Movimientos globales
   - Definir filtros por departamento, puesto, empleado u otros criterios.
   - Aplicar conceptos a grupos de empleados.
   - Manejar fecha inicio, veces a aplicar, monto limite, acumulado, estado y numero de control.
   - Reflejar el movimiento como permanente por empleado cuando corresponda.

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
