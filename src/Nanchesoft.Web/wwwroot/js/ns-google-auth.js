// Puente entre Google Identity Services y Blazor (JSInterop)
window.nsGoogleAuth = {
    dotNetRef: null,

    init: function (dotNetRef, clientId, buttonElementId) {
        this.dotNetRef = dotNetRef;

        if (!window.google || !window.google.accounts || !window.google.accounts.id) {
            console.error("Google Identity Services no está cargado todavía.");
            return;
        }

        window.google.accounts.id.initialize({
            client_id: clientId,
            callback: function (response) {
                // response.credential es el ID token (JWT de Google)
                if (window.nsGoogleAuth.dotNetRef) {
                    window.nsGoogleAuth.dotNetRef.invokeMethodAsync('OnGoogleCredential', response.credential);
                }
            }
        });

        var container = document.getElementById(buttonElementId);
        if (container) {
            window.google.accounts.id.renderButton(container, {
                theme: "outline",
                size: "large",
                text: "continue_with",
                shape: "rectangular",
                width: container.offsetWidth > 0 ? container.offsetWidth : 240
            });
        }
    }
};
