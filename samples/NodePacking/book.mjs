import { randomUUID } from 'node:crypto';
import { createSalon } from './salon.mjs';

const salon = createSalon(':memory:');
try {
    const offer = salon.findTogether('2030-06-03T10:00:00Z', '2030-06-03T12:00:00Z');
    if (!offer.candidateId) throw new Error(`No joint appointment: ${offer.outcome}`);

    for (const appointment of offer.appointments) {
        console.log(`${appointment.clientId}: ${appointment.serviceId} with ${appointment.technician}`);
    }

    const hold = salon.tryHold(offer.candidateId, randomUUID());
    if (!hold.success) throw new Error(hold.reason);

    const confirmation = salon.confirm(hold.reservationId, randomUUID());
    if (!confirmation.success) throw new Error(confirmation.reason);
    console.log('Both appointments confirmed.');
} finally {
    salon.dispose();
}
