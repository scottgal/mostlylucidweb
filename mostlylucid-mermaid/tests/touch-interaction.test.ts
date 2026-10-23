import svgPanZoom from 'svg-pan-zoom';
import {enhanceMermaidDiagrams, cleanupMermaidEnhancements, configure} from '../src/enhancements';

it('keeps taps passive and only captures gestures after explicit activation; disabling calls the real API', () => {
    let armed = false;
    const instance = {
        destroy: vi.fn(), resize: vi.fn(), fit: vi.fn(), center: vi.fn(),
        zoomIn: vi.fn(), zoomOut: vi.fn(), reset: vi.fn(),
        isPanEnabled: () => armed,
        enablePan: vi.fn(() => { armed = true; }),
        disablePan: vi.fn(() => { armed = false; }),
        enableZoom: vi.fn(), disableZoom: vi.fn(),
        enableMouseWheelZoom: vi.fn(), disableMouseWheelZoom: vi.fn(),
        enableDblClickZoom: vi.fn(), disableDblClickZoom: vi.fn()
    };
    vi.mocked(svgPanZoom).mockReturnValue(instance as any);
    configure({controls:{showControls:true,pan:true}});
    document.body.removeAttribute('data-mermaid-controls-initialized');
    document.body.innerHTML = '<div class="mermaid" data-processed="true"><svg viewBox="0 0 100 100"><rect width="100" height="100"/></svg></div>';
    enhanceMermaidDiagrams();
    const svg = document.querySelector('svg')!;
    const control = document.querySelector('[data-action="pan"]') as HTMLButtonElement;
    svg.dispatchEvent(new MouseEvent('click',{bubbles:true}));
    expect(instance.enablePan).not.toHaveBeenCalled();
    expect(svg.style.touchAction).toBe('pan-y pinch-zoom');
    expect(control.getAttribute('aria-pressed')).toBe('false');
    control.click();
    expect(armed).toBe(true);
    expect(control.getAttribute('aria-label')).toBe('Disable Pan & Zoom');
    expect(svg.style.touchAction).toBe('none');
    control.click();
    expect(instance.disablePan).toHaveBeenCalledOnce();
    expect(control.getAttribute('aria-label')).toBe('Enable Pan & Zoom');
    expect(armed).toBe(false);
    expect(svg.style.touchAction).toBe('pan-y pinch-zoom');
    expect(control.getAttribute('aria-pressed')).toBe('false');
    cleanupMermaidEnhancements();
});
