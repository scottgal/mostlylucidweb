import assert from 'node:assert/strict';
import { test } from 'node:test';
import { randomUUID } from 'node:crypto';
import { createEngine, catalogFor, createSchedulingFlow } from './auto-scheduling.mjs';
import appApi from './existing-calendar-api.cjs';
import { toCalendarEvents } from './calendar-events.mjs';

const start = '2030-06-03T10:00:00Z';
const end = '2030-06-03T12:00:00Z';

test('one JavaScript authority supplies snapshots and names, then commits the C# proposal', async () => {
    const engine = createEngine();
    const api = appApi.createCalendarApi(catalogFor(engine));
    api.rename('sarah', 'Sarah Jones');
    const first = createSchedulingFlow(api, engine);
    const second = createSchedulingFlow(api, engine);
    const offer = await first.suggest(start, end);
    const competing = await second.suggest(start, end);
    assert.equal(offer.appointments[0].clientName, 'Alice');
    assert.equal(offer.appointments[0].technicianName, 'Sarah Jones');
    assert.match(toCalendarEvents(offer)[0].title, /Alice: gels \(Sarah Jones\)/);
    assert.equal(api.getNameCallCount(), 8);
    assert.equal(offer.version, 0);
    assert.ok(!('demands' in offer), 'The UI does not receive authoritative resource demands.');

    // Editing the UI projection does not change the server-retained proposal.
    offer.appointments[0].technician = 'anna';
    const command = randomUUID();
    const booked = await first.accept(offer.candidateId, command);
    assert.equal(booked.success, true);
    assert.deepEqual(await first.accept(offer.candidateId, command), booked);
    assert.equal((await second.accept(competing.candidateId, randomUUID())).reason, 'SnapshotChanged');

    const snapshot = await api.readWindow(start, end);
    assert.equal(snapshot.version, 1);
    assert.ok(snapshot.used.some(units => units > 0));
    const next = await first.suggest(start, end);
    assert.equal(next.version, 1);
    assert.ok(Date.parse(next.start) > Date.parse(offer.start));
    await assert.rejects(api.tryBook('edited-candidate', 0, command, []), /Command ID reused/);
});

test('callbacks must be synchronous over preloaded names, and errors cross the boundary', async () => {
    const engine = createEngine();
    const api = appApi.createCalendarApi(catalogFor(engine));
    const snapshot = await api.readWindow(start, end);
    const window = { start, end };
    assert.throws(() => engine.suggest(window, snapshot, async id => id), Error);
    assert.throws(() => engine.suggest(window, snapshot, () => { throw new Error('Name lookup failed'); }), /Name lookup failed/);
    assert.throws(() => engine.suggest(window, { ...snapshot, used: [] }, id => id), /equal lengths/);
    const valid = engine.suggest(window, snapshot, id => api.getName(id));
    assert.equal(valid.appointments[0].clientName, 'Alice');
    assert.equal((await api.readWindow(start, end)).version, 0, 'Search and failed callbacks never write bookings.');
});
