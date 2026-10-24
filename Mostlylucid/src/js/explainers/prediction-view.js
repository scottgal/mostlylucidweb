import {Deck, OrthographicView, COORDINATE_SYSTEM} from '@deck.gl/core';
import {ScatterplotLayer, PathLayer, TextLayer, PolygonLayer} from '@deck.gl/layers';
import {destinations, outcomeLabels, objectives, predictionReplay, predictionSnapshot, probabilities, utilityRange} from './prediction-model';

const $ = id => document.getElementById(id);
const percent = value => `${(value * 100).toFixed(1)}%`;
const milestones = [
    {name:'ARRIVE',position:[65,865]}, {name:'COMPARE',position:[315,865]},
    {name:'BASKET',position:[565,865]}, {name:'CHECKOUT',position:[780,865]}
];

/** Observed-state tracks and real C# forecasts share a playhead, but display coordinates do not produce probabilities. */
export function createPredictionView(onChange) {
    let deck = null, failed = false, selected = 'a', current = [], inspectorKey = '', tableKey = '';
    const plot = $('prediction-plot'), fallback = $('prediction-fallback');
    const choose = id => { selected = id; $('prediction-subject').value = id; onChange(); };
    $('prediction-users').innerHTML = `<label>Inspect a journey<select id="prediction-subject">${predictionReplay.participants.map((p,i) =>
        `<option value="${p.id}">${String(i+1).padStart(2,'0')} / ${p.name}</option>`).join('')}</select></label><span>Click any moving user or trail. All 24 tracks stay visible.</span>`;
    $('prediction-subject').onchange = e => choose(e.target.value);
    $('prediction-objective').onchange = onChange;

    function svgPlot() {
        const width = plot.clientWidth || 600, height = plot.clientHeight || 520;
        const scaleX = (width - 40) / 1030, scaleY = (height - 45) / 950;
        const xy = p => [width / 2 + (p[0] - 510) * scaleX, height / 2 - (p[1] - 470) * scaleY];
        const regions = destinations.map(d => {
            const [x,y] = xy([860,d.position[1]+75]);
            return `<rect x="${x}" y="${y}" width="${155*scaleX}" height="${150*scaleY}" rx="12" fill="rgb(${d.color})" fill-opacity=".13"/>
                <text x="${x+77*scaleX}" y="${y+15}" text-anchor="middle" fill="currentColor">${d.name.toUpperCase()}</text>`;
        }).join('');
        const labels = milestones.map(m=>{const [x,y]=xy(m.position);return `<text x="${x}" y="${y}" text-anchor="middle" fill="currentColor" opacity=".6">${m.name}</text>`;}).join('');
        const paths = current.map(p => {
            const [x,y]=xy(p.position), chosen=p.id===selected;
            return `<polyline data-user="${p.id}" points="${p.trail.map(point=>xy(point).join(',')).join(' ')}" fill="none" stroke="rgb(${p.color})" stroke-width="${chosen?3.5:1.8}" opacity="${chosen?1:.55}"/>
                <circle data-user="${p.id}" cx="${x}" cy="${y}" r="${chosen?7:4}" fill="rgb(${p.color})" stroke="var(--paper)" stroke-width="1.5" tabindex="0" role="button" aria-label="Inspect journey ${p.index+1}"><title>${p.name}: ${p.frame.event??'arriving'}</title></circle>`;
        }).join('');
        fallback.innerHTML=`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" role="group" aria-label="24 behavioural journey tracks moving towards purchase, cancellation or departure">${regions}${labels}${paths}</svg>`;
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
                    getPolygon:d=>[[860,d.position[1]-75],[1015,d.position[1]-75],[1015,d.position[1]+75],[860,d.position[1]+75]],
                    getFillColor:d=>[...d.color,32],stroked:false}),
                new TextLayer({...common,id:'destination-labels',data:destinations,getPosition:d=>[937,d.position[1]+52],
                    getText:d=>d.name.toUpperCase(),getColor:ink,getSize:12}),
                new TextLayer({...common,id:'behaviour-labels',data:milestones,getPosition:d=>d.position,getText:d=>d.name,getColor:[...ink,140],getSize:10}),
                new PathLayer({...common,id:'behaviour-trails',data:current,pickable:true,getPath:p=>p.trail,
                    getColor:p=>[...p.color,p.id===selected?255:145],widthUnits:'pixels',getWidth:p=>p.id===selected?3.5:1.8,
                    onClick:({object})=>object&&choose(object.id)}),
                new ScatterplotLayer({...common,id:'observed-events',data:points,getPosition:p=>p.position,
                    getFillColor:p=>[...p.color,160],radiusUnits:'pixels',getRadius:2.5}),
                new ScatterplotLayer({...common,id:'moving-users',data:current,pickable:true,getPosition:p=>p.position,
                    getFillColor:p=>[...p.color,p.waiting?90:255],radiusUnits:'pixels',getRadius:p=>p.id===selected?8:5,
                    stroked:true,getLineColor:ink,lineWidthUnits:'pixels',getLineWidth:p=>p.id===selected?2:1,
                    onClick:({object})=>object&&choose(object.id)}),
                new TextLayer({...common,id:'selected-user',data:current.filter(p=>p.id===selected),getPosition:p=>p.position,
                    getText:p=>`${String(p.index+1).padStart(2,'0')} · ${(p.frame.event??'arriving').toUpperCase()}`,
                    getColor:ink,getSize:12,getPixelOffset:[12,-16],getTextAnchor:'start'})
            ]});
        } catch {showFallback();}
    }
    return {
        update(cursor) {
            current=predictionSnapshot(cursor);
            const counts=destinations.map(d=>`${current.filter(p=>p.frame.prediction.currentState===d.id).length} ${d.name.toLowerCase()}`);
            $('prediction-live').textContent=`${current.filter(p=>!p.waiting&&!p.finished).length} moving · ${counts.join(' · ')} · ${current.filter(p=>p.finished&&p.frame.prediction.status!=='observed_destination').length} incomplete`;
            const person=current.find(p=>p.id===selected),prediction=person.frame.prediction;
            const objective=objectives[$('prediction-objective').value];
            const key=`${selected}:${person.events.length}:${$('prediction-objective').value}`;
            if(key!==inspectorKey){
                inspectorKey=key;
                $('prediction-title').textContent=person.name;
                $('prediction-prefix').textContent=person.events.join(' → ')||'Arriving';
                $('prediction-state').textContent=prediction.currentState??'No state';
                $('prediction-support').textContent=`${prediction.supportingTransitions} outgoing transitions`;
                $('prediction-status').textContent=prediction.status==='observed_destination'?'Observed endpoint':prediction.status==='no_state'?'Waiting for evidence':`Forecast within ${prediction.horizonEvents} events`;
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
                $('data-table').innerHTML=current.map(p=>{const v=probabilities(p.frame.prediction);return `<tr><td>${p.index+1} / ${p.name}</td><td>${p.events.join(' → ')||'Arriving'}</td><td>Purchase ${percent(v.purchase)}; cancel ${percent(v.cancel)}; leave ${percent(v.leave)}; unresolved ${percent(v.unresolved)}</td></tr>`;}).join('');
            }
            draw();
        },
        label:cursor=>`24 user journeys · replay ${Math.floor(cursor)} / ${Math.ceil(cursor)>0?'follow the moving heads':'arriving'}`,
        reset(){selected='a';$('prediction-subject').value='a';inspectorKey='';},
        dispose(){deck?.finalize();deck=null;}
    };
}
