import svgPanZoom from 'svg-pan-zoom';
import {toPng} from 'html-to-image';
import {configure, enhanceMermaidDiagrams, cleanupMermaidEnhancements, showToolbar, toggleToolbar} from '../src/enhancements';

function mount() {
    document.body.innerHTML = '<form><div class="mermaid" data-processed="true"><svg viewBox="0 0 200 100"><defs><marker id="arrow"><path d="M0 0 L4 2"/></marker></defs><g class="svg-pan-zoom_viewport" style="transform:matrix(0.5,0,0,0.5,30,100)"><path marker-end="url(#arrow)" d="M0 0 L100 50"/></g></svg></div></form>';
    enhanceMermaidDiagrams();
}
const click = (action: string) => (document.querySelector(`[data-action="${action}"]`) as HTMLButtonElement).click();
beforeEach(() => {
    cleanupMermaidEnhancements();
    configure({renderer:undefined, controls:{showControls:true,fullscreen:true,pan:true,zoom:true,export:true,zoomIn:undefined,zoomOut:undefined,reset:undefined,exportPng:undefined,exportSvg:undefined}});
});
afterEach(() => { cleanupMermaidEnhancements(); vi.useRealTimers(); vi.restoreAllMocks(); });

it('cancels delayed fit after cleanup and repeated initialization', () => {
    vi.useFakeTimers(); mount();
    const first = vi.mocked(svgPanZoom).mock.results.at(-1)!.value;
    enhanceMermaidDiagrams();
    const second = vi.mocked(svgPanZoom).mock.results.at(-1)!.value;
    cleanupMermaidEnhancements(); vi.runAllTimers();
    expect(first.destroy).toHaveBeenCalledOnce(); expect(second.destroy).toHaveBeenCalledOnce();
    expect(first.resize).not.toHaveBeenCalled(); expect(second.resize).not.toHaveBeenCalled();
});

it.each(['close','backdrop','escape','cleanup'])('closes a pending fullscreen through %s without late initialization or leaked key listeners', mode => {
    vi.useFakeTimers(); mount();
    const remove = vi.spyOn(document,'removeEventListener');
    const opener = document.querySelector<HTMLButtonElement>('[data-action="fullscreen"]')!;
    opener.focus(); opener.click();
    const calls = vi.mocked(svgPanZoom).mock.calls.length;
    const modal = document.querySelector('.mermaid-lightbox')!;
    expect(modal.getAttribute('role')).toBe('dialog');
    if(mode==='close') (modal.querySelector('[data-action="close"]') as HTMLButtonElement).click();
    if(mode==='backdrop') modal.dispatchEvent(new MouseEvent('click',{bubbles:true}));
    if(mode==='escape') document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape'}));
    if(mode==='cleanup') cleanupMermaidEnhancements();
    vi.runAllTimers();
    expect(document.querySelector('.mermaid-lightbox')).toBeNull();
    expect(vi.mocked(svgPanZoom).mock.calls).toHaveLength(calls);
    expect(remove).toHaveBeenCalledWith('keydown', expect.any(Function));
    expect(document.activeElement).toBe(opener);
    expect(document.body.style.overflow).not.toBe('hidden');
});

it('gives a fullscreen copy unique marker IDs', () => {
    mount(); click('fullscreen');
    const marker = document.querySelector('.mermaid-lightbox marker')!;
    expect(marker.id).not.toBe('arrow');
    expect(document.querySelector('.mermaid-lightbox path[marker-end]')?.getAttribute('marker-end')).toBe(`url(#${marker.id})`);
});

it('keeps enhancement controls from submitting an enclosing form', () => {
    mount(); expect(Array.from(document.querySelectorAll('.mermaid-control-btn')).every(button=>button.getAttribute('type')==='button')).toBe(true);
});

