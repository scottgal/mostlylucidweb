import shopping from '../../../../samples/RelevancePredict/artifacts/seed-814/temporal-shop-replay.json';
import operations from '../../../../samples/RelevancePredict/artifacts/seed-814/temporal-operations-replay.json';

export const predictionReplay = new URLSearchParams(globalThis.location?.search ?? '').get('domain') === 'operations' ? operations : shopping;
export const isOperations = predictionReplay.domain === 'operations';
const frames = predictionReplay.participants.flatMap(p => p.frames);
export const timeOrigin = Math.min(...frames.map(f => Date.parse(f.at)));
export const predictionMax = Math.ceil((Math.max(...frames.map(f => Date.parse(f.at))) - timeOrigin) / 1000);
const colors = [[72,113,200],[177,101,176],[190,143,45],[40,158,155],[205,103,77],[115,126,211],[67,154,110],[164,117,71]];

// A fixed view of the two converter coordinates. Derivatives and forecasts use source values, never screen distances.
export function projectCoordinates(values) {
    return [70 + values[0] / (isOperations ? 1.3 : 1) * 850, 110 + (1 - values[1] / (isOperations ? .14 : 1)) * 650];
}
export const destinations = (isOperations ? [
    {id:'recovered',name:'Recovered',coordinates:[.1,.01],color:[49,133,106]},
    {id:'outage',name:'Outage',coordinates:[1.2,.12],color:[185,88,84]}
] : [
    {id:'purchase',name:'Purchase',coordinates:[1,0],color:[49,133,106]},
    {id:'cancel',name:'Cancel',coordinates:[1,.5],color:[185,88,84]},
    {id:'leave',name:'Leave',coordinates:[1,1],color:[190,126,66]}
]).map(d=>({...d,position:projectCoordinates(d.coordinates)}));
export const outcomeLabels = [...destinations,{id:'unresolved',name:'Unresolved',color:[125,136,147]}];
export const objectives = isOperations ? {
    recovery:{name:'Recover within 60 seconds',utilities:{recovered:1,outage:-1}},
    outage:{name:'Avoid an outage',utilities:{recovered:0,outage:-1}}
} : {
    purchase:{name:'Complete purchases',utilities:{purchase:1,cancel:-.5,leave:0}},
    cancellation:{name:'Avoid cancellation',utilities:{purchase:0,cancel:-1,leave:0}}
};
export const normalReference = frames.find(f=>f.prediction.trajectory?.normal?.deviation != null)?.prediction.trajectory.normal;
export function formatDuration(seconds) {
    const value=Math.max(0,Math.floor(seconds));
    return `${String(Math.floor(value/60)).padStart(2,'0')}:${String(value%60).padStart(2,'0')}`;
}
export const formatClock = at => new Date(at).toISOString().slice(11,19)+' UTC';
export function probabilities(prediction) {
    return {...Object.fromEntries(prediction.outcomes.map(o=>[o.destination,o.probability])),unresolved:prediction.remainingProbability};
}
export function behaviourPosition(frame,index,spread=true,count=predictionReplay.participants.length) {
    const motion=frame.prediction.trajectory;
    if(!motion)return [45,95+index/Math.max(1,count-1)*700];
    const point=projectCoordinates(motion.centroid);
    // Optional fixed display lanes separate coincident trajectories. Never use these offsets for inference.
    return [point[0],point[1]+(spread?((index*7)%count-(count-1)/2)*6:0)];
}
export function utilityRange(prediction,objective) {
    const values=objective.utilities;
    const known=prediction.outcomes.reduce((sum,o)=>sum+o.probability*values[o.destination],0);
    return [known+prediction.remainingProbability*Math.min(...Object.values(values)),known+prediction.remainingProbability*Math.max(...Object.values(values))];
}
function participantSnapshot(p,index,at,fraction,now,spread=true) {
    const prefix=p.frames.slice(0,at+1),frame=prefix.at(-1);
    const start=behaviourPosition(frame,index,spread),end=behaviourPosition(p.frames[Math.min(at+1,p.frames.length-1)],index,spread);
    const position=start.map((v,axis)=>v+(end[axis]-v)*fraction);
    return {...p,index,color:colors[index%colors.length],frame,position,
        trail:[...prefix.filter(f=>f.prediction.trajectory).map(f=>behaviourPosition(f,index,spread)),position],events:prefix.map(f=>f.event).filter(Boolean),
        waiting:at===0&&fraction===0,finished:at===p.frames.length-1,
        forecastExpired:frame.prediction.status!=='observed_destination'&&frame.prediction.observedAt!=null&&now>Date.parse(frame.prediction.observedAt)+predictionReplay.horizonSeconds*1000};
}
/** All visits share their actual UTC event timeline. Playback seconds are independent of event count. */
export function predictionSnapshot(cursor,spread=true) {
    const now=timeOrigin+Math.max(0,Math.min(predictionMax,cursor))*1000;
    return predictionReplay.participants.map((p,index)=>{
        let at=0;
        while(at+1<p.frames.length&&Date.parse(p.frames[at+1].at)<=now)at++;
        const start=Date.parse(p.frames[at].at),next=Date.parse(p.frames[Math.min(at+1,p.frames.length-1)].at);
        const fraction=next>start?Math.max(0,Math.min(1,(now-start)/(next-start))):0;
        return participantSnapshot(p,index,at,fraction,now,spread);
    });
}
/** Pin visits at the same measured position, retaining their true directions and elapsed durations. */
export function trajectoryComparisonPair(mode='direction',spread=true) {
    return [0,mode==='pace'?2:1].map(index=>{
        const p=predictionReplay.participants[index],at=p.comparisonFrame;
        return participantSnapshot(p,index,at,0,Date.parse(p.frames[at].at),spread);
    });
}
export function comparisonSnapshot(cursor,mode='direction',spread=true) {
    const pair=new Map(trajectoryComparisonPair(mode,spread).map(p=>[p.id,p]));
    return predictionSnapshot(cursor,spread).map(p=>pair.get(p.id)??p);
}
