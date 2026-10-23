// Literal teaching coordinates, not model embeddings or discovered communities.
// All calculations use the same three dimensions; the 2D view only displays two of them.
export const groups = [
    { id: 'outdoors', name: 'Outdoors', center: [.82, .16, .18], color: [49, 133, 106] },
    { id: 'electronics', name: 'Electronics', center: [.18, .82, .17], color: [99, 118, 191] },
    { id: 'home', name: 'Home', center: [.22, .23, .83], color: [190, 126, 66] }
];
let seed = 417;
function random() { seed = (1664525 * seed + 1013904223) >>> 0; return seed / 4294967296; }
export const candidates = groups.flatMap(group => Array.from({length: 48}, (_, i) => ({
    id: `${group.id}-${i + 1}`, name: `${group.name} example ${i + 1}`, group: group.id,
    values: group.center.map(v => Math.max(.02, Math.min(.98, v + (random() - .5) * .26))),
    price: 25 + Math.round(random() * 220), available: i % 8 !== 0, color: group.color
})));
export const events = [
    { minute: 0, type: 'view', group: 'outdoors', text: 'Open an outdoors product' },
    { minute: 8, type: 'compare', group: 'outdoors', text: 'Compare another outdoors product' },
    { minute: 16, type: 'cart', group: 'electronics', text: 'Add an electronics product to the basket' },
    { minute: 33, type: 'view', group: 'home', text: 'Open a home product' },
    { minute: 52, type: 'cart', group: 'home', text: 'Add a home product to the basket' }
].map((event, i) => ({...event, values: candidates.filter(p => p.group === event.group)[i + 3].values}));

/** Replay from source events, so scrubbing/reset never accumulate floating-point state or duplicate observations. */
export function replay(cursor, {halfLife = 30, cartWeight = 3, idle = 0} = {}) {
    let sum = [0, 0, 0], mass = 0, at = 0;
    const trail = [];
    for (const event of events.slice(0, cursor)) {
        const decay = Math.pow(.5, (event.minute - at) / halfLife);
        const weight = event.type === 'cart' ? cartWeight : event.type === 'compare' ? 2 : 1;
        sum = sum.map((v, i) => decay * v + weight * event.values[i]);
        mass = decay * mass + weight;
        at = event.minute;
        trail.push(sum.map(v => v / mass));
    }
    const mean = mass > 0 ? sum.map(v => v / mass) : null;
    mass *= Math.pow(.5, idle / halfLife);
    return {mean, mass, sum: sum.map(v => v * Math.pow(.5, idle / halfLife)), trail, at: at + idle};
}
export const distance = (a, b) => Math.sqrt(a.reduce((total, v, i) => total + (v - b[i]) ** 2, 0));
export const eligible = (p, {budget = 250, category = 'all', available = true} = {}) =>
    p.price <= budget && (category === 'all' || p.group === category) && (!available || p.available);
/** Eligibility precedes top-K. No screen-space or projected distances enter this ranking. */
export function rank(state, filters = {}, count = 4, points = candidates) {
    if (!state.mean || state.mass < .1) return [];
    return points.filter(p => eligible(p, filters)).map(p => ({...p, distance: distance(state.mean, p.values)}))
        .sort((a, b) => a.distance - b.distance || a.id.localeCompare(b.id)).slice(0, count);
}

export const proxyScenarios = {
    browser: {name: 'Ordinary browser', action: 'allow', evidence: [['Request shape', 'Familiar'], ['Rate', 'Low'], ['Session continuity', 'Consistent']], why: 'The supplied evidence fits this scenario’s allow rule.'},
    crawler: {name: 'Known crawler', action: 'allow', evidence: [['Claimed identity', 'Crawler'], ['Verification', 'Passed'], ['Rate', 'Within policy']], why: 'The example includes verified crawler evidence; the name alone is not trusted.'},
    unfamiliar: {name: 'Unfamiliar client', action: 'challenge', evidence: [['Request shape', 'Unfamiliar'], ['History', 'Insufficient'], ['Rate', 'Low']], why: 'The configured teaching policy asks for more evidence before forwarding.'},
    burst: {name: 'Scripted burst', action: 'challenge', evidence: [['Rate', 'Elevated'], ['Transitions', 'Repetitive'], ['Continuity', 'Weak']], why: 'The configured teaching policy challenges this combination of observations.'}
};
export function proxySteps(key) {
    const scenario = proxyScenarios[key];
    const allowed = scenario.action === 'allow';
    return [
        {node: 'Client', title: 'A request leaves the client', text: 'A normal HTTP request. The application needs no Stylo.Bot SDK.', origin: false},
        {node: 'Edge', title: 'The TLS edge receives it', text: 'This topology terminates TLS at the edge before the gateway. Encrypted traffic does not become readable by magic.', origin: false},
        {node: 'Observe', title: 'The gateway observes the request', text: 'Headers, timing and session behaviour supply observations. Missing observations stay missing.', origin: false},
        {node: 'Evidence', title: 'Observations become evidence', text: 'The displayed values belong to this labelled synthetic scenario, not live traffic or detector benchmarks.', origin: false},
        {node: 'Policy', title: `Policy selects ${scenario.action}`, text: scenario.why, origin: false},
        allowed
            ? {node: 'Origin', title: 'The origin receives the allowed request', text: 'The gateway forwards the request to the application. This is the first point at which the origin is reached.', origin: true}
            : {node: 'Challenge', title: 'The gateway handles the challenge', text: 'This request has not reached the origin. A later retry after successful verification is a new request and is outside this trace.', origin: false},
        {node: 'Reply', title: 'A response returns to the client', text: allowed ? 'The origin response returns through the gateway and edge.' : 'The challenge response comes from the gateway, with no origin response.', origin: allowed},
        {node: 'Memory', title: 'Telemetry continues asynchronously', text: 'A separate lane records bounded telemetry. This teaching trace does not claim that every background update blocks the response or feeds a trusted baseline.', origin: allowed}
    ];
}
