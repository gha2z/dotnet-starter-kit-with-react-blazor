window.fshError = {
    _ui: null,

    ensureUi: function () {
        if (!this._ui) {
            this._ui = document.getElementById("blazor-error-ui");
        }
        return this._ui;
    },

    show: function (message) {
        const ui = this.ensureUi();
        if (!ui) {
            return;
        }

        const detail = ui.querySelector(".fsh-error-detail");
        if (detail) {
            detail.textContent = message || "";
        }

        ui.classList.add("fsh-error-visible");
    },

    hide: function () {
        const ui = this.ensureUi();
        if (ui) {
            ui.classList.remove("fsh-error-visible");
        }
    }
};

(function () {
    window.addEventListener("error", function (event) {
        window.fshError.show(event.message ? "Unhandled error: " + event.message : "An unhandled error has occurred.");
    });

    window.addEventListener("unhandledrejection", function (event) {
        const reason = event.reason && event.reason.message ? event.reason.message : "An unhandled error has occurred.";
        window.fshError.show("Unhandled error: " + reason);
    });
})();
