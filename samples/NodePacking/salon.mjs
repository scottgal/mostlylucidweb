import { fileURLToPath } from 'node:url';
import dotnet from 'node-api-dotnet/net10.0';
import { snapshotOffer } from './offer-snapshot.mjs';

const assembly = fileURLToPath(new URL(
    './PackingBridge/bin/Release/net10.0/PackingBridge.dll', import.meta.url
));
const { Salon } = dotnet.require(assembly);

export function createSalon(databasePath) {
    const session = new Salon(databasePath);
    return {
        findTogether: (from, to) => snapshotOffer(session.findTogether({ start: from, end: to })),
        tryHold: (candidateId, commandId) => session.tryHold(candidateId, commandId),
        confirm: (reservationId, commandId) => session.confirm(reservationId, commandId),
        canSarahConsult: at => session.canSarahConsult(at),
        dispose: () => session.dispose()
    };
}
