const { isDeepStrictEqual } = require('node:util');

// Single authoritative store for this executable fixture. Replace these API methods with your
// existing repository and atomic transaction, not with another C# reservation database.
exports.createCalendarApi = catalog => {
    const grid = catalog.gridMinutes * 60_000;
    const first = Date.parse(catalog.start) / grid;
    const last = Date.parse(catalog.end) / grid;
    const known = new Set(catalog.resourceIds);
    const used = new Map();
    const commands = new Map();
    const names = new Map([['alice', 'Alice'], ['beth', 'Beth'], ['sarah', 'Sarah'], ['anna', 'Anna']]);
    let version = 0;
    let nameCalls = 0;
    const key = (resource, slot) => `${resource}/${slot}`;

    return {
        async readWindow(start, end) {
            // Real adapter: one consistent DB/API read including active bookings and holds.
            const from = Math.max(first, Math.floor(Date.parse(start) / grid));
            const to = Math.min(last, Math.ceil(Date.parse(end) / grid));
            const snapshot = { version, resourceIds: [], slots: [], capacity: [], used: [] };
            for (const resource of catalog.resourceIds) {
                for (let slot = from; slot < to; slot++) {
                    snapshot.resourceIds.push(resource);
                    snapshot.slots.push(slot);
                    snapshot.capacity.push(1);
                    snapshot.used.push(used.get(key(resource, slot)) ?? 0);
                }
            }
            return snapshot;
        },
        getName(id) {
            // Synchronous lookup over already-loaded application data; no DB wait inside C#.
            nameCalls++;
            return names.get(id) ?? id;
        },
        rename(id, name) { names.set(id, name); },
        getNameCallCount() { return nameCalls; },
        async tryBook(candidateId, expectedVersion, commandId, demands) {
            const request = structuredClone({ candidateId, expectedVersion, demands });
            if (commands.has(commandId)) {
                const prior = commands.get(commandId);
                if (!isDeepStrictEqual(prior.request, request)) throw new Error('Command ID reused with different booking.');
                return { ...prior.reply };
            }
            const remember = reply => {
                commands.set(commandId, { request, reply: { ...reply } });
                return reply;
            };
            if (expectedVersion !== version)
                return remember({ success: false, reason: 'SnapshotChanged', version });
            const changes = new Map();
            for (const demand of demands) {
                const start = Date.parse(demand.start) / grid;
                const end = Date.parse(demand.end) / grid;
                if (!known.has(demand.resourceId) || !Number.isInteger(start) || !Number.isInteger(end) ||
                    start < first || end > last || end <= start || !Number.isInteger(demand.units) || demand.units <= 0)
                    throw new Error('Invalid resource demand.');
                for (let slot = start; slot < end; slot++) {
                    const cell = key(demand.resourceId, slot);
                    changes.set(cell, (changes.get(cell) ?? 0) + demand.units);
                }
            }
            if (!changes.size) throw new Error('A booking needs resource demands.');
            for (const [cell, units] of changes)
                if ((used.get(cell) ?? 0) + units > 1)
                    return remember({ success: false, reason: 'CapacityChanged', version });

            // No await between check and write in this single-process fixture.
            // A durable multi-process implementation needs an atomic database transaction here.
            for (const [cell, units] of changes) used.set(cell, (used.get(cell) ?? 0) + units);
            version++;
            return remember({ success: true, reason: 'Booked', version, reservationId: commandId });
        }
    };
};
