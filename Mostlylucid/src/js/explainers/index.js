import {Deck, OrthographicView, OrbitView, COORDINATE_SYSTEM} from '@deck.gl/core';
import {ScatterplotLayer, LineLayer, PathLayer} from '@deck.gl/layers';
import {render} from 'pele';
import {configure, enhanceMermaidDiagrams, cleanupMermaidEnhancements} from '../../../../mostlylucid-mermaid/src/enhancements.ts';
import {createPeleRenderer} from '../../../../mostlylucid-mermaid/src/renderers.ts';
import '../../../../mostlylucid-mermaid/src/styles.css';
import {groups, candidates, events, replay, rank, eligible, proxyScenarios, proxySteps} from './model';
import {predictionMax} from './prediction-model';
import {createPredictionView} from './prediction-view';

const $ = id => document.getElementById(id);
const params = new URLSearchParams(location.search);
let view = ['proxy', 'prediction'].includes(params.get('view')) ? params.get('view') : 'vectors';
let cursor = 0, idle = 0, timer = null, threeD = false, interactive = false, selected = null;
let deck = null, webglFailed = false, proxyDirection = null;
let predictionView = null, predictionVisible = false;
const predictionVisibility = new IntersectionObserver(entries => { predictionVisible = entries[0].isIntersecting; });
predictionVisibility.observe($('prediction-plot'));
const prefersReducedMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;
const positions = values => threeD ? values.map(v => v * 1000) : [values[0] * 1000, values[1] * 1000, 0];
const format = values => '[' + values.map(v => v.toFixed(3)).join(', ') + ']';
const filters = () => ({budget: Number($('budget').value), category: $('category').value, available: $('available').checked});
const state = () => replay(cursor, {halfLife: Number($('half-life').value), cartWeight: Number($('cart-weight').value), idle});
const palette = () => document.documentElement.classList.contains('dark') ? [205, 235, 206] : [30, 78, 53];
const source = direction => `flowchart ${direction}
 Client[Client] --> Edge[TLS edge]
 subgraph Gateway[Protective gateway]
 direction TB
 Observe[Observe request] --> Evidence[Accumulate evidence] --> Policy{Policy}
 Policy -->|challenge| Challenge[Challenge response]
 end
 Edge --> Observe
 Policy -->|allow| Origin[Origin application]
 Origin --> Reply[Return response]
 Challenge --> Reply
 Evidence -.-> Memory[Async telemetry]
`;
const pele = createPeleRenderer((text, options) => render(text, {...options, fontSize: 14}));
configure({icons: {fullscreen: '', zoomIn: '', zoomOut: '', reset: '', pan: '', exportPng: '', exportSvg: '', hide: ''}});

// Rendering is isolated from the replay model. Layout is stable while the playhead moves.
async function mountProxy() {
    const direction = innerWidth <= 720 ? 'TB' : 'LR';
    if (direction === proxyDirection) return;
    proxyDirection = direction;
    cleanupMermaidEnhancements();
    const diagram = $('proxy-stage');
    const text = source(direction);
    diagram.innerHTML = await pele.render(text, {id: 'proxy-explainer', theme: 'default'});
    diagram.dataset.renderer = 'pele';
    diagram.dataset.originalCode = text;
    diagram.dataset.processed = 'true';
    syncDiagramTheme();
    enhanceMermaidDiagrams();
    diagram.querySelectorAll('.pele-node').forEach(node => {
        const id = node.dataset.id;
        node.setAttribute('tabindex', '0');
        node.setAttribute('role', 'button');
        node.setAttribute('aria-label', `Inspect ${id}`);
        const inspect = () => {
            const step = proxySteps($('scenario').value).findIndex(s => s.node === id);
            if (step >= 0) { pause(); cursor = step; update(); }
        };
        node.addEventListener('click', inspect);
        node.addEventListener('keydown', event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); inspect(); } });
    });
}
function syncDiagramTheme() { $('proxy-stage').dataset.diagramTheme = document.documentElement.classList.contains('dark') ? 'dark' : 'default'; }

