export function isOnline() {
    return navigator.onLine;
}

export function watch(dotNetRef) {
    const onStatus = () => dotNetRef.invokeMethodAsync('OnStatusChanged', navigator.onLine);
    window.addEventListener('online', onStatus);
    window.addEventListener('offline', onStatus);
    return () => {
        window.removeEventListener('online', onStatus);
        window.removeEventListener('offline', onStatus);
    };
}
