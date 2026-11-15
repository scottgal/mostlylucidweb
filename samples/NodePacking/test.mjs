import assert from 'node:assert/strict';
import { test } from 'node:test';
import { randomUUID } from 'node:crypto';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { createSalon } from './salon.mjs';
import formatter from './format-booking.cjs';
import { toCalendarEvents } from './calendar-events.mjs';

test('a linked hold excludes stale offers and preserves stage-specific availability', () => {
    const directory = mkdtempSync(join(tmpdir(), 'node-packing-test-'));
    const path = join(directory, 'salon.db');
    const first = createSalon(path);
    const second = createSalon(path);
    try {
        const from = '2030-06-03T10:00:00Z';
        const to = '2030-06-03T12:00:00Z';
        const offer = first.findTogether(from, to);
        const competing = second.findTogether(from, to);
        assert.equal(offer.outcome, 'Found');
        assert.equal(offer.candidatesComplete, true);
        assert.equal(offer.appointments.length, 2);
        assert.equal(offer.appointments[0].start, offer.appointments[1].start);
        const events = toCalendarEvents(offer);
        assert.equal(events.length, 2);
        assert.equal(events[0].extendedProps.candidateId, offer.candidateId);
        assert.equal(events[0].editable, false);
        assert.equal(events[0].start, events[1].start);
        assert.deepEqual(toCalendarEvents({ appointments: null }), []);
        assert.match(formatter.format(offer), /11:00 London/);
        assert.equal(typeof offer, 'object');
        assert.equal(typeof offer.version, 'number');
        assert.ok(Array.isArray(offer.appointments));
        assert.equal(typeof offer.appointments[0].technician, 'string');
        // A web endpoint must be able to serialize the complete snapshot, not an empty proxy.
        assert.deepEqual(JSON.parse(JSON.stringify(offer)), offer);
        assert.equal(first.canSarahConsult('2030-06-03T10:35:00Z'), true);
        assert.throws(() => first.findTogether('invalid-date', to), Error);

        const command = randomUUID();
        const hold = first.tryHold(offer.candidateId, command);
        assert.equal(hold.success, true);
        assert.deepEqual(first.tryHold(offer.candidateId, command), hold);
        assert.equal(second.tryHold(competing.candidateId, randomUUID()).success, false);
        assert.equal(first.canSarahConsult('2030-06-03T10:15:00Z'), true);
        assert.equal(first.canSarahConsult('2030-06-03T10:35:00Z'), false);
        assert.equal(first.confirm(hold.reservationId, randomUUID()).success, true);
        assert.equal(second.canSarahConsult('2030-06-03T10:35:00Z'), false);
        assert.equal(first.findTogether('2030-06-03T16:10:00Z', '2030-06-03T17:00:00Z').candidateId, undefined);
    } finally {
        second.dispose();
        first.dispose();
        rmSync(directory, { recursive: true, force: true });
    }
});

test('the shared JavaScript module returns and rejects Promises', async () => {
    const salon = createSalon(':memory:');
    try {
        const offer = salon.findTogether('2030-06-03T10:00:00Z', '2030-06-03T12:00:00Z');
        assert.equal(await formatter.formatAsync(offer), formatter.format(offer));
        await assert.rejects(formatter.formatAsync(null), TypeError);
    } finally {
        salon.dispose();
    }
});