function camera() {
    const size = Math.min($('plot').clientWidth, $('plot').clientHeight);
    return {target: [500, 500, threeD ? 450 : 0], zoom: Math.log2(size / 1150), rotationX: 28, rotationOrbit: 35};
}
function controller() {
    return {dragPan: interactive, dragRotate: interactive && threeD, scrollZoom: interactive,
        touchZoom: interactive, touchRotate: interactive && threeD, keyboard: interactive, doubleClickZoom: interactive};
}
function selectPoint(point) {
    selected = point;
    if (deck) drawSpace();
    $('point-details').open = true;
    $('point-detail').textContent = `${point.name}; £${point.price}; ${point.available ? 'available' : 'unavailable'}. Coordinates ${format(point.values)}.`;
}
function fallbackPlot(current, ranked) {
    const best = new Set(ranked.map(p => p.id));
    const width = $('plot').clientWidth || 600;
    const height = $('plot').clientHeight || 405;
    const size = Math.min(width - 60, height - 55), left = (width - size) / 2;
    const xy = values => [left + values[0] * size, height - 30 - values[1] * size];
    const lines = current.mean ? ranked.map(p => { const a = xy(current.mean), b = xy(p.values); return `<line x1="${a[0]}" y1="${a[1]}" x2="${b[0]}" y2="${b[1]}" stroke="#8eab91" stroke-width="1"/>`; }).join('') : '';
    const points = candidates.map(p => {
        const [x, y] = xy(p.values);
        return `<circle data-point="${p.id}" cx="${x}" cy="${y}" r="${best.has(p.id) ? 6 : 3.5}" fill="rgb(${p.color})" opacity="${eligible(p, filters()) ? .8 : .13}" tabindex="0" role="button" aria-label="Inspect ${p.name}"><title>${p.name}</title></circle>`;
    }).join('');
    const mean = current.mean ? (() => { const [x,y] = xy(current.mean); return `<circle cx="${x}" cy="${y}" r="10" fill="#fff8da" stroke="#2c6046" stroke-width="3"/>`; })() : '';
    $('fallback').innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" aria-label="Two-dimensional teaching coordinates">${lines}${points}${mean}</svg>`;
    $('fallback').querySelectorAll('[data-point]').forEach(point => {
        const inspect = () => selectPoint(candidates.find(p => p.id === point.dataset.point));
        point.addEventListener('click', inspect);
        point.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); inspect(); } });
    });
}
function showFallback(error) {
    if (webglFailed) return;
    webglFailed = true;
    console.warn('Using the SVG teaching view because WebGL is unavailable.', error?.message ?? '');
    deck?.finalize(); deck = null;
    $('fallback').hidden = false;
    $('plot-status').textContent = 'SVG teaching view · WebGL unavailable';
    $('dimension').disabled = true;
}
function drawSpace() {
    const current = state(), ranked = rank(current, filters());
    fallbackPlot(current, ranked); // Also provides a printable snapshot when WebGL is active.
    if (webglFailed) return;
    const best = new Set(ranked.map(p => p.id));
    try {
        if (!deck) deck = new Deck({parent: $('plot'), views: new OrthographicView({flipY: false, near: -2000, far: 2000}),
            initialViewState: camera(), controller: controller(),
            onLoad: () => { $('fallback').hidden = true; },
            onError: showFallback,
            getTooltip: ({object}) => object?.name ? `${object.name}\n${format(object.values)}` : null});
        const common = {coordinateSystem: COORDINATE_SYSTEM.CARTESIAN};
        const referenceCenters = groups.map(g => { const points = candidates.filter(p => p.group === g.id); return {...g, center: [0,1,2].map(i => points.reduce((sum,p) => sum+p.values[i],0)/points.length)}; });
        const layers = [
            new LineLayer({...common, id: 'axes', data: [
                {a: [0,0,0], b: [1000,0,0]}, {a: [0,0,0], b: [0,1000,0]}, ...(threeD ? [{a:[0,0,0],b:[0,0,1000]}] : [])],
                getSourcePosition:d=>d.a, getTargetPosition:d=>d.b, getColor:[140,155,140,90], getWidth:1}),
            new ScatterplotLayer({...common, id:'candidates', data:candidates, pickable:true,
                getPosition:d=>positions(d.values), radiusUnits:'pixels', getRadius:d=>best.has(d.id)?6:4,
                getFillColor:d=>[...d.color,eligible(d,filters())?210:35],
                stroked:true,lineWidthUnits:'pixels',getLineWidth:d=>selected?.id===d.id?2:0,
                getLineColor:palette(), onClick:({object})=>object&&selectPoint(object),
                updateTriggers:{getPosition:[threeD],getFillColor:[JSON.stringify(filters())],getRadius:[Array.from(best).join()],getLineWidth:[selected?.id]}}),
            new ScatterplotLayer({...common,id:'group-centroids',data:referenceCenters,getPosition:d=>positions(d.center),
                radiusUnits:'pixels',getRadius:9,getFillColor:[0,0,0,0],stroked:true,getLineColor:d=>[...d.color,120],getLineWidth:1.5,lineWidthUnits:'pixels'})
        ];
        if(current.mean) {
            layers.push(new PathLayer({...common,id:'trajectory',data:[current.trail],getPath:d=>d.map(positions),getColor:[187,145,71,190],widthUnits:'pixels',getWidth:2}));
            layers.push(new LineLayer({...common,id:'neighbors',data:ranked,getSourcePosition:()=>positions(current.mean),getTargetPosition:d=>positions(d.values),getColor:[110,153,118,140],getWidth:1.5}));
            layers.push(new ScatterplotLayer({...common,id:'current-centroid',data:[current.mean],getPosition:positions,transitions:{getPosition:prefersReducedMotion?0:450},
                radiusUnits:'pixels',getRadius:11,getFillColor:[246,231,185,255],stroked:true,getLineColor:palette(),getLineWidth:2.5,lineWidthUnits:'pixels'}));
        }
        deck.setProps({layers, views:threeD?new OrbitView({orbitAxis:'Z', far: 5000}):new OrthographicView({flipY:false, near: -2000, far: 2000}),controller:controller()});
    } catch(error) { showFallback(error); }
}
function vectorUpdate() {
    const current = state(), ranked = rank(current,filters());
    $('vector-title').textContent = cursor ? (current.mass < .1 ? 'Evidence has expired' : 'A live interest summary') : 'No personal evidence yet';
    $('vector-caption').textContent = cursor ? events[cursor-1].text + `. Observation at minute ${events[cursor-1].minute}.` : 'Step forward to observe the first product.';
    $('mass').textContent = current.mass.toFixed(3);
    $('mean-values').innerHTML = ['Outdoors','Electronics','Home'].map((name,i)=>`<div class="value-row"><span>${name}<strong>${current.mean?current.mean[i].toFixed(3):'—'}</strong></span><div class="track"><i style="width:${(current.mean?.[i]??0)*100}%"></i></div></div>`).join('');
    $('ranked').innerHTML = ranked.length ? ranked.map(p=>`<button class="ranked-item" data-candidate="${p.id}"><span>${p.name}<br>£${p.price}</span><small>L2 ${p.distance.toFixed(3)}</small></button>`).join('') : (current.mean && current.mass >= .1 ? '<p class="ranked-empty">No candidates match these filters. Try a different budget or category.</p>' : '<p class="ranked-empty">No usable personal evidence. No personalised list is invented here.</p>');
    $('ranked').querySelectorAll('button').forEach(b=>b.onclick=()=>selectPoint(candidates.find(p=>p.id===b.dataset.candidate)));
    $('budget-value').textContent = `£${$('budget').value}`;
    $('half-life-value').textContent = `${$('half-life').value} min`;
    $('cart-weight-value').textContent = $('cart-weight').value;
    $('idle-note').textContent = `${idle} idle minutes. Mass decays while the mean stays in place. Below 0.1, personal ranking abstains.`;
    $('data-table').innerHTML = ranked.map(p=>`<tr><td>${p.name}</td><td>${format(p.values)}</td><td>Distance ${p.distance.toFixed(4)}; £${p.price}</td></tr>`).join('') || (current.mean && current.mass >= .1 ? `<tr><td>Current entity</td><td>${format(current.mean)}</td><td>No eligible candidates</td></tr>` : '<tr><td>Current entity</td><td>No usable summary</td><td>No personal ranking</td></tr>');
    drawSpace();
}
function proxyUpdate() {
    const key = $('scenario').value, steps = proxySteps(key), step = steps[cursor];
    $('proxy-title').textContent = step.title;
    $('proxy-caption').textContent = step.text;
    $('origin-reached').textContent = step.origin ? 'Yes' : 'Not yet';
    if (proxyScenarios[key].action !== 'allow' && cursor>=5) $('origin-reached').textContent = 'No';
    $('policy-path').textContent = cursor>=4 ? proxyScenarios[key].action : 'Not evaluated';
    $('proxy-evidence').innerHTML = cursor>=3 ? proxyScenarios[key].evidence.map(([name,value])=>`<div class="evidence-item"><span>${name}</span><strong>${value}</strong></div>`).join('') : '<p class="small muted">Evidence appears after the observation stage.</p>';
    $('proxy-ledger').innerHTML = steps.map((s,i)=>`<li class="${i===cursor?'active':''}">${i<=cursor?s.title:'Pending'}</li>`).join('');
    $('proxy-stage').querySelectorAll('.pele-node').forEach(node=>{
        const id=node.dataset.id;
        const index=steps.findIndex(s=>s.node===id);
        node.setAttribute('tabindex', index >= 0 ? '0' : '-1');
        node.setAttribute('aria-disabled', String(index < 0));
        node.dataset.progress = id===step.node ? 'active' : index>=0&&index<cursor ? 'done' : 'dim';
    });
    $('data-table').innerHTML = steps.slice(0,cursor+1).map(s=>`<tr><td>${s.node}</td><td>${s.title}</td><td>Origin ${s.origin?'reached':'not reached'}</td></tr>`).join('');
}
const max = () => view==='prediction'?predictionMax:view==='vectors'?events.length:proxySteps($('scenario').value).length-1;
function update() {
    cursor=Math.max(0,Math.min(max(),cursor));
    if(view==='vectors') vectorUpdate(); else if(view==='prediction') predictionView.update(cursor); else proxyUpdate();
    $('scrub').max=String(max()); $('scrub').value=String(cursor);
    $('step-count').textContent=`${view==='prediction'?cursor.toFixed(1):cursor} / ${max()}`;
    const label=view==='prediction'?predictionView.label(cursor):view==='vectors'?(cursor?events[cursor-1].text:'Before the first observation'):proxySteps($('scenario').value)[cursor].title;
    $('step-label').textContent=label; if($('announcement').textContent!==label)$('announcement').textContent=label;
    $('back').disabled=cursor===0; $('next').disabled=cursor===max();
    postSize();
}
function pause(){clearInterval(timer);timer=null;$('play').textContent='Play';}
function advance(){if(cursor>=max()){pause();return;}cursor++;idle=0;update();if(cursor===max())pause();}
async function switchView(next){
    pause(); view=next;cursor=view==='prediction'?2:0;idle=0;
    document.documentElement.dataset.view=view;
    // Keep the prediction controls beside the map, ahead of the longer evidence inspector on phones.
    if(view==='prediction')$('prediction-view').before(document.querySelector('.playback'));
    else $('announcement').before(document.querySelector('.playback'));
    $('vector-view').hidden=view!=='vectors'; $('proxy-view').hidden=view!=='proxy'; $('prediction-view').hidden=view!=='prediction';
    if(view==='prediction'&&!predictionView)predictionView=createPredictionView(update);
    document.querySelectorAll('nav [data-view]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.view===view)));
    $('heading').innerHTML=view==='prediction'?'Watch a journey.<br>Predict the <em>destination.</em>':view==='vectors'?'Watch evidence become<br>a <em>recommendation.</em>':'Follow a request.<br>See the <em>decision.</em>';
    $('intro').textContent=view==='prediction'?'24 individual journeys. Watch comparisons, basket removals and checkout attempts split their paths towards different outcomes.':view==='vectors'?'A point is an observation. A centroid is a summary. Add evidence and follow what changes.':'A proxy sits in the request path. Observations inform policy before the origin receives anything.';
    $('article-link').href=view==='prediction'?'/blog/show-me-what-you-got-part3-predict':view==='vectors'?'/blog/show-me-what-you-got-streaming-recommendations':'/blog/botdetection-part3-as-simple-as-possible';
    if(view==='proxy')await mountProxy();
    update();
    if(view==='vectors'&&deck)deck.setProps({initialViewState:camera()});
    $('scrub').step=view==='prediction'?'0.01':'1';
    if(view==='prediction'&&!prefersReducedMotion)startPlayback(true);
}
function toggleTheme(){
    document.documentElement.classList.toggle('dark');
    $('theme').textContent=document.documentElement.classList.contains('dark')?'Light mode':'Dark mode';
    syncDiagramTheme(); if(view==='vectors')drawSpace(); else if(view==='prediction'&&predictionView)update();
}
function postSize(){
    if(window.parent!==window) requestAnimationFrame(()=>window.parent.postMessage({type:'mostlylucid-explainer-size',height:Math.ceil(document.body.getBoundingClientRect().height)+4},location.origin));
}
function startPlayback(loop=false){
    if(cursor>=max()){cursor=0;idle=0;update();}
    let held=0;
    timer=setInterval(()=>{
        if(view!=='prediction'){advance();return;}
        if(!predictionVisible||document.hidden)return;
        if(cursor>=max()){
            if(!loop){pause();return;}
            if(++held<45)return;
            cursor=0;held=0;
        }else cursor=Math.min(max(),cursor+.022);
        update();
    },view==='prediction'?33:1500);
    $('play').textContent='Pause';
}
$('play').onclick=()=>{if(timer){pause();return;}startPlayback();};
$('next').onclick=()=>{pause();advance();};$('back').onclick=()=>{pause();cursor--;idle=0;update();};
$('reset').onclick=()=>{pause();cursor=0;idle=0;selected=null;predictionView?.reset();$('point-details').open=false;$('point-detail').textContent='';update();};
$('scrub').oninput=()=>{pause();cursor=Number($('scrub').value);idle=0;update();};
for(const id of ['category','budget','available','half-life','cart-weight'])$(id).addEventListener('input',update);
$('idle').onclick=()=>{pause();idle+=30;update();};
$('scenario').onchange=()=>{pause();cursor=0;update();};
$('dimension').onclick=()=>{threeD=!threeD;$('dimension').textContent=threeD?'Return to 2D':'Explore in 3D';$('dimension').setAttribute('aria-pressed',String(threeD));drawSpace();deck?.setProps({initialViewState:camera()});};
$('interact').onclick=()=>{interactive=!interactive;$('interact').setAttribute('aria-pressed',String(interactive));$('interact').textContent=interactive?'Disable plot interaction':'Enable plot interaction';$('plot').classList.toggle('interacting',interactive);$('plot-hint').textContent=interactive?'Drag to explore; disable interaction to return to page scrolling.':'Scroll the page normally. Select a point to inspect it.';deck?.setProps({controller:controller()});};
$('reset-camera').onclick=()=>deck?.setProps({initialViewState:camera()});
$('theme').onclick=toggleTheme;
$('expand').onclick=async()=>{if(document.fullscreenElement)await document.exitFullscreen();else if(document.documentElement.requestFullscreen)await document.documentElement.requestFullscreen();};
document.querySelectorAll('nav [data-view]').forEach(b=>b.onclick=()=>switchView(b.dataset.view));
if(params.get('embed')==='1')document.documentElement.classList.add('embed');
if(matchMedia('(prefers-color-scheme: dark)').matches)toggleTheme();
const sizeObserver=new ResizeObserver(()=>{postSize();}); sizeObserver.observe(document.body);
let resizeTimeout;
addEventListener('resize',()=>{clearTimeout(resizeTimeout);resizeTimeout=setTimeout(async()=>{if(view==='proxy')await mountProxy();else if(view==='vectors'&&deck)deck.setProps({initialViewState:camera()});update();},150);});
addEventListener('pagehide',()=>{pause();clearTimeout(resizeTimeout);sizeObserver.disconnect();predictionVisibility.disconnect();deck?.finalize();deck=null;predictionView?.dispose();cleanupMermaidEnhancements();proxyDirection=null;});
// Safari/Chromium may restore this document from the back/forward cache without re-running the module.
addEventListener('pageshow',async event=>{if(!event.persisted)return;sizeObserver.observe(document.body);predictionVisibility.observe($('prediction-plot'));if(view==='proxy')await mountProxy();update();});
addEventListener('message',event=>{if(event.origin===location.origin&&event.source===window.parent&&event.data?.type==='mostlylucid-explainer-theme'){
    document.documentElement.classList.toggle('dark',event.data.dark===true);$('theme').textContent=event.data.dark===true?'Light mode':'Dark mode';syncDiagramTheme();if(view==='vectors')drawSpace();else if(view==='prediction'&&predictionView)update();
}});
await switchView(view);
