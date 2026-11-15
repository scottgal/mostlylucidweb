// Read each managed property once into ordinary JS data, with no stringify/parse step.
export function snapshotOffer(value) {
    return {
        outcome: value.outcome,
        version: value.version,
        candidatesComplete: value.candidatesComplete,
        candidateId: value.candidateId,
        start: value.start,
        appointments: Array.from(value.appointments, appointment => ({
            clientId: appointment.clientId,
            serviceId: appointment.serviceId,
            start: appointment.start,
            end: appointment.end,
            technician: appointment.technician
        }))
    };
}
