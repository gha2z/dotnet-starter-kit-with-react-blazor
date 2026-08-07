const cleanupByRef = new Map();

export function watchScrollTop(dotNetRef, threshold = 80) {
    const el = document.querySelector('.fsh-chat-messages');
    if (!el) return;

    let loading = false;
    const onScroll = () => {
        if (loading) return;
        if (el.scrollTop <= threshold) {
            loading = true;
            dotNetRef.invokeMethodAsync('OnScrollTopReached').finally(() => { loading = false; });
        }
    };

    el.addEventListener('scroll', onScroll, { passive: true });
    cleanupByRef.set(dotNetRef, () => el.removeEventListener('scroll', onScroll));
}

export function unwatchScrollTop(dotNetRef) {
    const cleanup = cleanupByRef.get(dotNetRef);
    if (cleanup) {
        cleanup();
        cleanupByRef.delete(dotNetRef);
    }
}

export function scrollToBottom() {
    document.querySelector('.fsh-chat-messages')?.scrollTo(0, 999999);
}

export function scrollHeight() {
    return document.querySelector('.fsh-chat-messages')?.scrollHeight ?? 0;
}

export function restoreScrollPosition(fromHeight) {
    const el = document.querySelector('.fsh-chat-messages');
    if (!el) return;
    el.scrollTo(0, el.scrollHeight - fromHeight);
}
