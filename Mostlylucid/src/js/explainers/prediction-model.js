import replay from '../../../../samples/RelevancePredict/artifacts/seed-814/replay.json';

// A forecast map, not embedding space: positions are calculated FROM the engine's probabilities.
// Four anchors preserve unresolved mass; this 2D display is not invertible. Inspect the values.
export const destinations = [
    {id: 'purchase', name: 'Purchase', position: [500, 840], color: [49, 133, 106]},
    {id: 'cancel', name: 'Cancel', position: [100, 280], color: [185, 88, 84]},
    {id: 'leave', name: 'Leave', position: [900, 280], color: [190, 126, 66]},
    {id: 'unresolved', name: 'Unresolved', position: [500, 100], color: [125, 136, 147]}
];
export const objectives = {
    purchase: {name: 'Complete purchases', utilities: {purchase: 1, cancel: -.5, leave: 0}},
    cancellation: {name: 'Avoid cancellation', utilities: {purchase: 0, cancel: -1, leave: 0}}
};
export const predictionReplay = replay;
export const predictionMax = Math.max(...replay.participants.map(p => p.frames.length - 1));
const colors = [[72, 113, 200], [177, 101, 176], [190, 143, 45]];

export function probabilities(prediction) {
    return {...Object.fromEntries(prediction.outcomes.map(o => [o.destination, o.probability])), unresolved: prediction.remainingProbability};
}
export function forecastPosition(prediction) {
    const values = probabilities(prediction);
    return [0, 1].map(axis => destinations.reduce((sum, d) => sum + values[d.id] * d.position[axis], 0));
}
/** Values apply only to resolved mass; retain bounds for the unknown portion instead of assigning zero. */
export function utilityRange(prediction, objective) {
    const values = objective.utilities;
    const known = prediction.outcomes.reduce((sum, o) => sum + o.probability * values[o.destination], 0);
    const remaining = prediction.remainingProbability;
    return [known + remaining * Math.min(...Object.values(values)), known + remaining * Math.max(...Object.values(values))];
}
/** Pure playback of captured prefixes. Shorter visits stay at their observed endpoint. */
export function predictionSnapshot(cursor) {
    const step = Math.max(0, Math.min(predictionMax, Math.trunc(cursor)));
    return replay.participants.map((p, index) => {
        const frames = p.frames.slice(0, Math.min(step + 1, p.frames.length));
        const frame = frames[frames.length - 1];
        return {...p, index, color: colors[index], frame, position: forecastPosition(frame.prediction),
            events: frames.map(f => f.event).filter(Boolean), trail: frames.map(f => forecastPosition(f.prediction))};
    });
}
