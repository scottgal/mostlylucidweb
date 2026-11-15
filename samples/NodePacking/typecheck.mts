import { createSalon } from './salon.mjs';
import { createEngine, createSchedulingFlow, type CalendarApi } from './auto-scheduling.mjs';

const salon = createSalon(':memory:');
const offer = salon.findTogether('2030-06-03T10:00:00Z', '2030-06-03T12:00:00Z');
const complete: boolean = offer.candidatesComplete;
const technician: string = offer.appointments[0].technician;
const candidate: string | undefined = offer.candidateId;
void [complete, technician, candidate];

// These must remain type errors as the C# contract evolves.
// @ts-expect-error timestamp parameters are strings
salon.findTogether(42, '2030-06-03T12:00:00Z');
// @ts-expect-error result fields are checked through the generated declarations
const invalid: number = offer.appointments[0].technician;
// @ts-expect-error there is no such result property
offer.apointment;
void invalid;

const api: CalendarApi = {
    readWindow: async () => ({ version: 0, resourceIds: [], slots: [], capacity: [], used: [] }),
    getName: id => id,
    tryBook: async () => ({ success: false, reason: 'SnapshotChanged', version: 1 })
};
const flow = createSchedulingFlow(api, createEngine());
const suggestion = await flow.suggest('2030-06-03T10:00:00Z', '2030-06-03T12:00:00Z');
const clientName: string = suggestion.appointments[0].clientName;
void clientName;
// @ts-expect-error authoritative demands are not exposed by the UI-facing result
suggestion.demands;
// @ts-expect-error name callbacks are synchronous over preloaded data
api.getName = async id => id;
