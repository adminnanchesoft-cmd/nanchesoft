// Puente de cámara para foto de perfil (getUserMedia) <-> Blazor
window.nsCamera = {
    _stream: null,

    // Inicia la cámara y conecta el stream al elemento <video> indicado
    start: async function (videoElementId) {
        try {
            var video = document.getElementById(videoElementId);
            if (!video) return { ok: false, error: "No se encontró el elemento de video." };
            this._stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: "user", width: { ideal: 640 }, height: { ideal: 640 } },
                audio: false
            });
            video.srcObject = this._stream;
            await video.play();
            return { ok: true };
        } catch (e) {
            return { ok: false, error: (e && e.message) ? e.message : "No se pudo acceder a la cámara." };
        }
    },

    // Captura el frame actual del video y lo devuelve como dataURL (base64 JPEG)
    capture: function (videoElementId) {
        var video = document.getElementById(videoElementId);
        if (!video) return null;
        var size = Math.min(video.videoWidth, video.videoHeight);
        if (!size) return null;
        var sx = (video.videoWidth - size) / 2;
        var sy = (video.videoHeight - size) / 2;
        var canvas = document.createElement("canvas");
        canvas.width = 512;
        canvas.height = 512;
        var ctx = canvas.getContext("2d");
        ctx.drawImage(video, sx, sy, size, size, 0, 0, 512, 512);
        return canvas.toDataURL("image/jpeg", 0.9);
    },

    // Detiene la cámara y libera el dispositivo
    stop: function () {
        if (this._stream) {
            this._stream.getTracks().forEach(function (t) { t.stop(); });
            this._stream = null;
        }
    }
};
