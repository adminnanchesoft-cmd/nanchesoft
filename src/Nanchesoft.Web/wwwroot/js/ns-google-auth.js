// Puente entre Google Identity Services y Blazor (JSInterop)
// Con espera/reintentos para evitar problemas de timing en Blazor Server.
window.nsGoogleAuth = {
    dotNetRef: null,

    init: function (dotNetRef, clientId, buttonElementId) {
        this.dotNetRef = dotNetRef;
        this._tryRender(clientId, buttonElementId, 0);
    },

    _tryRender: function (clientId, buttonElementId, attempt) {
        var maxAttempts = 40; // ~40 * 150ms = 6 segundos máximo
        var self = this;

        var googleReady = window.google
            && window.google.accounts
            && window.google.accounts.id;

        var container = document.getElementById(buttonElementId);
        var containerReady = container && container.offsetParent !== null;

        if (!googleReady || !containerReady) {
            if (attempt < maxAttempts) {
                setTimeout(function () {
                    self._tryRender(clientId, buttonElementId, attempt + 1);
                }, 150);
            } else {
                console.warn("nsGoogleAuth: Google Identity Services no se cargó a tiempo o el contenedor no está visible.");
            }
            return;
        }

        try {
            window.google.accounts.id.initialize({
                client_id: clientId,
                callback: function (response) {
                    if (window.nsGoogleAuth.dotNetRef) {
                        window.nsGoogleAuth.dotNetRef.invokeMethodAsync('OnGoogleCredential', response.credential);
                    }
                }
            });

            // Limpiar el contenedor antes de renderizar (evita duplicados en re-render)
            container.innerHTML = '';

            var width = container.offsetWidth;
            if (!width || width < 200) {
                width = 240;
            }

            window.google.accounts.id.renderButton(container, {
                type: "standard",
                theme: "outline",
                size: "large",
                text: "continue_with",
                shape: "rectangular",
                logo_alignment: "center",
                width: width
            });
        } catch (e) {
            console.error("nsGoogleAuth: error al renderizar el botón de Google", e);
        }
    }
};
