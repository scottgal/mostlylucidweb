import { randomUUID } from 'node:crypto';
import { createEngine, catalogFor, createSchedulingFlow } from './auto-scheduling.mjs';
import appApi from './existing-calendar-api.cjs';

const engine = createEngine();
const api = appApi.createCalendarApi(catalogFor(engine));
const first = createSchedulingFlow(api, engine);
const second = createSchedulingFlow(api, engine);
const offer = await first.suggest('2030-06-03T10:00:00Z', '2030-06-03T12:00:00Z');
const stale = await second.suggest('2030-06-03T10:00:00Z', '2030-06-03T12:00:00Z');
if (!offer.candidateId || !stale.candidateId) throw new Error('Expected offers.');
for (const item of offer.appointments)
    console.log(`${item.clientName}: ${item.serviceId} with ${item.technicianName}`);
console.log('C# called JavaScript for names:', api.getNameCallCount() > 0);
const command = randomUUID();
console.log('Existing API booked:', (await first.accept(offer.candidateId, command)).success);
console.log('Retry returns same reservation:',
    (await first.accept(offer.candidateId, command)).reservationId === command);
console.log('Existing API accepted stale offer:', (await second.accept(stale.candidateId, randomUUID())).success);
