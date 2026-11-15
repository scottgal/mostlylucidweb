import { randomUUID } from 'node:crypto';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { createSalon } from './salon.mjs';
import formatter from './format-booking.cjs';

const directory = mkdtempSync(join(tmpdir(), 'node-packing-'));
const path = join(directory, 'salon.db');
const first = createSalon(path);
const second = createSalon(path);

try {
    const from = '2030-06-03T10:00:00Z';
    const to = '2030-06-03T12:00:00Z';
    const offer = first.findTogether(from, to);
    const competingOffer = second.findTogether(from, to);
    console.log(formatter.format(offer));
    if (!offer.candidateId || !competingOffer.candidateId) throw new Error('Expected two offers.');

    const commandId = randomUUID();
    const hold = first.tryHold(offer.candidateId, commandId);
    if (!hold.success) throw new Error(hold.reason);
    console.log('First hold:', hold.success);
    console.log('Same command returns same reservation:',
        first.tryHold(offer.candidateId, commandId).reservationId === hold.reservationId);
    console.log('Competing stale offer accepted:',
        second.tryHold(competingOffer.candidateId, randomUUID()).success);
    console.log('Sarah free during drying:', first.canSarahConsult('2030-06-03T10:15:00Z'));
    console.log('Sarah free during finishing:', first.canSarahConsult('2030-06-03T10:35:00Z'));
    console.log('Confirmed:', first.confirm(hold.reservationId, randomUUID()).success);
} finally {
    second.dispose();
    first.dispose();
    rmSync(directory, { recursive: true, force: true });
}
