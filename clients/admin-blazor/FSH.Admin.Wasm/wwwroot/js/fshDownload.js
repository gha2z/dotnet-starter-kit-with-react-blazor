window.fshDownload = {
    saveFile: function (fileName, data) {
        const blob = data instanceof Blob ? data : new Blob([data], { type: "application/octet-stream" });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
        URL.revokeObjectURL(url);
    }
};
