import mermaid from 'mermaid';
import {render} from 'pele';
// Exercise the actual public bundle, not private source imports or a CDN release.
import {initMermaid, configure, enhanceMermaidDiagrams, cleanupMermaidEnhancements, createPeleRenderer, hideToolbar, showToolbar, toggleToolbar} from '../dist/index.bundle.js';

const $ = id => document.getElementById(id);
const settle = () => new Promise(resolve => setTimeout(resolve, 180));
window.mermaid = mermaid;
const pele = createPeleRenderer((source, options) => render(source, options));
const fixtures = [
    ['Flowchart / subgraph / markers', 'flowchart LR\n A[Client] --> B{Policy}\n subgraph Gateway\n B -->|allow| C[Origin]\n B -->|challenge| D[Response]\n end\n C -.-> E[Telemetry]'],
    ['Sequence', 'sequenceDiagram\n participant U as User\n participant G as Gateway\n participant A as App\n U->>G: Request\n alt Allowed\n G->>A: Forward\n A-->>G: Response\n else Challenge\n G-->>U: Verify\n end'],
    ['Class', 'classDiagram\n class Observation {\n +float weight\n +Vector values\n }\n class Profile {\n +update()\n }\n Profile --> Observation'],
    ['State', 'stateDiagram-v2\n [*] --> Passive\n Passive --> Interactive: Enable\n Interactive --> Passive: Disable\n Passive --> Fullscreen: Open\n Fullscreen --> Passive: Close'],
    ['Entity relationships', 'erDiagram\n CUSTOMER ||--o{ ORDER : places\n ORDER ||--|{ ITEM : contains'],
    ['Pie', 'pie title Evidence\n "Views" : 3\n "Comparisons" : 2\n "Basket" : 5'],
    ['Gantt', 'gantt\n title Request work\n dateFormat YYYY-MM-DD\n section Inline\n Observe :a, 2026-10-01, 1d\n Decide :after a, 1d'],
    ['Mind map', 'mindmap\n root((Evidence))\n  Behaviour\n   Timing\n   Transitions\n  Content\n   Features'],
    ['Code fence / escaped source', 'flowchart LR\n A["Words & numbers"] --> B["Coordinate 2"]']
];
let busy = false;
function status(message, error = false) { $('status').textContent = message; $('status').dataset.state = error ? 'error' : 'ready'; }
function addFixture(title, source, parent = $('fixtures'), fence = false) {
    const section = document.createElement('section'); section.className = 'fixture';
    const heading = document.createElement('h2'); heading.textContent = title; section.appendChild(heading);
    const diagram = document.createElement(fence ? 'pre' : 'div');
    if (fence) { const code = document.createElement('code'); code.className = 'language-mermaid'; code.textContent = source; diagram.appendChild(code); }
    else { diagram.className = 'mermaid'; diagram.textContent = source; }
    section.appendChild(diagram);
    const details = document.createElement('details'), summary = document.createElement('summary'), pre = document.createElement('pre');
    summary.textContent = 'Original source'; pre.textContent = source; details.append(summary, pre); section.appendChild(details);
    parent.appendChild(section); return diagram;
}
function applyConfig() {
    const name = $('renderer').value;
    configure({renderer: name === 'mermaid' ? undefined : name === 'pele' ? pele : {
        name: 'pele', async render(source, context) { await new Promise(resolve => setTimeout(resolve, 25)); return pele.render(source, context); }
    }, htmx: {enabled: $('auto-htmx').checked}, icons: Object.fromEntries(['fullscreen','zoomIn','zoomOut','reset','pan','exportPng','exportSvg','hide'].map(key=>[key,$('custom-icons').checked?'sample-icon':'']))});
    // Explicit values clear granular overrides left by a previous preset.
    const base = {showControls:true,fullscreen:true,zoom:true,pan:true,export:true,zoomIn:undefined,zoomOut:undefined,reset:undefined,exportPng:undefined,exportSvg:undefined};
    const preset = $('preset').value;
    if (preset === 'minimal') Object.assign(base,{fullscreen:false,pan:false,export:false});
    if (preset === 'no-export') base.export = false;
    if (preset === 'custom') Object.assign(base,{zoom:false,zoomIn:true,export:false,exportSvg:true});
    if (preset === 'none') base.showControls = false;
    configure({controls:base});
}
async function reload() { applyConfig(); await initMermaid(); status(`${$('renderer').value}: rendered ${document.querySelectorAll('.fixture .mermaid svg').length} diagrams.`); }
async function action(work) {
    if (busy) return; busy = true;
    document.querySelectorAll('.actions button,select,input').forEach(button=>button.disabled=true);
    try { await work(); } catch (error) { status(error.message,true); console.error(error); }
    finally { busy=false;document.querySelectorAll('.actions button,select,input').forEach(button=>button.disabled=false); }
}
function dark(value) {
    document.documentElement.classList.toggle('dark',value);
    window.__themeState = value ? 'dark' : 'light';
    document.body.dispatchEvent(new Event(value?'dark-theme-set':'light-theme-set'));
}
// Scoped HTMX events work without fetching a server response.
function insert(fence = true) {
    const diagram = addFixture('Dynamic content', 'flowchart LR\n Fresh --> Evidence', $('dynamic-fixtures'), fence);
    const target = fence ? diagram.parentElement : diagram;
    document.body.dispatchEvent(new CustomEvent('htmx:afterSettle',{detail:{target}}));
}
$('renderer').onchange = $('preset').onchange = $('custom-icons').onchange = () => action(reload);
$('auto-htmx').onchange = applyConfig;
$('theme').onclick = () => action(async()=>{dark(!document.documentElement.classList.contains('dark'));await initMermaid();});
$('rapid').onclick = () => action(async()=>{dark(true);dark(false);dark(true);await initMermaid();status('Rapid themes complete; final theme should be dark.');});
$('reinit').onclick = () => action(async()=>{await initMermaid();await initMermaid();status('Initialised twice; one toolbar per diagram expected.');});
$('cleanup').onclick = () => {cleanupMermaidEnhancements();status('Cleaned up. Restore to reactivate controls.');};
$('restore').onclick = () => action(reload);
$('hide').onclick = () => hideToolbar(); $('show').onclick = () => showToolbar(); $('toggle').onclick = () => toggleToolbar();
$('first').onclick = () => toggleToolbar(document.querySelector('.mermaid-wrapper')?.dataset.diagramId);
$('dynamic').onclick = () => action(async()=>{insert(true);await settle();status('Inserted code fence. With HTMX disabled it stays source until Restore.');});
$('root-dynamic').onclick = () => action(async()=>{insert(false);await settle();status('Inserted root diagram.');});
$('remove').onclick = () => action(async()=>{cleanupMermaidEnhancements();$('dynamic-fixtures').replaceChildren();await reload();});
$('invalid').onclick = () => action(async()=>{addFixture('Malformed source (expected failure)', 'THIS IS NOT A DIAGRAM', $('dynamic-fixtures'));addFixture('Valid after failure','flowchart LR\n Still --> Works',$('dynamic-fixtures'));await reload();});

