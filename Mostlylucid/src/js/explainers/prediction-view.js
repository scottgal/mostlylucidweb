import {Deck, OrthographicView, COORDINATE_SYSTEM} from '@deck.gl/core';
import {ScatterplotLayer, PathLayer, TextLayer, PolygonLayer} from '@deck.gl/layers';
import {destinations, outcomeLabels, objectives, predictionReplay, predictionSnapshot, comparisonSnapshot, trajectoryComparisonPair,
    probabilities, utilityRange, isOperations, normalReference, projectCoordinates, formatDuration, formatClock, timeOrigin} from './prediction-model';

const $ = id => document.getElementById(id);
const percent = value => `${(value * 100).toFixed(1)}%`;
const milestones = isOperations ? [{name:'LATENCY INCREASES →',position:[500,850]}, {name:'ERROR RATE INCREASES ↓',position:[500,60]}]
    : [{name:'CHECKOUT ENGAGEMENT →',position:[500,850]}, {name:'FRICTION INCREASES ↓',position:[500,60]}];

/** Observed-state tracks and real C# forecasts share a playhead, but display coordinates do not produce probabilities. */
export function createPredictionView(onChange) {
    let deck = null, failed = false, selected = 'a', current = [], inspectorKey = '', tableKey = '', comparing = null, spread = true;
    const plot = $('prediction-plot'), fallback = $('prediction-fallback');
    plot.dataset.replayVersion = predictionReplay.version;
    const choose = id => { selected = id; $('prediction-subject').value = id; onChange(); };
    $('prediction-users').innerHTML = `<label>Inspect a journey<select id="prediction-subject">${predictionReplay.participants.map((p,i) =>
        `<option value="${p.id}">${String(i+1).padStart(2,'0')} / ${p.name}</option>`).join('')}</select></label><span>Click any moving ${isOperations?'service':'user'} or trail. All 24 tracks stay visible.</span>`;
    $('prediction-subject').onchange = e => choose(e.target.value);
    $('prediction-objective').innerHTML = Object.entries(objectives).map(([key,value])=>`<option value="${key}">${value.name}</option>`).join('');
    $('prediction-objective').onchange = onChange;
    $('prediction-spread').onclick = () => { spread=!spread; $('prediction-spread').setAttribute('aria-pressed',String(spread)); onChange(); };
    $('prediction-domain').value = isOperations ? 'operations' : 'shop';
    $('prediction-domain').onchange = event => {
        const url = new URL(location.href); url.searchParams.set('domain',event.target.value); location.assign(url.href);
    };
    $('prediction-domain-heading').textContent = isOperations ? '24 services. Same metrics, different trajectories.' : '24 users. Watch their journeys diverge.';
    const highlighted = person => comparing ? person.id === 'a' || person.id === (comparing==='pace'?'c':'b') : person.id === selected;
    function endComparison() {
        comparing = null;
        $('prediction-compare').setAttribute('aria-pressed', 'false');
        $('prediction-pace').setAttribute('aria-pressed', 'false');
        $('prediction-pair').hidden = true;
    }
    function compare(mode) {
        if (comparing===mode) endComparison();
        else {
            comparing = mode;
            $('prediction-compare').setAttribute('aria-pressed',String(mode==='direction'));
            $('prediction-pace').setAttribute('aria-pressed',String(mode==='pace'));
            $('prediction-pair').hidden = false;
            $('prediction-pair-note').textContent = mode==='pace'
                ? 'Same observed position, different pace. Each visit is pinned at its own landmark. Forecasts ask about the next 60 seconds.'
                : 'Same observed position and state, opposite directions. Both forecasts use measured velocity, acceleration and elapsed time.';
            $('prediction-pair-cards').innerHTML = trajectoryComparisonPair(mode).map(person => {
                const values = probabilities(person.frame.prediction);
                const motion = person.frame.prediction.trajectory;
                return `<div class="comparison-card" style="border-color:rgb(${person.color})"><strong>${person.name}</strong><p>Position [${motion.position.map(v=>v.toFixed(3)).join(', ')}]<br>First-coordinate velocity ${motion.velocity[0].toFixed(5)}/s<br>Elapsed ${formatDuration(motion.elapsedSeconds)}</p><code>${person.frame.prediction.currentState}</code><p>${destinations.map(d=>`<b>${percent(values[d.id])} ${d.name.toLowerCase()}</b>`).join('<br>')}<br>Unresolved ${percent(values.unresolved)}</p></div>`;
            }).join('');
        }
        onChange({compare: comparing});
    }
    $('prediction-compare').onclick = () => compare('direction');
    $('prediction-pace').onclick = () => compare('pace');

    function svgPlot() {
        const width = plot.clientWidth || 600, height = plot.clientHeight || 520;
        const scaleX = (width - 40) / 1030, scaleY = (height - 45) / 950;
        const xy = p => [width / 2 + (p[0] - 510) * scaleX, height / 2 - (p[1] - 470) * scaleY];
        const regions = destinations.map(d => {
            const [x,y] = xy([d.position[0]-75,d.position[1]+75]);
            return `<rect x="${x}" y="${y}" width="${155*scaleX}" height="${150*scaleY}" rx="12" fill="rgb(${d.color})" fill-opacity=".13"/>
                <text x="${x+77*scaleX}" y="${y+15}" text-anchor="middle" fill="currentColor">${d.name.toUpperCase()}</text>`;
        }).join('');
        const labels = milestones.map(m=>{const [x,y]=xy(m.position);return `<text x="${x}" y="${y}" text-anchor="middle" fill="currentColor" opacity=".6">${m.name}</text>`;}).join('');
        const paths = current.map(p => {
            const [x,y]=xy(p.position), chosen=highlighted(p);
            return `<polyline data-user="${p.id}" points="${p.trail.map(point=>xy(point).join(',')).join(' ')}" fill="none" stroke="rgb(${p.color})" stroke-width="${chosen?3.5:1.8}" opacity="${chosen?1:.55}"/>
                <circle data-user="${p.id}" cx="${x}" cy="${y}" r="${chosen?7:4}" fill="rgb(${p.color})" stroke="var(--paper)" stroke-width="1.5" tabindex="0" role="button" aria-label="Inspect journey ${p.index+1}"><title>${p.name}: ${p.frame.event??'arriving'}</title></circle>`;
        }).join('');
        const normal = normalReference ? (()=>{
            const [x,y]=xy(projectCoordinates(normalReference.centroid));
            return `<circle cx="${x}" cy="${y}" r="16" fill="none" stroke="var(--ink)" stroke-width="2"/><text x="${x}" y="${y+30}" fill="currentColor" text-anchor="middle">LEARNED NORMAL</text>`;
        })():'';
        fallback.innerHTML=`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" role="group" aria-label="24 timestamped behavioural trajectories">${regions}${labels}${normal}${paths}</svg>`;
        fallback.querySelectorAll('[data-user]').forEach(node=>{
            node.onclick=()=>choose(node.dataset.user);
            node.onkeydown=e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();choose(node.dataset.user);}};
        });
    }
    function showFallback() {
        failed=true;deck?.finalize();deck=null;fallback.hidden=false;
        $('prediction-renderer').textContent='Behaviour tracks / SVG';
    }
    function draw() {
        svgPlot();
        if(failed)return;
        const camera={target:[510,470,0],zoom:0,zoomX:Math.log2((plot.clientWidth-40)/1030),zoomY:Math.log2((plot.clientHeight-45)/950)};
        const ink=document.documentElement.classList.contains('dark')?[224,235,227]:[35,58,50];
        const common={coordinateSystem:COORDINATE_SYSTEM.CARTESIAN};
        try {
            if(!deck)deck=new Deck({parent:plot,views:new OrthographicView({flipY:false,near:-2000,far:2000}),
                initialViewState:camera,controller:false,onError:showFallback,onLoad:()=>{if(!failed)fallback.hidden=true;},
                getTooltip:({object})=>object?.frame?`${object.name}\n${object.events.join(' → ')||'Arriving'}`:object?.name});
            const points=current.flatMap(p=>p.trail.slice(1,-1).map(position=>({position,color:p.color})));
            deck.setProps({initialViewState:camera,layers:[
                new PolygonLayer({...common,id:'destination-bands',data:destinations,
                    getPolygon:d=>[[d.position[0]-75,d.position[1]-75],[d.position[0]+75,d.position[1]-75],[d.position[0]+75,d.position[1]+75],[d.position[0]-75,d.position[1]+75]],
                    getFillColor:d=>[...d.color,32],stroked:false}),
                new TextLayer({...common,characterSet:'auto',id:'destination-labels',data:destinations,getPosition:d=>[d.position[0],d.position[1]+52],
                    getText:d=>d.name.toUpperCase(),getColor:ink,getSize:12}),
                new ScatterplotLayer({...common,id:'normal-centroid',data:normalReference?[normalReference]:[],
                    getPosition:d=>projectCoordinates(d.centroid),radiusUnits:'pixels',getRadius:16,
                    getFillColor:[0,0,0,0],stroked:true,getLineColor:ink,lineWidthUnits:'pixels',getLineWidth:2}),
                new TextLayer({...common,characterSet:'auto',id:'normal-label',data:normalReference?[normalReference]:[],getPosition:d=>projectCoordinates(d.centroid),
                    getText:()=> 'LEARNED NORMAL',getColor:ink,getSize:10,getPixelOffset:[0,30]}),
                new TextLayer({...common,characterSet:'auto',id:'behaviour-labels',data:milestones,getPosition:d=>d.position,getText:d=>d.name,getColor:[...ink,140],getSize:10}),
                new PathLayer({...common,id:'behaviour-trails',data:current,pickable:true,getPath:p=>p.trail,
                    getColor:p=>[...p.color,highlighted(p)?255:145],widthUnits:'pixels',getWidth:p=>highlighted(p)?3.5:1.8,
                    onClick:({object})=>object&&choose(object.id)}),
                new ScatterplotLayer({...common,id:'observed-events',data:points,getPosition:p=>p.position,
                    getFillColor:p=>[...p.color,160],radiusUnits:'pixels',getRadius:2.5}),
                new ScatterplotLayer({...common,id:'moving-users',data:current,pickable:true,getPosition:p=>p.position,
                    getFillColor:p=>[...p.color,p.waiting?90:255],radiusUnits:'pixels',getRadius:p=>highlighted(p)?8:5,
                    stroked:true,getLineColor:ink,lineWidthUnits:'pixels',getLineWidth:p=>highlighted(p)?2:1,
                    onClick:({object})=>object&&choose(object.id)}),
                new TextLayer({...common,characterSet:'auto',id:'selected-user',data:current.filter(highlighted),getPosition:p=>p.position,
                    getText:p=>`${String(p.index+1).padStart(2,'0')} / ${(p.frame.event??'arriving').toUpperCase()}`,
                    getColor:p=>p.color,getSize:12,getPixelOffset:p=>comparing?(p.id==='a'?[12,25]:[12,-25]):[12,-16],getTextAnchor:'start',
                    updateTriggers:{getPixelOffset:comparing}})
            ]});
        } catch {showFallback();}
    }
    return {
        update(cursor) {
            current=comparing?comparisonSnapshot(cursor,comparing,spread):predictionSnapshot(cursor,spread);
            $('prediction-clock').textContent = comparing ? 'Separate visit landmarks / 60-second forecasts' : `${formatClock(timeOrigin+cursor*1000)} / elapsed ${formatDuration(cursor)}`;
            const counts=destinations.map(d=>`${current.filter(p=>p.frame.prediction.currentState===d.id).length} ${d.name.toLowerCase()}`);
            $('prediction-live').textContent=`${current.filter(p=>!p.waiting&&!p.finished).length} moving · ${counts.join(' · ')} · ${current.filter(p=>p.finished&&p.frame.prediction.status!=='observed_destination').length} incomplete`;
            const person=current.find(p=>p.id===selected),prediction=person.frame.prediction;
            const objective=objectives[$('prediction-objective').value];
            const key=`${selected}:${person.events.length}:${$('prediction-objective').value}:${person.forecastExpired}`;
            if(key!==inspectorKey){
                inspectorKey=key;
                $('prediction-title').textContent=person.name;
                $('prediction-prefix').textContent=person.events.join(' → ')||'Arriving';
                $('prediction-state').textContent=prediction.currentState??'No state';
                $('prediction-support').textContent=prediction.status==='observed_destination'?'Observed result; no population match needed':`${prediction.supportingTransitions} matched episode landmarks`;
                $('prediction-status').textContent=prediction.status==='observed_destination'?'Observed endpoint':person.forecastExpired?'Forecast window elapsed; waiting for another observation':prediction.status==='no_trajectory'?'Waiting for evidence':prediction.status==='insufficient_motion'?'Collecting timestamped motion evidence':prediction.status==='insufficient_trajectory_evidence'?'Insufficient matched outcomes; abstaining':'Forecast within 60 seconds of the observation';
                const motion=prediction.trajectory;
                $('prediction-window').textContent=prediction.observedAt?(prediction.status==='observed_destination'?`Observed at ${formatClock(prediction.observedAt)}`:`${formatClock(prediction.observedAt)} to ${formatClock(Date.parse(prediction.observedAt)+predictionReplay.horizonSeconds*1000)}`):'No observation yet';
                $('prediction-motion').textContent=motion?`Centroid [${motion.centroid.map(v=>v.toFixed(3)).join(', ')}] · velocity ${motion.velocity?'['+motion.velocity.map(v=>v.toFixed(5)).join(', ')+']/s':'not yet known'} · acceleration ${motion.acceleration?'['+motion.acceleration.map(v=>v.toFixed(6)).join(', ')+']/s²':'not yet known'} · elapsed ${formatDuration(motion.elapsedSeconds)}`:'';
                $('prediction-normal').hidden=!motion?.normal;
                if(motion?.normal)$('prediction-normal').textContent=`Normal centroid [${motion.normal.centroid.map(v=>v.toFixed(4)).join(', ')}] · deviation ${motion.normal.deviation?.toFixed(2)??'insufficient normal evidence'} · effective mass ${motion.normal.effectiveMass.toFixed(1)} · reference half-life 24 hours. Deviation is a distance, not a probability.`;
                const values=probabilities(prediction);
                $('prediction-values').innerHTML=outcomeLabels.map(d=>`<div class="value-row"><span>${d.name}<strong>${percent(values[d.id])}</strong></span><div class="track"><i style="width:${values[d.id]*100}%;background:rgb(${d.color})"></i></div></div>`).join('');
                const [low,high]=utilityRange(prediction,objective);
                $('prediction-utility').textContent=Math.abs(high-low)<1e-9?low.toFixed(3):`${low.toFixed(3)} to ${high.toFixed(3)}`;
                $('prediction-utility-note').textContent='Preferences do not change the observed track or its probabilities. A range retains unresolved value.';
                $('prediction-destinations').innerHTML=destinations.map(d=>`<span>${d.name} <strong>${objective.utilities[d.id]}</strong></span>`).join('');
            }
            const nextTable=current.map(p=>p.events.length).join(',');
            if(nextTable!==tableKey){
                tableKey=nextTable;
                $('data-table').innerHTML=current.map(p=>{const v=probabilities(p.frame.prediction);return `<tr><td>${p.index+1} / ${p.name}</td><td>${p.events.join(' → ')||'Arriving'}</td><td>${outcomeLabels.map(d=>`${d.name} ${percent(v[d.id])}`).join('; ')}</td></tr>`;}).join('');
            }
            draw();
        },
        label:cursor=>comparing?`Same position / compare ${comparing}`:`24 ${isOperations?'service':'shopper'} trajectories / ${formatDuration(cursor)} of observed time`,
        endComparison,
        reset(){endComparison();selected='a';$('prediction-subject').value='a';inspectorKey='';},
        dispose(){deck?.finalize();deck=null;}
    };
}
