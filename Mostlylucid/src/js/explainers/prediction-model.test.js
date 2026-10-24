import {describe, expect, it} from 'vitest';
import {objectives, predictionReplay, predictionSnapshot, predictionMax, probabilities, behaviourPosition, utilityRange} from './prediction-model';
import report from '../../../../samples/RelevancePredict/artifacts/seed-814/report.json';

describe('multiple behavioural tracks', () => {
    it('uses 24 distinct visits with different paths, not clones of three users', () => {
        expect(predictionReplay.participants).toHaveLength(24);
        expect(new Set(predictionReplay.participants.map(p=>p.id)).size).toBe(24);
        expect(new Set(predictionReplay.participants.map(p=>p.frames.map(f=>f.event).join(','))).size).toBeGreaterThan(5);
        expect(new Set(predictionSnapshot(2).map(p=>p.position.join(','))).size).toBe(24);
    });
    it('preserves real forecast mass and agrees with the article cart estimates', () => {
        const frames=predictionReplay.participants.flatMap(p=>p.frames);
        for(const f of frames)expect(Object.values(probabilities(f.prediction)).reduce((a,b)=>a+b,0)).toBeCloseTo(1,12);
        for(const forecast of report.forecasts){
            const f=frames.find(f=>f.prediction.currentState===forecast.currentState);
            expect(f.prediction.outcomes).toEqual(forecast.outcomes);
        }
    });
    it('moves continuously, keeps individual trails, and rewinds deterministically', () => {
        const a=predictionSnapshot(2.1),b=predictionSnapshot(2.6);
        expect(a.filter((p,i)=>p.position.join(',')!==b[i].position.join(',')).length).toBeGreaterThan(10);
        expect(b.every(p=>p.trail.length>=2)).toBe(true);
        const before=predictionSnapshot(2.6);predictionSnapshot(predictionMax);
        expect(predictionSnapshot(2.6)).toEqual(before);
    });
    it('shows backwards movement for basket removal without consulting an endpoint', () => {
        const person=predictionReplay.participants[2];
        const cart=person.frames.find(f=>f.prediction.currentState==='research_cart');
        const removed=person.frames.find(f=>f.event==='remove');
        expect(behaviourPosition(removed,2)[0]).toBeLessThan(behaviourPosition(cart,2)[0]);
        const before=predictionSnapshot(3);
        const last=person.frames.at(-1);
        const original=last.prediction.currentState;last.prediction.currentState='purchase';
        expect(predictionSnapshot(3)).toEqual(before);
        last.prediction.currentState=original;
    });
    it('keeps censored visits unresolved and does not assign an invented destination', () => {
        const completed=predictionSnapshot(predictionMax);
        const censored=completed.filter(p=>p.frame.prediction.status!=='observed_destination');
        expect(censored.length).toBeGreaterThan(0);
        expect(completed.every(p=>p.finished)).toBe(true);
        expect(censored.every(p=>!['purchase','cancel','leave'].includes(p.frame.event))).toBe(true);
    });
    it('changes utility without moving a behavioural track or changing its forecast', () => {
        const snapshot=predictionSnapshot(3),before=JSON.stringify(snapshot);
        const forecast=snapshot[0].frame.prediction;
        expect(utilityRange(forecast,objectives.purchase)[0]).toBeGreaterThan(utilityRange(forecast,objectives.cancellation)[0]);
        expect(JSON.stringify(snapshot)).toBe(before);
        expect(utilityRange(predictionSnapshot(0)[0].frame.prediction,objectives.purchase)).toEqual([-.5,1]);
    });
});
