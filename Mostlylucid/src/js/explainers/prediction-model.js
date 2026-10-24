import replay from '../../../../samples/RelevancePredict/artifacts/seed-814/replay.json';

// This is an explicitly authored behavioural state layout, not a probability simplex or learned embedding.
// Coordinates never read a future outcome. Individual display lanes keep identical prefixes visible.
export const destinations = [
    {id: 'purchase', name: 'Purchase', position: [930, 790], color: [49, 133, 106]},
    {id: 'cancel', name: 'Cancel', position: [930, 450], color: [185, 88, 84]},
    {id: 'leave', name: 'Leave', position: [930, 110], color: [190, 126, 66]}
];
export const outcomeLabels = [...destinations, {id: 'unresolved', name: 'Unresolved', color: [125, 136, 147]}];
export const behaviourStates = {
    browsing: [170, 430], comparing: [310, 650], repeated_comparison: [370, 610],
    considered_cart: [570, 750], research_cart: [520, 610], impulsive_cart: [510, 450],
    post_cart_browsing: [320, 250], cart_removed: [410, 310], reconsidering: [500, 470],
    returning_cart: [660, 590], checkout: [790, 790]
};
export const objectives = {
    purchase: {name: 'Complete purchases', utilities: {purchase: 1, cancel: -.5, leave: 0}},
    cancellation: {name: 'Avoid cancellation', utilities: {purchase: 0, cancel: -1, leave: 0}}
};
export const predictionReplay = replay;
const delay = index => index * .13;
export const predictionMax = Math.ceil(Math.max(...replay.participants.map((p, i) => p.frames.length - 1 + delay(i))));
const colors = [[72,113,200],[177,101,176],[190,143,45],[40,158,155],[205,103,77],[115,126,211],[67,154,110],[164,117,71]];

export function probabilities(prediction) {
    return {...Object.fromEntries(prediction.outcomes.map(o => [o.destination, o.probability])), unresolved: prediction.remainingProbability};
}
/** A named state plus stable per-user display offsets. No terminal label can affect an earlier position. */
export function behaviourPosition(frame, index, count = replay.participants.length) {
    const state = frame.prediction.currentState;
    if (!state) return [65, 95 + index / Math.max(1, count - 1) * 700];
    const terminal = destinations.find(d => d.id === state);
    const base = terminal?.position ?? behaviourStates[state];
    if (!base) throw new Error(`No display coordinates for state ${state}`);
    const spread = Math.sin((index + 1) * 2.39996);
    const bend = Math.cos((index + 1) * 1.618 + base[0] / 150);
    return [base[0] + bend * (terminal ? 13 : 24), base[1] + spread * (terminal ? 35 : 70)];
}
/** Values apply only to resolved mass; retain bounds for the unknown portion instead of assigning zero. */
export function utilityRange(prediction, objective) {
    const values = objective.utilities;
    const known = prediction.outcomes.reduce((sum, o) => sum + o.probability * values[o.destination], 0);
    const remaining = prediction.remainingProbability;
    return [known + remaining * Math.min(...Object.values(values)), known + remaining * Math.max(...Object.values(values))];
}
/** Smooth movement between captured observations, with staggered starts and a full visible prefix trail. */
export function predictionSnapshot(cursor) {
    const time = Math.max(0, Math.min(predictionMax, cursor));
    return replay.participants.map((p, index) => {
        const local = Math.max(0, Math.min(p.frames.length - 1, time - delay(index)));
        const at = Math.floor(local), fraction = local - at;
        const frames = p.frames.slice(0, at + 1), frame = frames.at(-1);
        const start = behaviourPosition(frame, index);
        const end = behaviourPosition(p.frames[Math.min(at + 1, p.frames.length - 1)], index);
        const position = start.map((value, axis) => value + (end[axis] - value) * fraction);
        const trail = [...frames.map(f => behaviourPosition(f, index)), position];
        return {...p, index, color: colors[index % colors.length], frame, position, trail,
            waiting: time < delay(index), finished: local === p.frames.length - 1,
            events: frames.map(f => f.event).filter(Boolean)};
    });
}
