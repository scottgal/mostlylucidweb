import {describe, it, expect} from 'vitest';
import {replay, events, rank, distance, proxySteps} from './model';

describe('deterministic explainer mathematics', () => {
    it('starts without a summary, then uses the first observation as its mean', () => {
        expect(replay(0).mean).toBeNull();
        expect(replay(1).mean).toEqual(events[0].values);
        expect(replay(1).mass).toBe(1);
    });
    it('decays old mass before adding weighted evidence', () => {
        const result = replay(2, {halfLife: 8});
        expect(result.mass).toBe(2.5);
        result.mean.forEach((v, i) => expect(v).toBeCloseTo((.5 * events[0].values[i] + 2 * events[1].values[i]) / 2.5));
    });
    it('idle decay weakens evidence without moving its mean', () => {
        const active = replay(3);
        const idle = replay(3, {idle: 30});
        expect(idle.mean).toEqual(active.mean);
        expect(idle.mass).toBeCloseTo(active.mass / 2);
        expect(rank(replay(3, {idle: 600}))).toEqual([]);
    });
    it('weights change the summary and scrubbing is reproducible', () => {
        expect(distance(replay(3, {cartWeight: 6}).mean, events[2].values))
            .toBeLessThan(distance(replay(3, {cartWeight: 1}).mean, events[2].values));
        const before = replay(2);
        replay(5);
        expect(replay(2)).toEqual(before);
    });
    it('filters before truncation, using all coordinates', () => {
        const points = [
            {id: 'blocked', values: [0,0,0], available: false, price: 1, group: 'home'},
            {id: 'ok', values: [0,0,1], available: true, price: 100, group: 'home'},
            {id: 'over-budget', values: [0,0,.1], available: true, price: 999, group: 'home'}
        ];
        const found = rank({mean: [0,0,0], mass: 1}, {budget: 120}, 1, points);
        expect(found[0].id).toBe('ok');
        expect(found[0].distance).toBe(1);
    });
    it('only the allowed proxy path reaches the origin', () => {
        expect(proxySteps('browser').findIndex(s => s.origin)).toBe(5);
        expect(proxySteps('unfamiliar').some(s => s.origin)).toBe(false);
        expect(proxySteps('burst').some(s => s.node === 'Origin')).toBe(false);
    });
});
