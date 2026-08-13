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
