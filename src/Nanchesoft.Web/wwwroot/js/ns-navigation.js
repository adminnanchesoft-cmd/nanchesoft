window.nsNavigation = (() => {
    const registry = new Map();

    function init(searchElementId, treeElementId, dotNetRef, items, currentRoute) {
        if (!window.DevExpress || !window.jQuery) {
            return;
        }

        const searchElement = document.getElementById(searchElementId);
        const treeElement = document.getElementById(treeElementId);
        if (!searchElement || !treeElement) {
            return;
        }

        dispose(treeElementId);

        const $ = window.jQuery;
        const treeInstance = $(treeElement).dxTreeView({
            items,
            keyExpr: "id",
            dataStructure: "tree",
            selectionMode: "single",
            selectByClick: true,
            activeStateEnabled: false,
            focusStateEnabled: false,
            hoverStateEnabled: true,
            expandNodesRecursive: false,
            noDataText: "No hay opciones",
            animationEnabled: true,
            searchEnabled: true,
            searchExpr: ["text", "module", "badge"],
            searchMode: "contains",
            itemTemplate(itemData, itemIndex, itemElement) {
                renderItem(itemData, itemElement);
            },
            onItemClick(e) {
                if (!e.itemData || !e.itemData.route) {
                    // Group node: DX toggles expansion automatically — no manual call needed
                    return;
                }

                dotNetRef.invokeMethodAsync("NavigateTo", e.itemData.route).catch(function () {});
            },
            onContentReady(e) {
                syncSelection(e.component, items, currentRoute);
            }
        }).dxTreeView("instance");

        const searchInstance = $(searchElement).dxTextBox({
            mode: "search",
            placeholder: "Buscar pantalla, módulo o atajo...",
            stylingMode: "outlined",
            valueChangeEvent: "keyup input",
            buttons: [{
                name: "search",
                location: "before",
                options: {
                    icon: "search",
                    stylingMode: "text"
                }
            }],
            onValueChanged(e) {
                const value = e.value || "";
                treeInstance.option("searchValue", value);
                if (value.trim().length > 0) {
                    treeInstance.expandAll();
                } else {
                    restoreExpandedState(treeInstance, items);
                    syncSelection(treeInstance, items, currentRoute);
                }
            }
        }).dxTextBox("instance");

        registry.set(treeElementId, { treeInstance, searchInstance });
        syncSelection(treeInstance, items, currentRoute);
    }

    function renderItem(itemData, itemElement) {
        const isGroup = !itemData.route;
        const host = document.createElement("div");
        host.className = isGroup ? "ns-nav-tree-item ns-nav-tree-item--group" : "ns-nav-tree-item ns-nav-tree-item--leaf";

        const indicator = document.createElement("span");
        indicator.className = "ns-nav-tree-item__indicator";
        indicator.setAttribute("aria-hidden", "true");
        host.appendChild(indicator);

        const left = document.createElement("div");
        left.className = "ns-nav-tree-item__left";

        const icon = document.createElement("span");
        icon.className = isGroup ? "ns-nav-tree-item__icon ns-nav-tree-item__icon--group" : "ns-nav-tree-item__icon";
        icon.textContent = itemData.icon || (isGroup ? "NS" : "·");

        const title = document.createElement("span");
        title.className = "ns-nav-tree-item__title";
        title.textContent = itemData.text || "";

        left.appendChild(icon);
        left.appendChild(title);
        host.appendChild(left);

        const right = document.createElement("div");
        right.className = "ns-nav-tree-item__right";

        if (itemData.badge) {
            const badge = document.createElement("span");
            badge.className = "ns-nav-tree-item__badge";
            badge.textContent = itemData.badge;
            right.appendChild(badge);
        }

        if (isGroup) {
            const chevron = document.createElement("span");
            chevron.className = "ns-nav-tree-item__chevron";
            chevron.innerHTML = '<svg viewBox="0 0 24 24" width="14" height="14" aria-hidden="true"><path d="M9 6 L15 12 L9 18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>';
            right.appendChild(chevron);
        }

        host.appendChild(right);
        itemElement.append(host);
    }

    function syncSelection(treeInstance, items, currentRoute) {
        if (!treeInstance || !currentRoute) {
            return;
        }

        const selectedId = findNodeIdByRoute(items, currentRoute);
        const parentId = findParentIdByRoute(items, currentRoute);

        if (parentId) {
            treeInstance.expandItem(parentId);
        }

        if (selectedId) {
            treeInstance.selectItem(selectedId);
        }
    }

    function findNodeIdByRoute(nodes, currentRoute) {
        for (const node of nodes || []) {
            if (node.route && normalize(node.route) === normalize(currentRoute)) {
                return node.id;
            }

            const nested = findNodeIdByRoute(node.items, currentRoute);
            if (nested) {
                return nested;
            }
        }

        return null;
    }

    function findParentIdByRoute(nodes, currentRoute) {
        for (const node of nodes || []) {
            if (Array.isArray(node.items) && node.items.some(child => child.route && normalize(child.route) === normalize(currentRoute))) {
                return node.id;
            }

            const nested = findParentIdByRoute(node.items, currentRoute);
            if (nested) {
                return nested;
            }
        }

        return null;
    }

    function restoreExpandedState(treeInstance, nodes) {
        for (const node of nodes || []) {
            if (!node.id || !Array.isArray(node.items) || node.items.length === 0) {
                continue;
            }

            if (node.expanded) {
                treeInstance.expandItem(node.id);
            } else {
                treeInstance.collapseItem(node.id);
            }
        }
    }

    function normalize(value) {
        if (!value) {
            return "/dashboard";
        }

        let normalized = String(value).trim().toLowerCase();
        if (!normalized.startsWith("/")) {
            normalized = `/${normalized}`;
        }

        if (normalized.length > 1 && normalized.endsWith("/")) {
            normalized = normalized.slice(0, -1);
        }

        return normalized;
    }

    function dispose(treeElementId) {
        const entry = registry.get(treeElementId);
        if (!entry) {
            return;
        }

        try {
            entry.treeInstance?.dispose?.();
        } catch {
        }

        try {
            entry.searchInstance?.dispose?.();
        } catch {
        }

        registry.delete(treeElementId);
    }

    return {
        init
    };
})();
