import {Deck, OrthographicView, COORDINATE_SYSTEM} from '@deck.gl/core';
import {ScatterplotLayer, PathLayer, TextLayer} from '@deck.gl/layers';
import {destinations, objectives, predictionReplay, predictionSnapshot, probabilities, utilityRange} from './prediction-model';

const $ = id => document.getElementById(id);
const percent = value => `${(value * 100).toFixed(1)}%`;
const reducedMotion = () => matchMedia('(prefers-reduced-motion: reduce)').matches;

/** A renderer for captured C# forecasts. Selection and objectives never mutate the replay or train a browser model. */
export function createPredictionView(onChange) {
    let deck = null, failed = false, selected = 'a', current = [];
    const plot = $('prediction-plot'), fallback = $('prediction-fallback');
    const choose = id => { selected = id; onChange(); };
    $('prediction-users').innerHTML = predictionReplay.participants.map(p =>
        `<button data-user="${p.id}" aria-pressed="${p.id === selected}">${p.name}</button>`).join('');
    $('prediction-users').querySelectorAll('button').forEach(button => button.onclick = () => choose(button.dataset.user));
    $('prediction-objective').onchange = onChange;

    function svgPlot() {
        const width = plot.clientWidth || 600, height = plot.clientHeight || 420;
        const scale = Math.min((width - 40) / 1000, (height - 70) / 1000);
        const xy = p => [width / 2 + (p[0] - 500) * scale, height / 2 - (p[1] - 470) * scale];
        const anchors = destinations.map(d => {
            const [x, y] = xy(d.position);
            return `<circle cx="${x}" cy="${y}" r="32" fill="rgb(${d.color})" fill-opacity=".12" stroke="rgb(${d.color})"/>
                <text x="${x}" y="${y + 47}" text-anchor="middle" fill="currentColor">${d.name}</text>`;
        }).join('');
        const nodes = [...current].reverse().map(p => {
            const [x, y] = xy(p.position);
            const points = p.trail.map(point => xy(point).join(',')).join(' ');
            return `<polyline points="${points}" fill="none" stroke="rgb(${p.color})" stroke-width="${p.id === selected ? 3 : 1.5}" opacity=".7"/>
                <circle data-user="${p.id}" cx="${x}" cy="${y}" r="${10 + p.index * 3}" fill="rgb(${p.color})" stroke="var(--paper)" stroke-width="${p.id === selected ? 3 : 1}" tabindex="0" role="button" aria-label="Inspect user ${p.id.toUpperCase()}"><title>${p.name}</title></circle>
                <text x="${x + 21}" y="${y - 18 - p.index * 15}" fill="currentColor">${p.id.toUpperCase()}</text>`;
        }).join('');
        fallback.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" role="group" aria-label="Forecast map with destination anchors and three user trails">${anchors}${nodes}</svg>`;
        fallback.querySelectorAll('[data-user]').forEach(node => {
            node.onclick = () => choose(node.dataset.user);
            node.onkeydown = e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); choose(node.dataset.user); } };
        });
    }
    function showFallback() {
        failed = true;
        deck?.finalize(); deck = null;
        fallback.hidden = false;
        $('prediction-renderer').textContent = 'SVG forecast map';
    }
    function draw() {
        svgPlot(); // Also the printable equivalent when WebGL is available.
        if (failed) return;
        const camera = {target: [500, 470, 0], zoom: Math.log2(Math.min((plot.clientWidth - 40) / 1000, (plot.clientHeight - 70) / 1000))};
        const ink = document.documentElement.classList.contains('dark') ? [224, 235, 227] : [35, 58, 50];
        const common = {coordinateSystem: COORDINATE_SYSTEM.CARTESIAN};
        try {
            if (!deck) deck = new Deck({parent: plot, views: new OrthographicView({flipY: false, near: -2000, far: 2000}),
                initialViewState: camera, controller: false, onError: showFallback,
                onLoad: () => { if (!failed) fallback.hidden = true; },
                getTooltip: ({object}) => object?.frame ? `${object.name}\n${object.frame.prediction.currentState ?? 'No observations'}` : object?.name});
            deck.setProps({initialViewState: camera, layers: [
                new ScatterplotLayer({...common, id: 'destination-regions', data: destinations,
                    getPosition: d => d.position, radiusUnits: 'pixels', getRadius: 32,
                    getFillColor: d => [...d.color, 30], stroked: true, getLineColor: d => d.color, lineWidthUnits: 'pixels', getLineWidth: 1}),
                new PathLayer({...common, id: 'forecast-trails', data: current, getPath: p => p.trail,
                    getColor: p => [...p.color, 160], widthUnits: 'pixels', getWidth: p => p.id === selected ? 3 : 1.5}),
                new ScatterplotLayer({...common, id: 'forecast-users', data: [...current].reverse(), pickable: true,
                    getPosition: p => p.position, transitions: {getPosition: reducedMotion() ? 0 : 600},
                    getFillColor: p => p.color, radiusUnits: 'pixels', getRadius: p => 10 + p.index * 3,
                    stroked: true, getLineColor: ink, lineWidthUnits: 'pixels', getLineWidth: p => p.id === selected ? 3 : 1,
                    onClick: ({object}) => object && choose(object.id)}),
                new TextLayer({...common, id: 'destination-labels', data: destinations, getPosition: d => d.position,
                    getText: d => d.name, getColor: ink, getSize: 12, getPixelOffset: [0, 46], getTextAnchor: 'middle'}),
                new TextLayer({...common, id: 'user-labels', data: current, getPosition: p => p.position,
                    transitions: {getPosition: reducedMotion() ? 0 : 600}, getText: p => p.id.toUpperCase(), getColor: ink,
                    getSize: 12, getPixelOffset: p => [21, -18 - p.index * 15], getTextAnchor: 'start'})
            ]});
        } catch { showFallback(); }
    }
    return {
        update(cursor) {
            current = predictionSnapshot(cursor);
            const person = current.find(p => p.id === selected), prediction = person.frame.prediction;
            const objective = objectives[$('prediction-objective').value];
            $('prediction-users').querySelectorAll('button').forEach(b => b.setAttribute('aria-pressed', String(b.dataset.user === selected)));
            $('prediction-title').textContent = person.name;
            $('prediction-prefix').textContent = person.events.join(' → ') || 'No observations yet';
            $('prediction-state').textContent = prediction.currentState ?? 'No state';
            $('prediction-support').textContent = `${prediction.supportingTransitions} outgoing transitions`;
            $('prediction-status').textContent = prediction.status === 'observed_destination' ? 'Observed endpoint; no longer a forecast' :
                prediction.status === 'no_state' ? 'No state: all probability unresolved' :
                prediction.status === 'insufficient_evidence' ? 'Insufficient outgoing evidence; probability remains unresolved' :
                `Estimated within the next ${prediction.horizonEvents} events`;
            const values = probabilities(prediction);
            $('prediction-values').innerHTML = destinations.map(d => `<div class="value-row"><span>${d.name}<strong>${percent(values[d.id])}</strong></span><div class="track"><i style="width:${values[d.id] * 100}%;background:rgb(${d.color})"></i></div></div>`).join('');
            const [low, high] = utilityRange(prediction, objective);
            $('prediction-utility').textContent = Math.abs(high - low) < 1e-9 ? low.toFixed(3) : `${low.toFixed(3)} to ${high.toFixed(3)}`;
            $('prediction-utility-note').textContent = Math.abs(high - low) < 1e-9 ? 'Example preference units. The probabilities stay unchanged when the objective changes.' : 'Bounds assign the unresolved mass the lowest or highest configured destination utility. They are not a confidence interval.';
            $('prediction-destinations').innerHTML = destinations.filter(d => d.id !== 'unresolved').map(d =>
                `<span>${d.name} <strong>${objective.utilities[d.id] > 0 ? '+' : ''}${objective.utilities[d.id]}</strong></span>`).join('');
            $('data-table').innerHTML = current.map(p => {
                const v = probabilities(p.frame.prediction);
                return `<tr><td>${p.name}</td><td>${p.events.join(' → ') || 'No observations'}</td><td>Purchase ${percent(v.purchase)}; cancel ${percent(v.cancel)}; leave ${percent(v.leave)}; unresolved ${percent(v.unresolved)}</td></tr>`;
            }).join('');
            draw();
        },
        label: cursor => `Observation ${cursor}: ${predictionSnapshot(cursor).map(p => `${p.id.toUpperCase()} ${p.frame.event ?? 'waiting'}`).join('; ')}`,
        reset() { selected = 'a'; },
        dispose() { deck?.finalize(); deck = null; }
    };
}
