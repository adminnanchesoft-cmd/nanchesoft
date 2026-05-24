(function () {
    'use strict';

    var STORAGE_KEY = 'nanchesoft.theme';
    var DX_CDN      = 'https://cdn3.devexpress.com/jslib/25.1.6/css/';

    // Map DevExtreme theme API name → CSS file name
    var DX_CSS = {
        'generic.light':               'dx.light.css',
        'generic.dark':                'dx.dark.css',
        'fluent.saas.light':           'dx.fluent.saas.light.css',
        'fluent.saas.dark':            'dx.fluent.saas.dark.css',
        'material.blue.light.compact': 'dx.material.blue.light.compact.css',
        'material.blue.dark.compact':  'dx.material.blue.dark.compact.css',
    };

    // ── Color helpers ─────────────────────────────────────────────────────────

    function isValidHex(v) {
        return v && /^#[0-9a-fA-F]{3}([0-9a-fA-F]{3})?$/.test((v + '').trim());
    }

    function hexToRgb(hex) {
        var c = hex.replace('#', '');
        if (c.length === 3) c = c[0]+c[0]+c[1]+c[1]+c[2]+c[2];
        return parseInt(c.slice(0,2),16)+','+parseInt(c.slice(2,4),16)+','+parseInt(c.slice(4,6),16);
    }

    function shiftHex(hex, factor) {
        var c = hex.replace('#','');
        if (c.length===3) c=c[0]+c[0]+c[1]+c[1]+c[2]+c[2];
        var r=parseInt(c.slice(0,2),16), g=parseInt(c.slice(2,4),16), b=parseInt(c.slice(4,6),16);
        if (factor > 0) {
            r=Math.max(0,Math.round(r*(1-factor)));
            g=Math.max(0,Math.round(g*(1-factor)));
            b=Math.max(0,Math.round(b*(1-factor)));
        } else {
            var f=-factor;
            r=Math.min(255,Math.round(r+(255-r)*f));
            g=Math.min(255,Math.round(g+(255-g)*f));
            b=Math.min(255,Math.round(b+(255-b)*f));
        }
        return '#'+r.toString(16).padStart(2,'0')+g.toString(16).padStart(2,'0')+b.toString(16).padStart(2,'0');
    }

    // ── DevExtreme theme switching ────────────────────────────────────────────

    function switchDxTheme(themeName) {
        var name = themeName || 'generic.light';

        // Método oficial: DevExpress.ui.themes.current() (disponible después de dx.all.js)
        if (typeof DevExpress !== 'undefined' && DevExpress.ui && DevExpress.ui.themes) {
            DevExpress.ui.themes.current(name);
            return;
        }

        // Fallback antiFlash (antes de que cargue dx.all.js): swap directo del CSS
        var cssFile = DX_CSS[name] || 'dx.light.css';
        var link = document.getElementById('dx-active-theme');
        if (link) {
            var href = DX_CDN + cssFile;
            if (link.href !== href) link.href = href;
        }
    }

    // ── Variables CSS del acento ──────────────────────────────────────────────

    function applyAccentVars(accent, secondary, background) {
        var root = document.documentElement;

        if (isValidHex(accent)) {
            var a = accent.trim();
            root.style.setProperty('--ns-accent',      a);
            root.style.setProperty('--ns-accent-end',  shiftHex(a, 0.14));
            root.style.setProperty('--ns-accent-soft', shiftHex(a, -0.88));
            root.style.setProperty('--ns-accent-rgb',  hexToRgb(a));
        } else {
            ['--ns-accent','--ns-accent-end','--ns-accent-soft','--ns-accent-rgb']
                .forEach(function(v){ root.style.removeProperty(v); });
        }

        if (isValidHex(secondary)) {
            var s = secondary.trim();
            root.style.setProperty('--ns-secondary',     s);
            root.style.setProperty('--ns-secondary-end', shiftHex(s, 0.14));
            root.style.setProperty('--ns-secondary-rgb', hexToRgb(s));
        } else {
            ['--ns-secondary','--ns-secondary-end','--ns-secondary-rgb']
                .forEach(function(v){ root.style.removeProperty(v); });
        }

        if (isValidHex(background)) {
            root.style.setProperty('--ns-background', background.trim());
        } else {
            root.style.removeProperty('--ns-background');
        }
    }

    // ── Familia de tema (para variables de layout claro/oscuro) ──────────────

    function applyThemeFamily(themeName) {
        var isDark = themeName && themeName.indexOf('dark') !== -1;
        document.documentElement.setAttribute('data-ns-dark', isDark ? '1' : '0');
    }

    // ── localStorage ──────────────────────────────────────────────────────────

    function save(theme, accent, secondary, background, autoMode) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify({
                t: theme, a: accent, s: secondary, b: background, auto: autoMode
            }));
        } catch(e){}
    }

    function load() {
        try { return JSON.parse(localStorage.getItem(STORAGE_KEY)); } catch(e){ return null; }
    }

    // ── AntiFlash (corre en <head> antes de renderizar) ───────────────────────
    (function antiFlash() {
        var p = load();
        if (!p) return;
        switchDxTheme(p.t || 'generic.light');
        applyAccentVars(p.a || '', p.s || '', p.b || '');
        applyThemeFamily(p.t || '');
    })();

    // ── API pública ───────────────────────────────────────────────────────────
    window.nsTheme = {

        // Aplica y persiste el tema completo
        apply: function(themeName, accent, secondary, background, autoMode) {
            var t = themeName || 'generic.light';
            switchDxTheme(t);
            applyAccentVars(accent || '', secondary || '', background || '');
            applyThemeFamily(t);
            save(t, accent || '', secondary || '', background || '', !!autoMode);
        },

        // Solo actualiza colores de acento (sin cambiar la familia de tema DX)
        applyAccent: function(accent, secondary, background) {
            applyAccentVars(accent || '', secondary || '', background || '');
        },

        // Reinicia al tema por defecto
        reset: function() {
            try { localStorage.removeItem(STORAGE_KEY); } catch(e){}
            switchDxTheme('generic.light');
            applyAccentVars('', '', '');
            applyThemeFamily('generic.light');
            document.documentElement.removeAttribute('data-ns-dark');
        },

        getStored: function() { return load(); }
    };

})();
