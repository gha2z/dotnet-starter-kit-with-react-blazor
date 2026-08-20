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

export function getPreference(key) {
    return localStorage.getItem(key);
}

export function setPreference(key, value) {
    localStorage.setItem(key, value);
}

// On-demand Google Fonts load for the appearance font picker. The boot only
// ships Figtree + Outfit + JetBrains Mono; every other selectable family is
// fetched here the first time it is chosen. Idempotent: reuses one <link>.
export function loadFont(query) {
    if (typeof document === 'undefined') return;
    const id = 'fsh-dynamic-font';
    let link = document.getElementById(id);
    if (!link) {
        link = document.createElement('link');
        link.id = id;
        link.rel = 'stylesheet';
        document.head.appendChild(link);
    }
    link.href = 'https://fonts.googleapis.com/css2?family=' + query + '&display=swap';
}
