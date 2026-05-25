(function () {
    'use strict';

    var _ref = null;

    // ── Global drag/paste intercept — attached immediately on script load ──────
    // This runs BEFORE the modal opens so the browser never navigates to a
    // dropped PDF file regardless of modal state.
    document.addEventListener('dragover', function (e) {
        if (e.dataTransfer && e.dataTransfer.types && e.dataTransfer.types.includes('Files')) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'copy';
        }
    });

    document.addEventListener('drop', function (e) {
        var file = e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0];
        if (!file) return;
        // Always prevent the browser from navigating to the dropped file
        e.preventDefault();
        // Only process if the modal is open (_ref set by init())
        if (!_ref) return;
        if (file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf')) {
            uploadPdf(file);
        }
    });

    document.addEventListener('paste', function (e) {
        if (!_ref || !e.clipboardData || !e.clipboardData.items) return;
        var items = e.clipboardData.items;
        for (var i = 0; i < items.length; i++) {
            if (items[i].kind === 'file') {
                var file = items[i].getAsFile();
                if (file && (file.type === 'application/pdf' || (file.name && file.name.toLowerCase().endsWith('.pdf')))) {
                    e.preventDefault();
                    uploadPdf(file);
                    return;
                }
            }
        }
    });

    // ── Helpers ───────────────────────────────────────────────────────────────

    async function uploadPdf(file) {
        if (!_ref) return;
        try {
            await _ref.invokeMethodAsync('SetParsing', true);
            var fd = new FormData();
            fd.append('file', file, file.name || 'constancia.pdf');
            var resp = await fetch('/api/third-parties/parse-constancia', { method: 'POST', body: fd });
            var json = await resp.json();
            if (resp.ok) {
                await _ref.invokeMethodAsync('ReceiveParsedData', JSON.stringify(json));
            } else {
                await _ref.invokeMethodAsync('ReceiveParseError', json.message || 'Error al procesar el PDF.');
            }
        } catch (err) {
            try { await _ref.invokeMethodAsync('ReceiveParseError', 'Error de conexión: ' + err.message); } catch (e2) { }
        }
    }

    async function processQrImage(file) {
        if (!_ref) return;
        try {
            await _ref.invokeMethodAsync('SetParsing', true);

            if (typeof jsQR === 'undefined') {
                await new Promise(function (resolve, reject) {
                    var s = document.createElement('script');
                    s.src = 'https://cdn.jsdelivr.net/npm/jsqr@1.4.0/dist/jsQR.js';
                    s.onload = resolve;
                    s.onerror = reject;
                    document.head.appendChild(s);
                });
            }

            var dataUrl = await new Promise(function (resolve) {
                var reader = new FileReader();
                reader.onload = function (e) { resolve(e.target.result); };
                reader.readAsDataURL(file);
            });

            var qrText = await new Promise(function (resolve) {
                var img = new Image();
                img.onload = function () {
                    var canvas = document.createElement('canvas');
                    canvas.width = img.width;
                    canvas.height = img.height;
                    var ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0);
                    var data = ctx.getImageData(0, 0, img.width, img.height);
                    var result = typeof jsQR !== 'undefined' ? jsQR(data.data, data.width, data.height) : null;
                    resolve(result ? result.data : null);
                };
                img.onerror = function () { resolve(null); };
                img.src = dataUrl;
            });

            if (qrText) {
                await _ref.invokeMethodAsync('ReceiveQrText', qrText);
            } else {
                await _ref.invokeMethodAsync('ReceiveParseError', 'No se pudo leer el código QR. Intenta con más luz o pega el texto del QR manualmente.');
            }
        } catch (err) {
            try { await _ref.invokeMethodAsync('ReceiveParseError', 'Error al procesar imagen: ' + err.message); } catch (e2) { }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    window.constanciaHelper = {

        // Called by Blazor when the modal opens — just registers the .NET ref
        init: function (dotNetRef) {
            _ref = dotNetRef;
        },

        // Called by Blazor when the modal closes
        dispose: function () {
            _ref = null;
        },

        // Wire a file input's change event to the appropriate handler.
        // Uses a flag on the element so multiple calls are safe (no duplicate listeners).
        // Does NOT use replaceChild — that breaks Blazor's DOM reference tracking.
        wireFileInput: function (inputId, isPdf) {
            var input = document.getElementById(inputId);
            if (!input || input._nsCsfWired) return;
            input._nsCsfWired = true;
            input.addEventListener('change', function () {
                var file = input.files && input.files[0];
                if (!file) return;
                if (isPdf) {
                    uploadPdf(file);
                } else {
                    processQrImage(file);
                }
                input.value = '';
            });
        }
    };

})();
