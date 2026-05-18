window.nsCrudGrid = (() => {
    const instances = {};

    function pick(obj, camel, pascal, fallback = undefined) {
        if (!obj) return fallback;
        if (obj[camel] !== undefined) return obj[camel];
        if (obj[pascal] !== undefined) return obj[pascal];
        return fallback;
    }

    function getColumns(definition) {
        return pick(definition, "columns", "Columns", []);
    }

    function getRows(definition) {
        return pick(definition, "rows", "Rows", []);
    }

    function getKeyExpr(definition) {
        return pick(definition, "keyExpr", "KeyExpr", "Id");
    }

    function getCatalogKey(definition) {
        return pick(definition, "catalogKey", "CatalogKey", "catalog");
    }

    function getTitle(definition) {
        return pick(definition, "title", "Title", "Catálogo");
    }

    function getAllowCreate(definition) {
        return pick(definition, "allowCreate", "AllowCreate", true);
    }

    function getAllowUpdate(definition) {
        return pick(definition, "allowUpdate", "AllowUpdate", true);
    }

    function getAllowDelete(definition) {
        return pick(definition, "allowDelete", "AllowDelete", true);
    }

    function getNewUrl(definition) {
        return pick(definition, "newUrl", "NewUrl", null);
    }

    function getEditUrl(definition) {
        return pick(definition, "editUrl", "EditUrl", null);
    }

    function getMetadata(definition) {
        return pick(definition, "metadata", "Metadata", {});
    }

    function getMetadataArray(definition, key) {
        const metadata = getMetadata(definition);
        const value = metadata?.[key];
        return Array.isArray(value) ? value : [];
    }

    function getColumnValue(col, camel, pascal, fallback = undefined) {
        return pick(col, camel, pascal, fallback);
    }

    function getLookupItems(col) {
        return pick(col, "lookupItems", "LookupItems", []);
    }

    function getLookupItemValue(item, camel, pascal, fallback = undefined) {
        return pick(item, camel, pascal, fallback);
    }

    function getResultSuccess(result) {
        return pick(result, "success", "Success", false);
    }

    function getResultError(result) {
        return pick(result, "error", "Error", "Ocurrió un error.");
    }

    function getResultDefinition(result) {
        return pick(result, "definition", "Definition", null);
    }

    function normalizeId(value) {
        return String(value ?? "").trim().toLowerCase();
    }

    function parseDateValue(value) {
        if (!value) return null;
        const date = value instanceof Date ? value : new Date(value);
        return Number.isNaN(date.getTime()) ? null : date;
    }

    function toNumber(value) {
        if (value === null || value === undefined || value === "") return null;
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : null;
    }

    function ensurePopupStyles() {
        if (document.getElementById("ns-crud-grid-popup-style")) {
            return;
        }

        const style = document.createElement("style");
        style.id = "ns-crud-grid-popup-style";
        style.textContent = `
            .ns-crud-popup-wrapper .dx-overlay-content {
                max-height: 92vh !important;
            }
            .ns-crud-popup-wrapper .dx-popup-content {
                overflow-y: auto !important;
                padding-bottom: 12px;
            }
            .ns-crud-popup-wrapper .dx-popup-bottom {
                position: sticky;
                bottom: 0;
                background: #fff;
                border-top: 1px solid #e5e7eb;
                z-index: 5;
            }
            .ns-qc-plus-btn {
                flex-shrink: 0;
                width: 36px;
                height: 36px;
                border: 1px solid #cbd5e1;
                background: #f8fafc;
                border-radius: 8px;
                cursor: pointer;
                font-size: 20px;
                font-weight: 700;
                color: #1d4ed8;
                display: flex;
                align-items: center;
                justify-content: center;
                transition: background .15s, border-color .15s;
                padding: 0;
                line-height: 1;
            }
            .ns-qc-plus-btn:hover {
                background: #eff6ff;
                border-color: #93c5fd;
            }
            .ns-qc-wrapper {
                display: flex;
                gap: 6px;
                align-items: center;
            }
            .ns-qc-wrapper > div:first-child {
                flex: 1;
                min-width: 0;
            }
        `;

        document.head.appendChild(style);
    }

    function showQuickCreateDialog(title, onSave) {
        const overlay = document.createElement("div");
        overlay.style.cssText = "position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:10000;display:flex;align-items:center;justify-content:center;padding:16px;";

        const dialog = document.createElement("div");
        dialog.style.cssText = "background:#fff;border-radius:14px;padding:24px 28px;max-width:380px;width:100%;box-shadow:0 16px 48px rgba(0,0,0,.22);font-family:'Segoe UI',sans-serif;";

        dialog.innerHTML = [
            `<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">`,
            `  <h3 style="margin:0;font-size:16px;font-weight:700;color:#0f172a;">${title}</h3>`,
            `  <button class="qc-x" style="background:none;border:none;font-size:22px;cursor:pointer;color:#94a3b8;line-height:1;">×</button>`,
            `</div>`,
            `<div class="qc-error" style="display:none;margin-bottom:12px;padding:8px 12px;border-radius:8px;background:#fff1f2;color:#991b1b;font-size:13px;border:1px solid #fecdd3;"></div>`,
            `<div style="margin-bottom:12px;">`,
            `  <label style="display:block;font-size:11px;font-weight:700;color:#475569;margin-bottom:4px;letter-spacing:.4px;text-transform:uppercase;">Código *</label>`,
            `  <input class="qc-code" type="text" style="width:100%;box-sizing:border-box;padding:8px 12px;border-radius:8px;border:1px solid #cbd5e1;font-size:14px;" autocomplete="off"/>`,
            `</div>`,
            `<div style="margin-bottom:20px;">`,
            `  <label style="display:block;font-size:11px;font-weight:700;color:#475569;margin-bottom:4px;letter-spacing:.4px;text-transform:uppercase;">Nombre *</label>`,
            `  <input class="qc-name" type="text" style="width:100%;box-sizing:border-box;padding:8px 12px;border-radius:8px;border:1px solid #cbd5e1;font-size:14px;" autocomplete="off"/>`,
            `</div>`,
            `<div style="display:flex;gap:10px;justify-content:flex-end;">`,
            `  <button class="qc-cancel" style="padding:8px 18px;border-radius:8px;border:1px solid #cbd5e1;background:#fff;color:#475569;cursor:pointer;font-size:14px;">Cancelar</button>`,
            `  <button class="qc-save" style="padding:8px 18px;border-radius:8px;border:none;background:#1d4ed8;color:#fff;cursor:pointer;font-size:14px;font-weight:600;">Crear</button>`,
            `</div>`
        ].join("");

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        const codeInput = dialog.querySelector(".qc-code");
        const nameInput = dialog.querySelector(".qc-name");
        const errorDiv  = dialog.querySelector(".qc-error");
        const saveBtn   = dialog.querySelector(".qc-save");
        const cancelBtn = dialog.querySelector(".qc-cancel");
        const xBtn      = dialog.querySelector(".qc-x");

        setTimeout(() => codeInput.focus(), 60);

        const close = () => { if (overlay.parentNode) document.body.removeChild(overlay); };
        cancelBtn.addEventListener("click", close);
        xBtn.addEventListener("click", close);
        overlay.addEventListener("click", e => { if (e.target === overlay) close(); });

        const doSave = async () => {
            const code = codeInput.value.trim();
            const name = nameInput.value.trim();
            if (!code || !name) {
                errorDiv.textContent = "Código y nombre son obligatorios.";
                errorDiv.style.display = "block";
                return;
            }
            saveBtn.disabled = true;
            saveBtn.textContent = "Guardando…";
            errorDiv.style.display = "none";
            try {
                await onSave(code, name);
                close();
            } catch (err) {
                errorDiv.textContent = err.message || "Error al crear el registro.";
                errorDiv.style.display = "block";
                saveBtn.disabled = false;
                saveBtn.textContent = "Crear";
            }
        };

        saveBtn.addEventListener("click", doSave);
        nameInput.addEventListener("keydown", e => { if (e.key === "Enter") doSave(); });
    }

    function disposeCrudGrid(containerId) {
        const current = instances[containerId];
        if (current?.grid) {
            try {
                current.grid.dispose();
            } catch {
            }
        }

        const host = document.getElementById(containerId);
        if (host) {
            host.innerHTML = "";
        }

        delete instances[containerId];
    }

    function renderCrudGrid(containerId, definition, dotNetRef) {
        ensurePopupStyles();
        disposeCrudGrid(containerId);

        const host = document.getElementById(containerId);
        if (!host) {
            throw new Error(`No se encontró el contenedor '${containerId}'.`);
        }

        if (typeof DevExpress === "undefined" || !DevExpress.ui || !DevExpress.ui.dxDataGrid) {
            throw new Error("DevExtreme no está cargado. Revisa App.razor.");
        }

        const grid = new DevExpress.ui.dxDataGrid(
            host,
            buildGridOptions(containerId, definition, dotNetRef)
        );

        instances[containerId] = {
            grid,
            definition,
            dotNetRef
        };
    }

    function buildGridOptions(containerId, definition, dotNetRef) {
        const columns = getColumns(definition);
        const rows = getRows(definition);
        const keyExpr = getKeyExpr(definition);
        const title = getTitle(definition);
        const catalogKey = getCatalogKey(definition);
        const popupWidth = Math.max(760, Math.min(window.innerWidth - 32, 1100));
        const popupHeight = Math.max(620, Math.min(window.innerHeight - 32, 900));
        const allowCreate = getAllowCreate(definition);
        const allowUpdate = getAllowUpdate(definition);
        const allowDelete = getAllowDelete(definition);
        const newUrl = getNewUrl(definition);
        const editUrl = getEditUrl(definition);

        const editableColumns = columns.filter(x =>
            getColumnValue(x, "visible", "Visible", true) !== false &&
            getColumnValue(x, "allowEditing", "AllowEditing", true) &&
            getColumnValue(x, "dataField", "DataField", "") !== keyExpr
        );

        return {
            dataSource: Array.isArray(rows) ? rows : [],
            keyExpr: keyExpr,
            showBorders: true,
            rowAlternationEnabled: true,
            hoverStateEnabled: true,
            columnAutoWidth: true,
            allowColumnResizing: true,
            columnResizingMode: "widget",
            columnFixing: { enabled: true },
            repaintChangesOnly: false,
            height: 660,

            sorting: { mode: "multiple" },
            filterRow: { visible: true, applyFilter: "auto" },
            headerFilter: { visible: true },
            filterPanel: { visible: true },
            searchPanel: {
                visible: true,
                width: 280,
                placeholder: "Buscar en todos los campos..."
            },
            groupPanel: { visible: true, emptyPanelText: "Arrastra una columna aquí para agrupar" },
            grouping: { autoExpandAll: true },
            selection: { mode: "single" },

            paging: { pageSize: 12 },
            pager: {
                visible: true,
                allowedPageSizes: [12, 20, 40, 100],
                showPageSizeSelector: true,
                showNavigationButtons: true,
                showInfo: true
            },

            stateStoring: {
                enabled: true,
                type: "localStorage",
                storageKey: `nanchesoft:${catalogKey}:grid-state:v6`
            },

            columns: buildColumns(columns, definition, dotNetRef),

            editing: {
                mode: "popup",
                allowAdding: allowCreate && !newUrl,
                allowUpdating: allowUpdate && !editUrl,
                allowDeleting: allowDelete,
                useIcons: true,
                popup: {
                    title: `${title} · captura`,
                    showTitle: true,
                    width: popupWidth,
                    height: popupHeight,
                    maxWidth: popupWidth,
                    maxHeight: popupHeight,
                    dragEnabled: false,
                    resizeEnabled: true,
                    wrapperAttr: { class: "ns-crud-popup-wrapper" },
                    onShown(e) {
                        configurePopupLayout(e.component);
                        const form = getPopupForm(e.component);
                        if (form) {
                            refreshDependentLookups(form, definition);
                            applyServiceNoteDefaults(form, definition, true);
                        }
                    }
                },
                form: {
                    colCount: 2,
                    labelLocation: "top",
                    items: editableColumns.map(col => buildFormItem(col, definition, dotNetRef)),
                    onFieldDataChanged(e) {
                        refreshDependentLookups(e.component, definition);
                        applyServiceNoteDefaults(e.component, definition, false, e.dataField);
                    }
                }
            },

            toolbar: {
                items: [
                    {
                        location: "before",
                        template() {
                            const el = document.createElement("div");
                            el.className = "ns-grid-toolbar-title";
                            el.textContent = title;
                            return el;
                        }
                    },
                    ...(allowCreate && newUrl ? [{
                        location: "after",
                        widget: "dxButton",
                        options: {
                            text: "Nuevo",
                            icon: "add",
                            type: "success",
                            stylingMode: "contained",
                            onClick: () => dotNetRef.invokeMethodAsync("HandleNavigateTo", newUrl)
                        }
                    }] : allowCreate ? [{
                        name: "addRowButton",
                        location: "after",
                        showText: "always",
                        options: {
                            text: "Nuevo",
                            icon: "add",
                            type: "success",
                            stylingMode: "contained"
                        }
                    }] : []),
                    "searchPanel",
                    {
                        location: "after",
                        widget: "dxButton",
                        options: {
                            text: "Recargar",
                            icon: "refresh",
                            type: "default",
                            stylingMode: "contained",
                            onClick: async () => {
                                const result = await dotNetRef.invokeMethodAsync("HandleRefresh");
                                renderCrudGrid(containerId, result, dotNetRef);
                                notify("Catálogo recargado.", "success");
                            }
                        }
                    }
                ]
            },

            onInitNewRow(e) {
                const hasPascalActive = columns.some(x => getColumnValue(x, "dataField", "DataField", "") === "IsActive");
                const hasCamelActive = columns.some(x => getColumnValue(x, "dataField", "DataField", "") === "isActive");

                if (hasPascalActive) {
                    e.data.IsActive = true;
                }
                if (hasCamelActive) {
                    e.data.isActive = true;
                }

                applySingleLookupDefaults(e.data, columns, definition);
                if (catalogKey === "service-notes") {
                    if (!e.data.NoteDate) {
                        e.data.NoteDate = new Date();
                    }
                }
                if (catalogKey === "hr-work-schedules") {
                    e.data.Monday = true;
                    e.data.Tuesday = true;
                    e.data.Wednesday = true;
                    e.data.Thursday = true;
                    e.data.Friday = true;
                }
            },

            onEditingStart(e) {
                if (catalogKey === "service-notes") {
                    applySingleLookupDefaults(e.data, columns, definition);
                }
            },

            onSaving(e) {
                if (!e.changes || e.changes.length === 0) {
                    return;
                }

                e.cancel = true;
                const change = e.changes[0];
                e.promise = handleSave(containerId, dotNetRef, change);
            },

            noDataText: "No hay registros para mostrar."
        };
    }

    function buildColumns(columns, definition, dotNetRef) {
        const allowUpdate = getAllowUpdate(definition);
        const allowDelete = getAllowDelete(definition);
        const editUrl = getEditUrl(definition);
        const keyExpr = getKeyExpr(definition);
        const mapped = columns
            .filter(x => getColumnValue(x, "showInGrid", "ShowInGrid", true) !== false && getColumnValue(x, "dataField", "DataField", "") !== keyExpr)
            .map(col => {
                const dataType = getColumnValue(col, "dataType", "DataType", "string");
                const useLookup = getColumnValue(col, "useLookup", "UseLookup", false);
                const lookupItems = getLookupItems(col);
                const dataField = getColumnValue(col, "dataField", "DataField", "");

                const dxColumn = {
                    dataField: dataField,
                    caption: getColumnValue(col, "caption", "Caption", ""),
                    dataType: mapDataType(dataType),
                    width: getColumnValue(col, "width", "Width", 160),
                    allowEditing: getColumnValue(col, "allowEditing", "AllowEditing", true),
                    allowFiltering: getColumnValue(col, "allowFiltering", "AllowFiltering", true),
                    allowSorting: getColumnValue(col, "allowSorting", "AllowSorting", true)
                };

                if (String(dataType).toLowerCase() === "boolean") {
                    dxColumn.alignment = "center";
                }

                if (String(dataType).toLowerCase() === "number") {
                    dxColumn.format = { type: "fixedPoint", precision: 2 };
                }

                if (String(dataType).toLowerCase() === "date") {
                    dxColumn.format = "dd/MM/yyyy";
                }

                if (String(dataType).toLowerCase() === "time") {
                    dxColumn.dataType = "string";
                    dxColumn.formItem = {
                        editorType: "dxDateBox",
                        editorOptions: {
                            type: "time",
                            displayFormat: "HH:mm",
                            pickerType: "list",
                            interval: 15,
                            useMaskBehavior: true,
                            onValueChanged(ev) {
                                if (ev.value instanceof Date) {
                                    const h = String(ev.value.getHours()).padStart(2, "0");
                                    const m = String(ev.value.getMinutes()).padStart(2, "0");
                                    ev.component.option("value", `${h}:${m}`);
                                }
                            }
                        }
                    };
                }

                if (useLookup && Array.isArray(lookupItems) && lookupItems.length > 0) {
                    dxColumn.lookup = {
                        dataSource: lookupItems.map(x => ({
                            id: getLookupItemValue(x, "id", "Id", ""),
                            name: getLookupItemValue(x, "name", "Name", "")
                        })),
                        valueExpr: "id",
                        displayExpr: "name"
                    };
                }

                return dxColumn;
            });

        const commandButtons = [];
        if (allowUpdate && editUrl) {
            commandButtons.push({
                hint: "Editar",
                icon: "edit",
                onClick(e) {
                    const key = e.row?.data?.[keyExpr];
                    if (key) dotNetRef.invokeMethodAsync("HandleNavigateTo", `${editUrl}/${key}`);
                }
            });
        } else if (allowUpdate) {
            commandButtons.push("edit");
        }
        if (allowDelete) commandButtons.push("delete");

        if (commandButtons.length > 0) {
            mapped.unshift({
                type: "buttons",
                caption: "Acciones",
                width: 96,
                minWidth: 96,
                fixed: true,
                fixedPosition: "left",
                alignment: "center",
                cssClass: "ns-grid-command-column",
                buttons: commandButtons
            });
        }

        return mapped;
    }

    function buildFormItem(col, definition, dotNetRef) {
        const caption = getColumnValue(col, "caption", "Caption", "");
        const dataField = getColumnValue(col, "dataField", "DataField", "");
        const dataType = getColumnValue(col, "dataType", "DataType", "string");
        const useLookup = getColumnValue(col, "useLookup", "UseLookup", false);
        const lookupItems = getLookupItems(col);
        const required = getColumnValue(col, "required", "Required", false);
        const quickCreateKey = getColumnValue(col, "quickCreateKey", "QuickCreateKey", null);

        const item = {
            dataField: dataField,
            label: { text: caption },
            colSpan: 1
        };

        if (useLookup && Array.isArray(lookupItems)) {
            const normalizedItems = lookupItems.map(x => ({
                id: getLookupItemValue(x, "id", "Id", ""),
                name: getLookupItemValue(x, "name", "Name", "")
            }));

            if (quickCreateKey && dotNetRef) {
                // Custom template: dxSelectBox + "+" button
                item.template = function(data, itemElement) {
                    const wrapper = document.createElement("div");
                    wrapper.className = "ns-qc-wrapper";

                    const selectDiv = document.createElement("div");

                    const itemsRef = [...normalizedItems];

                    const currentVal = (data.component?.option?.("formData") ?? {})[dataField] ?? null;

                    const sb = new DevExpress.ui.dxSelectBox(selectDiv, {
                        dataSource: itemsRef,
                        valueExpr: "id",
                        displayExpr: "name",
                        searchEnabled: true,
                        showClearButton: !required,
                        deferRendering: false,
                        value: currentVal,
                        onValueChanged(e) {
                            data.component?.updateData?.(dataField, e.value ?? null);
                        }
                    });

                    const btn = document.createElement("button");
                    btn.type = "button";
                    btn.className = "ns-qc-plus-btn";
                    btn.title = `Crear ${caption}`;
                    btn.textContent = "+";

                    btn.addEventListener("click", function(e) {
                        e.preventDefault();
                        e.stopPropagation();
                        showQuickCreateDialog(`Nuevo: ${caption}`, async function(code, name) {
                            const result = await dotNetRef.invokeMethodAsync("HandleQuickCreate", quickCreateKey, code, name);
                            if (!result.success) throw new Error(result.error || "Error al crear.");
                            const newItems = (result.allItems || []).map(x => ({
                                id: x.id || x.Id || "",
                                name: x.name || x.Name || ""
                            }));
                            // Update column definition so refreshes also see new items
                            if (col.LookupItems) col.LookupItems = result.allItems;
                            if (col.lookupItems) col.lookupItems = result.allItems;
                            itemsRef.length = 0;
                            newItems.forEach(x => itemsRef.push(x));
                            sb.option("dataSource", [...newItems]);
                            if (result.newId) {
                                sb.option("value", result.newId);
                                data.component?.updateData?.(dataField, result.newId);
                            }
                        });
                    });

                    wrapper.appendChild(selectDiv);
                    wrapper.appendChild(btn);

                    const el = itemElement instanceof HTMLElement ? itemElement : itemElement?.[0];
                    if (el) el.appendChild(wrapper);
                };
            } else {
                item.editorType = "dxSelectBox";
                item.editorOptions = {
                    dataSource: normalizedItems,
                    valueExpr: "id",
                    displayExpr: "name",
                    searchEnabled: true,
                    showClearButton: false,
                    deferRendering: false
                };
            }
        } else if (String(dataType).toLowerCase() === "boolean") {
            item.editorType = "dxSwitch";
        } else if (String(dataType).toLowerCase() === "number") {
            item.editorType = "dxNumberBox";
            item.editorOptions = {
                showSpinButtons: true,
                format: "#,##0.00"
            };
        }

        if (required) {
            item.validationRules = [
                {
                    type: "required",
                    message: `${caption} es obligatorio.`
                }
            ];
        }

        return item;
    }

    function configurePopupLayout(popup) {
        if (!popup) return;
        const content = popup.content && popup.content();
        if (content) {
            content.style.overflowY = "auto";
            content.style.maxHeight = `${Math.max(420, Math.min(window.innerHeight - 140, 760))}px`;
        }
    }

    function getPopupForm(popup) {
        const content = popup?.content && popup.content();
        if (!content) return null;
        const formElement = content.querySelector(".dx-form");
        if (!formElement || !DevExpress?.ui?.dxForm?.getInstance) return null;
        return DevExpress.ui.dxForm.getInstance(formElement);
    }

    function setEditorDataSource(form, fieldName, items) {
        const editor = form.getEditor(fieldName);
        if (!editor) return;
        editor.option("dataSource", items);
    }

    function applySingleLookupDefaults(formData, columns, definition) {
        for (const column of columns) {
            if (!getColumnValue(column, "useLookup", "UseLookup", false)) continue;
            const fieldName = getColumnValue(column, "dataField", "DataField", "");
            if (!fieldName || formData[fieldName]) continue;
            const items = getLookupItems(column);
            if (Array.isArray(items) && items.length === 1) {
                formData[fieldName] = getLookupItemValue(items[0], "id", "Id", "");
            }
        }

        const companies = getMetadataArray(definition, "companies");
        if (!formData.CompanyId && formData.TenantId) {
            const tenantCompanies = companies.filter(x => normalizeId(x.tenantId) === normalizeId(formData.TenantId));
            if (tenantCompanies.length === 1) {
                formData.CompanyId = tenantCompanies[0].id;
            }
        }
    }

    function refreshDependentLookups(form, definition) {
        if (!form || getCatalogKey(definition) !== "service-notes") {
            return;
        }

        const data = form.option("formData") || {};
        const tenantId = normalizeId(data.TenantId);
        const companyId = normalizeId(data.CompanyId);

        const companies = getMetadataArray(definition, "companies");
        const customers = getMetadataArray(definition, "customers");
        const services = getMetadataArray(definition, "services");

        const companyOptions = companies
            .filter(x => !tenantId || normalizeId(x.tenantId) === tenantId)
            .map(x => ({ id: x.id, name: x.name }));
        setEditorDataSource(form, "CompanyId", companyOptions);

        if (!companyId && companyOptions.length === 1) {
            form.updateData("CompanyId", companyOptions[0].id);
        }

        const effectiveCompanyId = normalizeId((form.option("formData") || {}).CompanyId);
        const customerOptions = customers
            .filter(x => (!tenantId || normalizeId(x.tenantId) === tenantId) && (!effectiveCompanyId || normalizeId(x.companyId) === effectiveCompanyId))
            .map(x => ({ id: x.id, name: x.name }));
        setEditorDataSource(form, "CustomerId", customerOptions);

        const serviceOptions = services
            .filter(x => (!tenantId || normalizeId(x.tenantId) === tenantId) && (!effectiveCompanyId || normalizeId(x.companyId) === effectiveCompanyId))
            .map(x => ({ id: x.id, name: x.name }));
        setEditorDataSource(form, "ServiceCatalogItemId", serviceOptions);
    }

    function resolveHourlyRate(formData, definition) {
        const customerId = normalizeId(formData.CustomerId);
        const serviceCatalogItemId = normalizeId(formData.ServiceCatalogItemId);
        const noteDate = parseDateValue(formData.NoteDate);
        const tenantId = normalizeId(formData.TenantId);
        const companyId = normalizeId(formData.CompanyId);

        if (!serviceCatalogItemId) {
            return null;
        }

        const rates = getMetadataArray(definition, "rates");
        const matchingRates = rates
            .filter(x => x && x.isActive !== false)
            .filter(x => !customerId || normalizeId(x.customerId) === customerId)
            .filter(x => normalizeId(x.serviceCatalogItemId) === serviceCatalogItemId)
            .filter(x => !tenantId || normalizeId(x.tenantId) === tenantId)
            .filter(x => !companyId || normalizeId(x.companyId) === companyId)
            .filter(x => {
                if (!noteDate) return true;
                const from = parseDateValue(x.effectiveFrom);
                const to = parseDateValue(x.effectiveTo);
                if (from && noteDate < from) return false;
                if (to && noteDate > to) return false;
                return true;
            })
            .sort((a, b) => {
                const aDate = parseDateValue(a.effectiveFrom)?.getTime() ?? 0;
                const bDate = parseDateValue(b.effectiveFrom)?.getTime() ?? 0;
                return bDate - aDate;
            });

        const negotiatedRate = toNumber(matchingRates[0]?.rate);
        if (negotiatedRate !== null) {
            return negotiatedRate;
        }

        const services = getMetadataArray(definition, "services");
        const service = services.find(x => normalizeId(x.serviceCatalogItemId || x.id) === serviceCatalogItemId);
        return toNumber(service?.defaultRate);
    }

    function applyServiceNoteDefaults(form, definition, force = false, changedField = "") {
        if (!form || getCatalogKey(definition) !== "service-notes") {
            return;
        }

        const data = form.option("formData") || {};
        if (!data.NoteDate) {
            form.updateData("NoteDate", new Date());
        }

        const watchedFields = ["CustomerId", "ServiceCatalogItemId", "NoteDate", "CompanyId", "TenantId"];
        if (!force && changedField && !watchedFields.includes(changedField)) {
            return;
        }

        const rate = resolveHourlyRate(form.option("formData") || {}, definition);
        if (rate !== null) {
            form.updateData("HourlyRate", rate);
        }
    }

    function mapDataType(dataType) {
        switch (String(dataType || "").toLowerCase()) {
            case "boolean":
                return "boolean";
            case "number":
                return "number";
            case "date":
                return "date";
            case "datetime":
                return "datetime";
            default:
                return "string";
        }
    }

    async function handleSave(containerId, dotNetRef, change) {
        let result = null;

        switch (change.type) {
            case "insert": {
                const insertData = change.data || {};
                result = await dotNetRef.invokeMethodAsync("HandleInsert", insertData);
                break;
            }

            case "update": {
                const mergedData = {
                    ...(change.oldData || {}),
                    ...(change.data || {})
                };

                result = await dotNetRef.invokeMethodAsync(
                    "HandleUpdate",
                    String(change.key),
                    mergedData
                );
                break;
            }

            case "remove": {
                result = await dotNetRef.invokeMethodAsync("HandleDelete", String(change.key));
                break;
            }

            default:
                return;
        }

        const success = getResultSuccess(result);
        const error = getResultError(result);
        const definition = getResultDefinition(result);

        if (definition) {
            renderCrudGrid(containerId, definition, dotNetRef);
        }

        if (!success) {
            notify(error || "Ocurrió un error.", "error");
            return;
        }

        switch (change.type) {
            case "insert":
                notify("Registro creado correctamente.", "success");
                break;
            case "update":
                notify("Registro actualizado correctamente.", "success");
                break;
            case "remove":
                notify("Registro eliminado correctamente.", "success");
                break;
        }
    }

    function notify(message, type) {
        DevExpress.ui.notify({
            message,
            type,
            displayTime: 2600,
            width: 420
        });
    }

    return {
        renderCrudGrid,
        disposeCrudGrid
    };
})();
