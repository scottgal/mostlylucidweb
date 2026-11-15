import { fileURLToPath } from 'node:url';
import dotnet from 'node-api-dotnet/net10.0';

const { AutoScheduler } = dotnet.require(fileURLToPath(new URL(
    './AppScheduler/bin/Release/net10.0/AppScheduler.dll', import.meta.url
)));

export function createEngine() { return new AutoScheduler(); }

export function catalogFor(engine) {
    return { start: engine.horizonStart, end: engine.horizonEnd,
        gridMinutes: engine.gridMinutes, resourceIds: Array.from(engine.resourceIds) };
}

export function createSchedulingFlow(api, engine = createEngine()) {
    const offers = new Map();
    return {
        async suggest(start, end) {
            const snapshot = await api.readWindow(start, end);
            const native = engine.suggest({ start, end }, snapshot, id => api.getName(id));
            const offer = {
                outcome: native.outcome, version: native.version,
                candidatesComplete: native.candidatesComplete, candidateId: native.candidateId,
                start: native.start,
                appointments: Array.from(native.appointments, x => ({
                    clientId: x.clientId, clientName: x.clientName, serviceId: x.serviceId,
                    technician: x.technician, technicianName: x.technicianName, start: x.start, end: x.end
                }))
            };
            offers.clear();
            if (offer.candidateId) offers.set(offer.candidateId, {
                ...structuredClone(offer),
                demands: Array.from(native.demands, x => ({
                    resourceId: x.resourceId, start: x.start, end: x.end, units: x.units
                }))
            });
            return offer;
        },
        async accept(candidateId, commandId) {
            const trusted = offers.get(candidateId);
            if (!trusted) throw new Error('Search for an offer in this booking flow first.');
            return api.tryBook(candidateId, trusted.version, commandId, trusted.demands);
        }
    };
}
