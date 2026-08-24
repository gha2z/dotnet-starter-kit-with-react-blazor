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

// React parity (message-list.tsx): auto-scroll is suppressed once the user has
// scrolled away from the bottom; the page asks this before scrolling for an
// incoming message. Returns 0 when the container is missing (treat as pinned).
export function distanceFromBottom() {
    const el = document.querySelector('.fsh-chat-messages');
    if (!el) return 0;
    return el.scrollHeight - el.scrollTop - el.clientHeight;
}

export function scrollHeight() {
    return document.querySelector('.fsh-chat-messages')?.scrollHeight ?? 0;
}

export function restoreScrollPosition(fromHeight) {
    const el = document.querySelector('.fsh-chat-messages');
    if (!el) return;
    el.scrollTo(0, el.scrollHeight - fromHeight);
}
