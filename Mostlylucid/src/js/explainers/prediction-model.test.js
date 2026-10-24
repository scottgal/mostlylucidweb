import {describe,expect,it} from 'vitest';
import {predictionReplay,predictionSnapshot,predictionMax,timeOrigin,probabilities,trajectoryComparisonPair,comparisonSnapshot,
    objectives,utilityRange,formatDuration} from './prediction-model';
import operations from '../../../../samples/RelevancePredict/artifacts/seed-814/temporal-operations-replay.json';

describe('physical-time behavioural tracks',()=>{
    it('uses 24 separate timestamped visits and preserves the actual forecast mass',()=>{
        expect(predictionReplay.participants).toHaveLength(24);
        expect(new Set(predictionReplay.participants.map(p=>p.id)).size).toBe(24);
        for(const participant of predictionReplay.participants){
            expect(participant.frames.every(f=>Number.isFinite(Date.parse(f.at)))).toBe(true);
            for(const frame of participant.frames){
                expect(Object.values(probabilities(frame.prediction)).reduce((a,b)=>a+b,0)).toBeCloseTo(1,12);
                expect(frame.prediction.horizonSeconds).toBe(60);
                expect(frame.prediction.horizonEvents).toBe(0);
            }
        }
    });
    it('shows real differences at the same observed position and categorical state',()=>{
        const [a,b]=trajectoryComparisonPair();
        expect(a.frame.prediction.trajectory.position).toEqual(b.frame.prediction.trajectory.position);
        expect(a.frame.prediction.currentState).toBe(b.frame.prediction.currentState);
        expect(a.frame.prediction.trajectory.velocity[0]).toBeGreaterThan(0);
        expect(b.frame.prediction.trajectory.velocity[0]).toBeLessThan(0);
        expect(probabilities(a.frame.prediction).purchase).toBeCloseTo(.855,5);
        expect(probabilities(b.frame.prediction).purchase).toBeCloseTo(.22,5);
        const population=comparisonSnapshot(60);
        expect(population).toHaveLength(24);
        expect(population.slice(0,2)).toEqual([a,b]);
        expect(population.slice(2)).toEqual(predictionSnapshot(60).slice(2));
        expect(a.events).not.toContain('purchase');
        expect(b.events).not.toContain('leave');
    });
    it('distinguishes the same route at different paces and retains out-of-window mass',()=>{
        const [a,c]=trajectoryComparisonPair('pace');
        expect(a.frame.prediction.trajectory.position).toEqual(c.frame.prediction.trajectory.position);
        expect(a.frame.prediction.trajectory.elapsedSeconds).toBe(20);
        expect(c.frame.prediction.trajectory.elapsedSeconds).toBe(120);
        expect(a.frame.prediction.trajectory.velocity[0]).toBeGreaterThan(c.frame.prediction.trajectory.velocity[0]);
        expect(c.frame.prediction.remainingProbability).toBe(1);
        expect(c.frame.prediction.status).toBe('estimated_trajectory');
    });
    it('uses the shared UTC timeline and interpolates continuous movement without inventing events',()=>{
        expect(predictionSnapshot(3)[0].events).toEqual(['view']);
        // Waiting placeholders have no measured coordinates and must not become part of an observed trail.
        expect(predictionSnapshot(3)[0].trail).toHaveLength(2);
        expect(predictionSnapshot(3)[1].events).toEqual([]);
        const before=predictionSnapshot(45),after=predictionSnapshot(50);
        expect(before.filter((p,i)=>p.position.join(',')!==after[i].position.join(',')).length).toBeGreaterThan(10);
        predictionSnapshot(predictionMax);
        expect(predictionSnapshot(45)).toEqual(before);
        expect(formatDuration(120)).toBe('02:00');
        expect(predictionMax).toBeGreaterThan(300);
        expect(Number.isFinite(timeOrigin)).toBe(true);
    });
    it('keeps unfinished visits unfinished and marks expired recorded forecast windows',()=>{
        const finished=predictionSnapshot(predictionMax);
        expect(finished.filter(p=>p.frame.prediction.status!=='observed_destination')).toHaveLength(2);
        expect(predictionSnapshot(200)[2].forecastExpired).toBe(true);
        expect(predictionSnapshot(200)[0].forecastExpired).toBe(false);
    });
    it('retains a stable, learned normal reference despite incident trajectories',()=>{
        const normalFrames=operations.participants.flatMap(p=>p.frames).filter(f=>f.prediction.trajectory?.normal);
        const centroid=normalFrames[0].prediction.trajectory.normal.centroid;
        expect(normalFrames.every(f=>JSON.stringify(f.prediction.trajectory.normal.centroid)===JSON.stringify(centroid))).toBe(true);
        expect(normalFrames.some(f=>f.prediction.trajectory.normal.deviation>50)).toBe(true);
        expect(centroid[0]).toBeCloseTo(.1,3);
    });
    it('keeps desirability independent of geometry and forecast probabilities',()=>{
        const person=trajectoryComparisonPair()[0],before=JSON.stringify(person);
        expect(utilityRange(person.frame.prediction,objectives.purchase)[0]).toBeGreaterThan(utilityRange(person.frame.prediction,objectives.cancellation)[0]);
        expect(JSON.stringify(person)).toBe(before);
    });
});
