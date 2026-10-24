import {describe, expect, it} from 'vitest';
import {destinations, objectives, predictionReplay, predictionSnapshot, predictionMax, probabilities, forecastPosition, utilityRange} from './prediction-model';
import report from '../../../../samples/RelevancePredict/artifacts/seed-814/report.json';

describe('captured destination replay', () => {
    it('preserves probability mass on every real engine frame, including no-state and observed endpoints', () => {
        for (const p of predictionReplay.participants) {
            for (const frame of p.frames) {
                expect(Object.values(probabilities(frame.prediction)).reduce((a, b) => a + b, 0)).toBeCloseTo(1, 12);
                expect(frame.prediction.isCalibrated).toBe(false);
            }
            expect(p.frames[0].prediction.status).toBe('no_state');
            expect(p.frames[0].prediction.remainingProbability).toBe(1);
            expect(p.frames.at(-1).prediction.status).toBe('observed_destination');
            expect(p.frames.at(-1).prediction.outcomes.find(o => o.destination === p.frames.at(-1).event).probability).toBe(1);
        }
    });
    it('agrees with the article report for the three fresh cart states', () => {
        const frames = predictionReplay.participants.flatMap(p => p.frames);
        for (const forecast of report.forecasts) {
            const frame = frames.find(f => f.prediction.currentState === forecast.currentState);
            expect(frame.prediction.outcomes).toEqual(forecast.outcomes);
            expect(frame.prediction.remainingProbability).toBe(forecast.remainingProbability);
        }
    });
    it('replays only the observed prefix and keeps shorter visits at their endpoint', () => {
        expect(predictionSnapshot(3)[0].events).toEqual(['view', 'compare', 'cart']);
        expect(predictionSnapshot(3)[0].frame.prediction.status).toBe('estimated');
        expect(predictionSnapshot(predictionMax)[1].events).toEqual(['view', 'cart', 'view', 'leave']);
        const before = predictionSnapshot(2);
        predictionSnapshot(predictionMax);
        expect(predictionSnapshot(2)).toEqual(before);
        expect(predictionSnapshot(-2)).toEqual(predictionSnapshot(0));
    });
    it('maps unknown evidence to its own anchor and observed endpoints to their destination', () => {
        expect(forecastPosition(predictionReplay.participants[0].frames[0].prediction)).toEqual(destinations.find(d => d.id === 'unresolved').position);
        for (const p of predictionReplay.participants) {
            expect(forecastPosition(p.frames.at(-1).prediction)).toEqual(destinations.find(d => d.id === p.frames.at(-1).event).position);
        }
    });
    it('changes utility without changing probabilities, and bounds unresolved value explicitly', () => {
        const forecast = predictionSnapshot(3)[0].frame.prediction;
        const before = JSON.stringify(forecast);
        expect(utilityRange(forecast, objectives.purchase)[0]).toBeGreaterThan(utilityRange(forecast, objectives.cancellation)[0]);
        expect(JSON.stringify(forecast)).toBe(before);
        expect(utilityRange(predictionSnapshot(0)[0].frame.prediction, objectives.purchase)).toEqual([-.5, 1]);
        expect(utilityRange(predictionSnapshot(predictionMax)[2].frame.prediction, objectives.cancellation)).toEqual([-1, -1]);
    });
});
