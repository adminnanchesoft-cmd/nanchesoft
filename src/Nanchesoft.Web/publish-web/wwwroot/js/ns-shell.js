window.nsShell = (() => {
    const registry = new Map();
    let keyboardInstalled = false;

    function init(hostId, dotNetRef, items, currentRoute) {
        if (!window.jQuery || !window.DevExpress?.ui?.dxPopup) {
            return;
        }

        const host = document.getElementById(hostId);
        if (!host) {
            return;
        }

        const $ = window.jQuery;
        const normalizedItems = Array.isArray(items)
            ? items.map(item => ({
                title: item.title || item.Title || "",
                route: normalizeRoute(item.route || item.Route || ""),
                module: item.module || item.Module || "General",
                kind: item.kind || item.Kind || "Pantalla",
                badge: item.badge || item.Badge || "",
                keywords: item.keywords || item.Keywords || ""
            }))
            : [];

        let entry = registry.get(hostId);
        if (entry) {
            entry.dotNetRef = dotNetRef;
            entry.items = normalizedItems;
            entry.currentRoute = normalizeRoute(currentRoute);
            refresh(entry);
            return;
        }

        entry = {
            hostId,
            dotNetRef,
            items: normalizedItems,
            currentRoute: normalizeRoute(currentRoute),
            query: "",
            popup: null,
            search: null,
            list: null
        };

        entry.popup = $(host).dxPopup({
            width: () => Math.min(window.innerWidth - 48, 860),
            maxHeight: () => Math.min(window.innerHeight - 36, 720),
            showTitle: false,
            dragEnabled: false,
            resizeEnabled: false,
            shading: true,
            shadingColor: "rgba(15, 23, 42, 0.38)",
            hideOnOutsideClick: true,
            wrapperAttr: { class: "ns-command-popup-wrapper" },
            elementAttr: { class: "ns-command-popup" },
            contentTemplate(contentElement) {
                const shell = document.createElement("div");
                shell.className = "ns-command";

                const head = document.createElement("div");
                head.className = "ns-command__head";
                head.innerHTML = `
                    <div>
                        <div class="ns-command__eyebrow">Búsqueda global</div>
                        <div class="ns-command__title">Módulos, pantallas y acciones del ERP</div>
                    </div>
                    <div class="ns-command__hint">Esc</div>`;

                const searchHost = document.createElement("div");
                searchHost.className = "ns-command__search";

                const summary = document.createElement("div");
                summary.className = "ns-command__summary";

                const listHost = document.createElement("div");
                listHost.className = "ns-command__list";

                const footer = document.createElement("div");
                footer.className = "ns-command__footer";
                footer.innerHTML = `
                    <span>Enter para abrir</span>
                    <span>↑ ↓ para recorrer</span>
                    <span>/ o Ctrl + K para abrir</span>`;

                shell.append(head, searchHost, summary, listHost, footer);
                contentElement.append(shell);

                entry.search = $(searchHost).dxTextBox({
                    mode: "search",
                    placeholder: "Escribe: compras, clientes, kardex, impresión, notas de servicio...",
                    stylingMode: "filled",
                    valueChangeEvent: "keyup input",
                    buttons: [{
                        name: "search",
                        location: "before",
                        options: { icon: "search" }
                    }],
                    onValueChanged(e) {
                        entry.query = e.value || "";
                        refresh(entry);
                    },
                    onEnterKey() {
                        openFirst(entry);
                    }
                }).dxTextBox("instance");

                entry.list = $(listHost).dxList({
                    dataSource: [],
                    noDataText: "No hay coincidencias.",
                    keyExpr: "route",
                    focusStateEnabled: true,
                    activeStateEnabled: false,
                    pageLoadMode: "scrollBottom",
                    itemTemplate(itemData, itemIndex, itemElement) {
                        renderItem(itemData, itemElement);
                    },
                    onItemClick(e) {
                        if (e.itemData?.route) {
                            navigate(entry, e.itemData.route);
                        }
                    }
                }).dxList("instance");

                entry.summaryHost = summary;
                refresh(entry);
            },
            onShown() {
                window.setTimeout(() => entry.search?.focus(), 30);
            },
            onHiding() {
                entry.query = "";
                if (entry.search) {
                    entry.search.option("value", "");
                }
                refresh(entry);
            }
        }).dxPopup("instance");

        registry.set(hostId, entry);
        installKeyboard();
    }

    function renderItem(item, itemElement) {
        const wrapper = document.createElement("div");
        wrapper.className = "ns-command__item";

        const badge = document.createElement("div");
        badge.className = "ns-command__item-badge";
        badge.textContent = shortCode(item.module || item.title);

        const meta = document.createElement("div");
        meta.className = "ns-command__item-meta";

        const top = document.createElement("div");
        top.className = "ns-command__item-top";

        const title = document.createElement("div");
        title.className = "ns-command__item-title";
        title.textContent = item.title || "";

        const kind = document.createElement("div");
        kind.className = "ns-command__item-kind";
        kind.textContent = item.kind || "Pantalla";

        top.append(title, kind);

        const bottom = document.createElement("div");
        bottom.className = "ns-command__item-bottom";
        bottom.textContent = `${item.module || "General"} · ${item.route || "/"}`;

        meta.append(top, bottom);
        wrapper.append(badge, meta);
        itemElement.append(wrapper);
    }

    function refresh(entry) {
        if (!entry?.list) {
            return;
        }

        const data = rank(entry.items, entry.query, entry.currentRoute).slice(0, 14);
        entry.list.option("dataSource", data);

        if (entry.summaryHost) {
            if ((entry.query || "").trim().length === 0) {
                entry.summaryHost.textContent = "Sugerencias rápidas del ERP.";
            } else {
                entry.summaryHost.textContent = `${data.length} resultado(s) para “${entry.query.trim()}”.`;
            }
        }
    }

    function rank(items, query, currentRoute) {
        const q = normalizeText(query);
        const normalizedCurrent = normalizeRoute(currentRoute);

        const scored = (items || []).map(item => ({ item, score: scoreItem(item, q, normalizedCurrent) }))
            .filter(x => x.score > 0)
            .sort((a, b) => b.score - a.score || a.item.title.localeCompare(b.item.title, "es", { sensitivity: "base" }));

        return scored.map(x => x.item);
    }

    function scoreItem(item, query, currentRoute) {
        let score = 0;
        const title = normalizeText(item.title);
        const route = normalizeText(item.route);
        const module = normalizeText(item.module);
        const kind = normalizeText(item.kind);
        const keywords = normalizeText(item.keywords);

        if (!query) {
            score += item.kind === "Acción" ? 150 : 100;
            if (item.route === currentRoute) score += 30;
            if (module.includes("servicios") || route.includes("print-center")) score += 10;
            return score;
        }

        if (title === query) score += 400;
        if (title.startsWith(query)) score += 260;
        if (title.includes(query)) score += 190;
        if (module.startsWith(query)) score += 150;
        if (module.includes(query)) score += 120;
        if (route.includes(query)) score += 110;
        if (keywords.includes(query)) score += 95;
        if (kind.includes(query)) score += 60;

        const words = query.split(" ").filter(Boolean);
        for (const word of words) {
            if (title.startsWith(word)) score += 80;
            else if (title.includes(word)) score += 45;
            if (module.includes(word)) score += 28;
            if (route.includes(word)) score += 22;
            if (keywords.includes(word)) score += 18;
        }

        if (item.route === currentRoute) score += 20;
        return score;
    }

    function open(hostId) {
        const entry = resolveEntry(hostId);
        entry?.popup?.show();
    }

    function close(hostId) {
        const entry = resolveEntry(hostId);
        entry?.popup?.hide();
    }

    function openFirst(entry) {
        const items = entry.list?.option("dataSource") || [];
        if (Array.isArray(items) && items.length > 0) {
            navigate(entry, items[0].route);
        }
    }

    function navigate(entry, route) {
        if (!route || !entry?.dotNetRef) {
            return;
        }

        entry.popup?.hide();
        entry.dotNetRef.invokeMethodAsync("NavigateTo", route);
    }

    function resolveEntry(hostId) {
        if (hostId && registry.has(hostId)) {
            return registry.get(hostId);
        }

        const first = registry.values().next();
        return first.done ? null : first.value;
    }

    function updateItems(hostId, items, currentRoute) {
        const entry = registry.get(hostId);
        if (!entry) {
            return;
        }

        entry.items = Array.isArray(items) ? items : [];
        entry.currentRoute = normalizeRoute(currentRoute);
        refresh(entry);
    }

    function dispose(hostId) {
        const entry = registry.get(hostId);
        if (!entry) {
            return;
        }

        try {
            entry.popup?.dispose?.();
        } catch {
        }

        registry.delete(hostId);
    }

    function installKeyboard() {
        if (keyboardInstalled) {
            return;
        }

        keyboardInstalled = true;
        document.addEventListener("keydown", event => {
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
                event.preventDefault();
                open();
                return;
            }

            if (event.key === "/" && !isEditable(event.target)) {
                event.preventDefault();
                open();
                return;
            }

            if (event.key === "Escape") {
                close();
            }
        });
    }

    function isEditable(target) {
        if (!target) return false;
        const tag = target.tagName?.toLowerCase();
        return tag === "input" || tag === "textarea" || target.isContentEditable;
    }

    function normalizeText(value) {
        return String(value || "")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .trim();
    }

    function normalizeRoute(value) {
        let route = String(value || "").trim().toLowerCase();
        if (!route) return "/dashboard";
        if (!route.startsWith("/")) route = `/${route}`;
        if (route.length > 1 && route.endsWith("/")) route = route.slice(0, -1);
        return route;
    }

    function shortCode(text) {
        const parts = String(text || "NS").trim().split(/\s+/).filter(Boolean);
        if (parts.length === 0) return "NS";
        if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
        return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
    }

    return {
        init,
        open,
        close,
        updateItems,
        dispose
    };
})();
