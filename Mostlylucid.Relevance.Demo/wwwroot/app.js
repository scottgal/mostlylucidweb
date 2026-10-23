const $ = selector => document.querySelector(selector);
const escape = value => String(value).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const money = value => new Intl.NumberFormat('en-GB', {style:'currency',currency:'GBP',maximumFractionDigits:0}).format(value);
let session = sessionStorage.getItem('fieldwork-session') || crypto.randomUUID();
sessionStorage.setItem('fieldwork-session', session);
let products = [], category = 'all', budget = 250, currentTab = 'shop', eventCount = 0;
let latestRecommendation, noticeTimer, selectedProduct;
const basket = new Map(), attribution = new Map(), comparisons = new Set();

async function api(path, body) {
  const response = await fetch('/api/' + path, body === undefined ? {} : {method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
  const data = await response.json();
  if (!response.ok) throw new Error(data.detail || data.error || 'Request failed.');
  return data;
}
function notice(message) { $('#notice').textContent=message; $('#notice').hidden=false; clearTimeout(noticeTimer); noticeTimer=setTimeout(()=>$('#notice').hidden=true,4500); }
function fail(error) { console.error(error); notice(error.message); }

function icon(p) {
  const paths = {
    pack:'<path d="M31 30c0-20 34-20 34 0" fill="none"/><rect x="22" y="24" width="52" height="61" rx="15"/><rect x="32" y="54" width="32" height="22" rx="6" opacity=".7"/><path d="M40 29v14h16V29" fill="none" stroke="#eee9da"/>',
    boot:'<path d="M29 15h30l-1 39 22 10q8 4 5 14H15V61h13z"/><path d="M15 78h70v7H15z" opacity=".7"/><path d="M37 29h16m-16 9h16m-16 9h16" stroke="#e5e0d0" fill="none"/>',
    coat:'<path d="M33 17l15 8 15-8 22 20-14 16-9-8v40H34V45l-9 8-14-16z"/><path d="M48 25v60M34 64h9m10 0h9" stroke="#e7e5d9" fill="none"/>',
    bottle:'<rect x="36" y="10" width="24" height="13" rx="3" opacity=".8"/><path d="M36 23h24v12q10 5 10 14v35H26V49q0-9 10-14z"/><path d="M30 60h36" stroke="#d6dacc" fill="none"/>',
    cap:'<path d="M21 57q0-40 29-40t29 40z"/><path d="M21 57h58q13 0 13 11H24q-12 0-3-11z" opacity=".75"/><path d="M50 18v37" fill="none" stroke="#dadbca"/>',
    headphones:'<path d="M20 60V43q0-37 28-37t28 37v17" fill="none" stroke-width="9"/><rect x="12" y="44" width="19" height="35" rx="8"/><rect x="65" y="44" width="19" height="35" rx="8"/>',
    speaker:'<rect x="19" y="13" width="58" height="74" rx="12"/><circle cx="48" cy="55" r="19" fill="#f7f5e5" opacity=".4"/><circle cx="48" cy="55" r="13"/><circle cx="48" cy="26" r="3" fill="#eee8d5"/>',
    mug:'<path d="M65 32h7q19 0 14 19t-21 13" fill="none" stroke-width="7"/><path d="M18 24h49v45q0 15-24 15T18 69z"/><ellipse cx="42" cy="24" rx="24" ry="6" opacity=".7"/>',
    kettle:'<path d="M56 25q30-10 30 18t-20 19" fill="none" stroke-width="7"/><path d="M29 23h29l17 55q-20 14-54 0z"/><path d="M23 57L7 30l20 8"/><path d="M32 19h23" fill="none" stroke-width="5"/>',
    lamp:'<path d="M45 38h7v43H45z"/><ellipse cx="48" cy="84" rx="25" ry="5"/><path d="M27 14h42l14 35H13z"/><path d="M18 48h60" fill="none" stroke="#e4dfc7"/>'
  };
  return `<svg viewBox="0 0 96 100" fill="${escape(p.swatch)}" stroke="${escape(p.swatch)}" stroke-width="2" stroke-linejoin="round" aria-hidden="true">${paths[p.shape] || paths.pack}</svg>`;
}
function thumb(p, extra='') { return `<div class="thumb" style="background:${p.category==='outdoors'?'#e8ecdf':p.category==='electronics'?'#e8e8e2':'#eee7dc'}">${extra}${icon(p)}</div>`; }
function card(p) {
  return `<article class="product-card" data-product="${p.id}"><button class="product-open" data-open="${p.id}" aria-label="View ${escape(p.name)} in ${escape(p.colour)}">${thumb(p,`<span class="tag">${escape(p.category.toUpperCase())}</span>${p.available?'':'<span class="sold-out">Currently unavailable</span>'}`)}</button><div class="product-info"><div class="product-line"><strong>${escape(p.name)}</strong><span>${money(p.price)}</span></div><small><i class="swatch" style="background:${p.swatch}"></i>${escape(p.colour)}</small><div class="product-actions"><button class="compare" data-compare="${p.id}" aria-pressed="${comparisons.has(p.id)}">${comparisons.has(p.id)?'Comparing ✓':'Compare +'}</button><button data-cart="${p.id}" ${p.available?'':'disabled'}>Add to basket +</button></div></div></article>`;
}
function renderProducts() { const shown=products.filter(p=>(category==='all'||p.category===category)&&p.price<=budget); $('#products').innerHTML=shown.length?shown.map(card).join(''):'<p class="empty">No finds in this range. Try widening your budget.</p>'; $('#product-total').textContent=shown.length; }
async function refreshRecommendations() {
  const requestedCategory=category, requestedBudget=budget;
  const result=await api(`shop/recommendations?session=${encodeURIComponent(session)}&category=${category}&budget=${budget}`);
  if(requestedCategory!==category || requestedBudget!==budget) return;
  latestRecommendation=result;
  $('#recommendation-note').textContent=result.items.some(i=>i.reasons.length)?'Matched from this visit: category, price, and colour.':'A starting selection. Browse something to make it yours.';
  $('#recommendations').innerHTML=result.items.length?result.items.map(item=>{const p=products.find(p=>p.id===item.id);if(!p)return ''; return `<button class="rec" data-recommend="${p.id}" title="${escape(item.reasons.map(r=>`${r.head}: rank ${r.rank}, distance ${r.distance.toFixed(3)}`).join('; ') || 'Eligible cold-start selection')}">${thumb(p)}<div><strong>${escape(p.name)}</strong><small>${escape(p.colour)} · ${item.reasons.length?'Matched to your visit':'Explore a new find'}</small><div class="price">${money(p.price)}</div></div></button>`;}).join(''):'<p class="empty">No eligible picks within these filters.</p>';
}
function probabilities(prediction) {
  if(!prediction) return '<p class="micro">Waiting for observations.</p>';
  const rows=[...prediction.outcomes.map(o=>({name:o.destination,value:o.probability})),{name:'Unresolved',value:prediction.remainingProbability}];
  return rows.map(r=>`<div class="prob-row"><span>${escape(r.name)}</span><div class="prob-track"><div class="prob-fill" style="width:${Math.max(0,Math.min(100,r.value*100))}%"></div></div><b>${Math.round(r.value*100)}%</b></div>`).join('')+`<p class="micro">${prediction.status==='observed_destination'?'Destination observed.':prediction.status==='estimated'?`${prediction.supportingTransitions} outgoing transitions; uncalibrated.`:'Not enough transition evidence for this state.'}</p>`;
}
async function record(product,type,recommendation) {
  const result=await api('shop/events',{id:crypto.randomUUID(),session,product,type,recommendation});
  eventCount++; $('#event-count').textContent=eventCount;
  $('#shop-prediction').innerHTML=probabilities(result.prediction);
  await refreshRecommendations();
}
async function openProduct(id,recommendation) {
  const p=products.find(p=>p.id===id); selectedProduct=p;
  if(recommendation) attribution.set(id,recommendation);
  $('#product-detail').innerHTML=`<div class="detail-layout">${thumb(p)}<div><p class="eyebrow">${escape(p.category.toUpperCase())}</p><h2>${escape(p.name)}</h2><p>${escape(p.description)}</p><p><i class="swatch" style="background:${p.swatch}"></i>${escape(p.colour)} · ${money(p.price)}</p><div class="detail-buttons"><button class="primary" data-cart="${p.id}" ${p.available?'':'disabled'}>Add to basket</button><button data-compare="${p.id}">Compare +</button></div></div></div>`;
  $('#product-dialog').showModal();
  await record(id,'view');
}
async function addToBasket(id) { await record(id,'cart'); basket.set(id,products.find(p=>p.id===id)); renderBasket(); notice('Added to your basket. Your next picks have updated.'); }
function renderBasket() { $('#cart-count').textContent=basket.size; $('#basket-items').innerHTML=basket.size?[...basket.values()].map(p=>`<div class="basket-item"><span>${escape(p.name)} · ${escape(p.colour)}<br><small>${money(p.price)}</small></span><button data-remove="${p.id}">Remove</button></div>`).join(''):'<p class="empty">A little room for something good.</p>'; $('#checkout').disabled=!basket.size; $('#cancel-checkout').disabled=!basket.size; }
async function compare(id) { await record(id,'compare'); comparisons.add(id); renderProducts(); const names=[...comparisons].slice(-3).map(id=>products.find(p=>p.id===id)); let tray=$('#compare-tray'); if(!tray){tray=document.createElement('section');tray.id='compare-tray';tray.className='compare-tray';$('.catalogue').prepend(tray);} tray.innerHTML='<span class="eyebrow">YOUR COMPARISON</span><div>'+names.map(p=>`<div><strong>${escape(p.name)}</strong><small>${escape(p.colour)} · ${money(p.price)} · ${p.available?'Available':'Unavailable'}</small></div>`).join('')+'</div>'; notice('Comparison recorded. Your picks have updated.'); }
async function finish(type) { if(!basket.size)return; $('#checkout').disabled=true;$('#cancel-checkout').disabled=true;try {if(type==='purchase'){for(const p of basket.values())await record(p.id,type,attribution.get(p.id));}else await record([...basket.keys()][0],type);basket.clear();renderBasket();$('#basket-dialog').close();notice(type==='purchase'?'Demo purchase complete. That outcome is now part of the evidence.':'Basket cancelled. The destination observation has been recorded.');}finally{renderBasket();} }

document.addEventListener('click',event=>{
  const button=event.target.closest('button'); if(!button)return;
  const run=async()=>{
    if(button.dataset.open) await openProduct(button.dataset.open);
    if(button.dataset.recommend) await openProduct(button.dataset.recommend,latestRecommendation.id);
    if(button.dataset.cart) await addToBasket(button.dataset.cart);
    if(button.dataset.compare) await compare(button.dataset.compare);
    if(button.dataset.remove){await record(button.dataset.remove,'remove');basket.delete(button.dataset.remove);renderBasket();}
    if(button.dataset.category){category=button.dataset.category;document.querySelectorAll('[data-category]').forEach(b=>b.classList.toggle('selected',b===button));renderProducts();await refreshRecommendations();}
    if(button.dataset.tab){currentTab=button.dataset.tab;document.querySelectorAll('[data-tab]').forEach(b=>b.classList.toggle('active',b===button));$('#shop-view').hidden=currentTab!=='shop';$('#operations-view').hidden=currentTab!=='operations';if(currentTab==='operations')await refreshOperations();}
    if(button.dataset.scenario){button.disabled=true;try{await api('operations/scenarios/'+button.dataset.scenario,{});await refreshOperations();notice('Sample trace processed. Diagnostic recommendations updated.');}finally{button.disabled=false;}}
    if(button.dataset.action){button.disabled=true;try{const result=await api('operations/actions/'+button.dataset.action,{});notice(result.message);await refreshOperations();}finally{button.disabled=false;}}
  };run().catch(fail);
});
$('#cart-toggle').onclick=()=>{renderBasket();$('#basket-dialog').showModal();};
$('.close-dialog').onclick=()=>$('#product-dialog').close();$('.close-basket').onclick=()=>$('#basket-dialog').close();
$('#checkout').onclick=()=>finish('purchase').catch(fail);$('#cancel-checkout').onclick=()=>finish('cancel').catch(fail);
$('#budget').addEventListener('input',()=>{budget=Number($('#budget').value);$('#budget-label').textContent=money(budget);renderProducts();});
$('#budget').addEventListener('change',()=>refreshRecommendations().catch(fail));
$('#new-session').onclick=()=>{session=crypto.randomUUID();sessionStorage.setItem('fieldwork-session',session);eventCount=0;comparisons.clear();attribution.clear();basket.clear();renderBasket();$('#event-count').textContent='0';$('#shop-prediction').textContent='Browse a product to begin.';$('#compare-tray')?.remove();renderProducts();refreshRecommendations().catch(fail);notice('A fresh visit. Earlier session evidence expires separately.');};
$('#reset-policy').onclick=()=>api('operations/reset',{}).then(()=>refreshOperations()).then(()=>notice('Shop request limit reset to 240 per minute.')).catch(fail);

let refreshingOps=false;
async function refreshOperations(){
  if(refreshingOps)return;refreshingOps=true;
  try{
    const {telemetry:t,policy}=await api('operations');
    $('#state-badge').textContent=t.state.toUpperCase();$('#state-badge').className='state '+t.state;
    $('#latency').innerHTML=`${Math.round(t.metrics.latencyMs)} <small>ms</small>`;$('#error-rate').textContent=Math.round(t.metrics.errorFraction*100)+'%';$('#retries').textContent=t.metrics.retries.toFixed(1);$('#queue').textContent=Math.round(t.metrics.queueDepth);
    $('#rate-limit').textContent=policy.limit;$('#ops-prediction').innerHTML=probabilities(t.prediction);$('#log-count').textContent=t.observations+' measurements';
    $('#actions').innerHTML=t.suggestions?.items.length?t.suggestions.items.map((item,i)=>`<article class="action"><strong>${i+1}. ${escape(item.data.name)}</strong><p>${escape(item.data.evidence)}</p><button data-action="${item.id}">${item.id==='tighten-rate-limit'?'Apply local limit':'Inspect snapshot'} ↗</button><small>${item.reasons.length} evidence heads</small></article>`).join(''):'<p class="empty">No incident needs attention. Replay a sample trace to see how the same engine matches diagnostic actions.</p>';
    $('#interventions').innerHTML=policy.interventions.length?policy.interventions.map(i=>`<div class="intervention"><strong>${escape(i.action)} · ${new Date(i.at).toLocaleTimeString()}</strong><span>${escape(i.message)}</span><br><small>At intervention: ${Math.round(i.before.latencyMs)}ms latency, ${Math.round(i.before.queueDepth)} queued. Current: ${Math.round(t.metrics.latencyMs)}ms, ${Math.round(t.metrics.queueDepth)} queued. Observed change only.</small></div>`).join(''):'<p class="empty">Apply an eligible action to record the context and what changed.</p>';
    $('#logs').innerHTML=t.logs.map(l=>`<div class="log-row"><time>${new Date(l.at).toLocaleTimeString()}</time><span class="${l.synthetic?'synthetic':''}">${l.synthetic?'SAMPLE':escape(l.source)}</span><span>${escape(l.message)}</span><span class="${l.status>=400?'status-error':''}">${l.status}</span></div>`).join('')||'<p class="empty">Waiting for the first request.</p>';
  }finally{refreshingOps=false;}
}
setInterval(()=>{if(currentTab==='operations')refreshOperations().catch(fail);},2500);
try{products=await api('shop/products');renderProducts();renderBasket();await refreshRecommendations();}catch(error){fail(error);}
