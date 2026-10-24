// Same-origin, source-checked sizing keeps a mobile explainer from becoming a nested scrolling viewport.
window.addEventListener('message', event => {
    if (event.origin !== window.location.origin || event.data?.type !== 'mostlylucid-explainer-size') return;
    const height = Number(event.data.height);
    if (!Number.isFinite(height) || height < 300 || height > 8000) return;
    const frame = Array.from(document.querySelectorAll('iframe.article-explainer')).find(f => f.contentWindow === event.source);
    if (!frame) return;
    const pixels = Math.ceil(height);
    if (Math.abs(Number(frame.getAttribute('height')) - pixels) > 2) frame.setAttribute('height', String(pixels));
    frame.contentWindow.postMessage({type:'mostlylucid-explainer-theme',dark:document.documentElement.classList.contains('dark')||document.body.classList.contains('dark')},window.location.origin);
});
// The iframe reports its size again after its parent theme changes.
const themeObserver = new MutationObserver(() => document.querySelectorAll('iframe.article-explainer').forEach(frame =>
    frame.contentWindow?.postMessage({type:'mostlylucid-explainer-theme',dark:document.documentElement.classList.contains('dark')||document.body.classList.contains('dark')},window.location.origin)
));
themeObserver.observe(document.documentElement,{attributes:true,attributeFilter:['class']});
themeObserver.observe(document.body,{attributes:true,attributeFilter:['class']});
