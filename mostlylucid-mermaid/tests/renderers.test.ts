import { configure } from '../src/enhancements';
import { createPeleRenderer } from '../src/renderers';
import { initMermaid } from '../src/theme-switcher';

// The integration contract is independent of Pele's optional runtime dependency.
afterEach(() => configure({ renderer: undefined }));

it('passes unique SVG prefixes to an injected Pele renderer', async () => {
    const render = vi.fn((source: string, options: { idPrefix: string }) => ({ svg: `<svg id="${options.idPrefix}"><text>${source}</text></svg>` }));
    const adapter = createPeleRenderer(render);
    await adapter.render('flowchart LR; A-->B', { id: 'first', theme: 'dark' });
    expect(render).toHaveBeenCalledWith('flowchart LR; A-->B', { idPrefix: 'first', responsive: false });
});

it('renders with an adapter when Mermaid is absent and preserves the original text for theme changes', async () => {
    const oldMermaid = window.mermaid;
    window.mermaid = undefined;
    const render = vi.fn(() => ({ svg: '<svg viewBox="0 0 200 100"><text>Rendered</text></svg>' }));
    configure({ renderer: createPeleRenderer(render) });
    document.body.innerHTML = '<pre><code class="language-mermaid"></code></pre>';
    const source = 'flowchart LR\n A["<b>Source</b>"] --> B';
    document.querySelector('code')!.textContent = source;
    try {
        await initMermaid();
        const diagram = document.querySelector('.mermaid')!;
        expect(diagram.getAttribute('data-original-code')).toBe(source);
        expect(diagram.getAttribute('data-renderer')).toBe('pele');
        expect(diagram.querySelector('svg')).not.toBeNull();
        expect(document.querySelectorAll('.mermaid-wrapper')).toHaveLength(1);
        document.body.dispatchEvent(new Event('dark-theme-set'));
        await vi.waitFor(() => expect(diagram.getAttribute('data-diagram-theme')).toBe('dark'));
        expect(render.mock.calls.every(call => call[0] === source)).toBe(true);
    } finally { window.mermaid = oldMermaid; }
});