$('checks').onclick = () => action(async()=>{
    const lines=[];
    const check=(label,condition)=>{lines.push(`${condition?'PASS':'FAIL'} ${label}`);$('results').textContent=lines.join('\n');};
    const selected=$('renderer').value;
    $('dynamic-fixtures').replaceChildren();
    for (const renderer of ['mermaid','pele']) {
        $('renderer').value=renderer;$('preset').value='default';await reload();
        check(`${renderer}: every fixture renders`,document.querySelectorAll('#fixtures .mermaid svg').length===fixtures.length);
        const before=document.querySelectorAll('.mermaid-wrapper').length; await initMermaid();await settle();
        check(`${renderer}: repeat init does not duplicate wrappers`,document.querySelectorAll('.mermaid-wrapper').length===before);
        check(`${renderer}: one toolbar per wrapper`,Array.from(document.querySelectorAll('.mermaid-wrapper')).every(w=>w.querySelectorAll('.mermaid-controls').length===1));
        const svg=document.querySelector('.mermaid svg');svg.dispatchEvent(new MouseEvent('click',{bubbles:true}));
        check(`${renderer}: taps stay passive`,svg.dataset.interactive==='false');
        const viewport=svg.querySelector('.svg-pan-zoom_viewport');
        const transform=()=>viewport.style.transform||viewport.getAttribute('transform');
        const original=transform();document.querySelector('[data-action=zoomIn]').click();await settle();
        check(`${renderer}: zoom button changes the viewport while passive`,transform()!==original&&svg.dataset.interactive==='false');
        document.querySelector('[data-action=zoomOut]').click();document.querySelector('[data-action=reset]').click();await settle();
        const pan=document.querySelector('[data-action=pan]');pan.click();check(`${renderer}: explicit enable`,svg.dataset.interactive==='true');pan.click();check(`${renderer}: explicit disable`,svg.dataset.interactive==='false');
        for (const preset of ['minimal','no-export','custom','none']) {
            $('preset').value=preset;applyConfig();enhanceMermaidDiagrams();
            const toolbar=document.querySelector('.mermaid-wrapper .mermaid-controls');
            check(`${renderer}: ${preset} preset`,preset==='none'?!toolbar:preset==='custom'?!!toolbar.querySelector('[data-action=exportSvg]')&&!toolbar.querySelector('[data-action=exportPng]'):!toolbar.querySelector('[data-action=exportPng]'));
        }
        $('preset').value='default';applyConfig();enhanceMermaidDiagrams();
        document.querySelector('[data-action=hideToolbar]').click();showToolbar();
        check(`${renderer}: API show restores slid toolbar`,!document.querySelector('.mermaid-controls').classList.contains('hidden-slide'));
        document.querySelector('[data-action=hideToolbar]').click();document.querySelector('.mermaid-show-toolbar').click();
        check(`${renderer}: show button restores the current toolbar`,!document.querySelector('.mermaid-controls').classList.contains('hidden-slide'));
        const opener=document.querySelector('[data-action=fullscreen]');opener.focus();opener.click();
        document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));await settle();
        check(`${renderer}: immediate fullscreen close cleans up`,!document.querySelector('.mermaid-lightbox')&&document.activeElement===opener);
        cleanupMermaidEnhancements();await reload();check(`${renderer}: cleanup then restore`,!!document.querySelector('.mermaid svg'));
        $('auto-htmx').checked=true;applyConfig();insert(true);await initMermaid();insert(false);await initMermaid();
        check(`${renderer}: dynamic fence and root render`,document.querySelectorAll('#dynamic-fixtures svg').length===2);
        $('dynamic-fixtures').replaceChildren();
        dark(true);dark(false);dark(true);await initMermaid();
        check(`${renderer}: rapid changes finish in the final theme`,Array.from(document.querySelectorAll('.mermaid')).every(d=>d.dataset.diagramTheme==='dark'));
    }
    $('renderer').value=selected;$('preset').value='default';await reload();
    status(`Checks complete: ${lines.filter(line=>line.startsWith('PASS')).length}/${lines.length}. Complete the manual checklist too.`);
});
fixtures.forEach(([title,source],i)=>addFixture(title,source,$('fixtures'),i===fixtures.length-1));
await action(reload);