it('preserves icon configuration when only controls change and API show restores a slid toolbar', () => {
    configure({icons:{pan:'custom-pan'}}); configure({controls:{export:false}}); mount();
    expect(document.querySelector('[data-action="pan"]')?.classList.contains('custom-pan')).toBe(true);
    click('hideToolbar');showToolbar();
    expect(document.querySelector('.mermaid-controls')?.classList.contains('hidden-slide')).toBe(false);
    expect((document.querySelector('.mermaid-show-toolbar') as HTMLElement).style.display).toBe('none');
});

it('exports native SVG with source bounds instead of wrapping it in a foreignObject', async () => {
    mount(); const anchor = vi.spyOn(HTMLAnchorElement.prototype,'click').mockImplementation(()=>{});
    click('exportSvg');
    await vi.waitFor(()=>expect(anchor).toHaveBeenCalled());
    const link = anchor.mock.instances[0] as unknown as HTMLAnchorElement;
    const xml = decodeURIComponent(link.href.split(',').slice(1).join(','));
    expect(xml).toContain('viewBox="0 0 200 100"'); expect(xml).toContain('<marker'); expect(xml).not.toContain('foreignObject');expect(xml).not.toContain('matrix(');
});

it('removes the offscreen export container even when PNG conversion fails', async () => {
    mount(); vi.spyOn(console,'error').mockImplementation(()=>{});
    vi.mocked(toPng).mockRejectedValueOnce(new Error('failed conversion'));
    click('exportPng');
    await vi.waitFor(()=>expect(console.error).toHaveBeenCalledWith('Failed to export diagram:',expect.any(Error)));
    expect(Array.from(document.body.children).some(el=>(el as HTMLElement).style.left==='-9999px')).toBe(false);
});

it('refits passive diagrams after resize and disconnects the observer during cleanup', () => {
    vi.useFakeTimers(); let resized:()=>void=()=>{};const disconnect=vi.fn();
    vi.stubGlobal('ResizeObserver',class {constructor(callback:()=>void){resized=callback;}observe(){}disconnect=disconnect;});
    try {
        mount(); const instance=vi.mocked(svgPanZoom).mock.results.at(-1)!.value;
        resized();vi.advanceTimersByTime(100);expect(instance.resize).toHaveBeenCalledOnce();
        cleanupMermaidEnhancements();expect(disconnect).toHaveBeenCalled();resized();vi.runAllTimers();
        expect(instance.resize).toHaveBeenCalledOnce();
    } finally {vi.unstubAllGlobals();}
});


it('restores the current toolbar after a theme render replaces the old toolbar', () => {
    mount();click('hideToolbar');(document.querySelector('.mermaid-show-toolbar') as HTMLButtonElement).click();
    enhanceMermaidDiagrams();click('hideToolbar');(document.querySelector('.mermaid-show-toolbar') as HTMLButtonElement).click();
    expect(document.querySelector('.mermaid-controls')?.classList.contains('hidden-slide')).toBe(false);
});


it('toggles a slid toolbar back into view and tolerates arbitrary diagram IDs', () => {
    mount();click('hideToolbar');toggleToolbar();
    expect(document.querySelector('.mermaid-controls')?.classList.contains('hidden-slide')).toBe(false);
    expect(()=>showToolbar('not-a-selector"[]')).not.toThrow();
});


it('updates embedded stylesheet selectors when a cloned SVG root receives a new ID', () => {
    mount();const source=document.querySelector('svg')!;source.id='source-diagram';
    const style=document.createElementNS('http://www.w3.org/2000/svg','style');style.textContent='#source-diagram .label p { margin: 0; }';source.prepend(style);
    click('fullscreen');const copy=document.querySelector('.mermaid-lightbox svg')!;
    expect(copy.querySelector('style')?.textContent).toContain(`#${copy.id} .label p`);
});


it('returns gesture ownership to the page when cleanup occurs while interaction is enabled', () => {
    mount();click('pan');const svg=document.querySelector('svg')!;
    expect(svg.style.touchAction).toBe('none');cleanupMermaidEnhancements();
    expect(svg.style.touchAction).toBe('pan-y pinch-zoom');expect(svg.dataset.interactive).toBe('false');
});
