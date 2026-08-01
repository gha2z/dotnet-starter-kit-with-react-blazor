export function getTheme(key) {
    const stored = localStorage.getItem(key);
    if (stored === 'dark' || stored === 'light' || stored === 'system') {
        return stored;
    }
    return null;
}

export function setTheme(key, value) {
    localStorage.setItem(key, value);
}

export function prefersDark() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
}
