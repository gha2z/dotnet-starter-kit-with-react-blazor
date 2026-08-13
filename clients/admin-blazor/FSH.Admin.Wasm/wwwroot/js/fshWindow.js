// Opens a URL in a new tab (React parity: impersonation handoff opens
// the dashboard app with the impersonation token in the URL hash).
window.openUrl = function (url) {
    window.open(url, "_blank", "noopener,noreferrer");
};