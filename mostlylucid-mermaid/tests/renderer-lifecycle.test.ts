import {configure, cleanupMermaidEnhancements} from '../src/enhancements';
import {initMermaid} from '../src/theme-switcher';
const svg = '<svg viewBox="0 0 100 50"><rect width="100" height="50"/></svg>';
function source(text='flowchart LR; A-->B') {const el=document.createElement('div');el.className='mermaid';el.textContent=text;document.body.appendChild(el);return el;}
beforeEach(()=>{document.body.innerHTML='';configure({renderer:undefined,htmx:{enabled:false}});});
afterEach(()=>{cleanupMermaidEnhancements();configure({renderer:undefined,htmx:{enabled:false}});vi.restoreAllMocks();});

it('isolates renderer errors and preserves source for recovery', async()=>{
    vi.spyOn(console,'error').mockImplementation(()=>{});
    const bad=source('bad source'), good=source();
    configure({renderer:{name:'test',render(text){if(text==='bad source')throw new Error('invalid');return svg;}}});
    await initMermaid();
    expect(bad.getAttribute('data-render-error')).toBe('true'); expect(good.querySelector('svg')).not.toBeNull();
    configure({renderer:{name:'test',render:()=>svg}});await initMermaid();
    expect(bad.querySelector('svg')).not.toBeNull();expect(bad.getAttribute('data-original-code')).toBe('bad source');
    expect(bad.hasAttribute('data-render-error')).toBe(false);
});

it('serializes slow theme renders so the final requested theme wins', async()=>{
    const diagram=source();let release:()=>void=()=>{};let darkStarted=false;
    configure({renderer:{name:'slow',async render(_source,context){
        if(context.theme==='dark'){darkStarted=true;await new Promise<void>(resolve=>{release=resolve;});}
        return svg;
    }}});
    await initMermaid();document.body.dispatchEvent(new Event('dark-theme-set'));
    await vi.waitFor(()=>expect(darkStarted).toBe(true));
    document.body.dispatchEvent(new Event('light-theme-set'));release();
    await vi.waitFor(()=>{expect(diagram.getAttribute('data-diagram-theme')).toBe('default');expect(diagram.querySelector('svg')).not.toBeNull();});
});

it('honours HTMX opt-in and discovers both code fences and a target which is itself a diagram', async()=>{
    configure({renderer:{name:'test',render:()=>svg}});source();await initMermaid();
    const root=source();document.body.dispatchEvent(new CustomEvent('htmx:afterSettle',{detail:{target:root}}));
    await new Promise(resolve=>setTimeout(resolve,10));expect(root.querySelector('svg')).toBeNull();
    configure({htmx:{enabled:true}});document.body.dispatchEvent(new CustomEvent('htmx:afterSettle',{detail:{target:root}}));
    await vi.waitFor(()=>expect(root.querySelector('svg')).not.toBeNull());
    const section=document.createElement('section');section.innerHTML='<pre><code class="language-mermaid"></code></pre>';
    section.querySelector('code')!.textContent='flowchart LR; X-->Y';document.body.appendChild(section);
    document.body.dispatchEvent(new CustomEvent('htmx:afterSettle',{detail:{target:section}}));
    await vi.waitFor(()=>expect(section.querySelector('svg')).not.toBeNull());
});


it('preserves an empty source instead of treating an error message as diagram source', async()=>{
    vi.spyOn(console,'error').mockImplementation(()=>{});const diagram=source('');
    configure({renderer:{name:'test',render(){throw new Error('empty');}}});await initMermaid();
    configure({renderer:{name:'test',render(text){expect(text).toBe('');return svg;}}});await initMermaid();
    expect(diagram.getAttribute('data-original-code')).toBe('');expect(diagram.querySelector('svg')).not.toBeNull();
});
