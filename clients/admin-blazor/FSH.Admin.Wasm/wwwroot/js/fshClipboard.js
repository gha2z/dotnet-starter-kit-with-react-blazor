window.fshClipboard = {
    copy: function (value) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(value ?? "");
        }
        return new Promise((resolve, reject) => {
            try {
                const textarea = document.createElement("textarea");
                textarea.value = value ?? "";
                textarea.style.position = "fixed";
                textarea.style.opacity = "0";
                document.body.appendChild(textarea);
                textarea.select();
                const ok = document.execCommand("copy");
                textarea.remove();
                ok ? resolve() : reject(new Error("execCommand copy failed"));
            } catch (err) {
                reject(err);
            }
        });
    }
};
