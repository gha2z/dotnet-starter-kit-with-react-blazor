// Skip-to-content target focus for the hybrid shell. BlazorWebView's SPA
// click interceptor swallows plain fragment navigation (the hash changes but
// focus never moves), so MainLayout prevents the default and calls this.
window.fshSkipLink = {
    focusMain: function () {
        const el = document.getElementById('fsh-main');
        if (el) {
            el.focus();
            el.scrollIntoView();
        }
    }
};