const { basename } = require('node:path');
const { setTimeout: delay } = require('node:timers/promises');

exports.format = offer => {
    if (!offer.candidateId) return `No joint appointment (${offer.outcome}).`;
    const time = new Intl.DateTimeFormat('en-GB', {
        timeZone: 'Europe/London', hour: '2-digit', minute: '2-digit'
    });
    const names = offer.appointments.map(x =>
        `${x.clientName ?? x.clientId}: ${x.serviceId} with ${x.technicianName ?? x.technician}`);
    return `${basename(__filename)}: ${time.format(new Date(offer.start))} London: ${names.join('; ')}`;
};

exports.formatAsync = async offer => {
    await delay(1);
    return exports.format(offer);
};
