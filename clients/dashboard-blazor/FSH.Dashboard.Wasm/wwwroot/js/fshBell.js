// Programmatic trigger for the notifications bell (React parity: the React
// dashboard clicks the same [data-notification-bell] element). Blazor WASM's
// SPA interceptor does not route synthetic clicks through MudMenu otherwise.
window.fshBell = {
    open: function () {
        const el = document.querySelector('[data-notification-bell]');
        if (el) {
            el.click();
        }
    }
};