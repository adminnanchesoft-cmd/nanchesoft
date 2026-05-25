const SILENT_THRESHOLD_MS = 5000; // show toast after this delay
const MODAL_THRESHOLD_MS  = 12000; // show blocking modal after this delay

const reconnectModal = document.getElementById("components-reconnect-modal");
const retryButton    = document.getElementById("components-reconnect-button");
const resumeButton   = document.getElementById("components-resume-button");

let toastTimer  = null;
let modalTimer  = null;
let toastEl     = null;

reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);
retryButton.addEventListener("click", retry);
resumeButton.addEventListener("click", resume);

function handleReconnectStateChanged(event) {
    const state = event.detail.state;

    if (state === "show") {
        // Brief hiccup — stay silent, start timers
        toastTimer = setTimeout(showToast, SILENT_THRESHOLD_MS);
        modalTimer = setTimeout(showModal, MODAL_THRESHOLD_MS);

    } else if (state === "hide") {
        // Reconnected — clean up everything silently
        clearTimers();
        hideToast();
        if (reconnectModal.open) reconnectModal.close();

    } else if (state === "failed") {
        // Definitively failed — skip timers, show modal now
        clearTimers();
        hideToast();
        showModal();
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

    } else if (state === "rejected") {
        location.reload();

    } else if (state === "paused") {
        clearTimers();
        hideToast();
        showModal();
    }
}

function clearTimers() {
    clearTimeout(toastTimer);
    clearTimeout(modalTimer);
    toastTimer = null;
    modalTimer = null;
}

function showModal() {
    hideToast();
    if (!reconnectModal.open) reconnectModal.showModal();
}

function showToast() {
    if (toastEl) return;
    toastEl = document.createElement("div");
    toastEl.id = "ns-reconnect-toast";
    toastEl.setAttribute("role", "status");
    toastEl.setAttribute("aria-live", "polite");
    toastEl.innerHTML =
        '<svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg" class="ns-reconnect-spinner" aria-hidden="true">' +
            '<circle cx="8" cy="8" r="6" stroke="currentColor" stroke-width="2" stroke-dasharray="28" stroke-dashoffset="10" stroke-linecap="round"/>' +
        '</svg>' +
        '<span>Reconectando…</span>';
    document.body.appendChild(toastEl);
    // Trigger animation
    requestAnimationFrame(() => toastEl && toastEl.classList.add("ns-reconnect-toast--visible"));
}

function hideToast() {
    if (!toastEl) return;
    toastEl.classList.remove("ns-reconnect-toast--visible");
    const el = toastEl;
    toastEl = null;
    setTimeout(() => el.remove(), 400);
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    try {
        const successful = await Blazor.reconnect();
        if (!successful) {
            const resumed = await Blazor.resumeCircuit();
            if (!resumed) {
                location.reload();
            } else {
                reconnectModal.close();
            }
        }
    } catch {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            location.reload();
        }
    } catch {
        reconnectModal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}
