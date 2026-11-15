export function toCalendarEvents(offer) {
    return (offer.appointments ?? []).map(appointment => ({
        id: `${offer.candidateId}/${appointment.clientId}`,
        title: `${appointment.clientName ?? appointment.clientId}: ${appointment.serviceId} (${appointment.technicianName ?? appointment.technician})`,
        start: appointment.start,
        end: appointment.end,
        editable: false,
        extendedProps: { candidateId: offer.candidateId, state: 'suggested' }
    }));
}
