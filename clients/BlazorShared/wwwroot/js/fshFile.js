// File upload helper — PUT presigned URL with progress
window.fshFileUpload = async function (url, bytes, headers) {
    const blob = new Blob([new Uint8Array(bytes)]);
    const response = await fetch(url, {
        method: 'PUT',
        headers: headers || {},
        body: blob
    });
    if (!response.ok) throw new Error('Upload failed: ' + response.status);
    return true;
};

window.fshOpenDownload = function (url) {
    window.open(url, '_blank');
};

// Drag-and-drop bridge — captures dropped File objects on the drop event
// (Blazor DragEventArgs only carries file *names* in WebAssembly) and exposes
// them to .NET for the upload pipeline.
window.fshPendingDropFiles = [];

window.fshInitDropCapture = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el || el.dataset.fshDropBound) return;
    el.dataset.fshDropBound = '1';
    el.addEventListener('drop', (e) => {
        window.fshPendingDropFiles = Array.from(e.dataTransfer && e.dataTransfer.files ? e.dataTransfer.files : []);
    });
};

window.fshGetDroppedFiles = function () {
    return window.fshPendingDropFiles.map((f) => ({ name: f.name, size: f.size, type: f.type }));
};

window.fshReadDroppedFile = function (index, maxBytes) {
    const file = window.fshPendingDropFiles[index];
    if (!file) return null;
    return file.slice(0, maxBytes).arrayBuffer().then((buf) => new Uint8Array(buf));
};
