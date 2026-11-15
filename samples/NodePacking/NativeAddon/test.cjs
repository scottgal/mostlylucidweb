const assert = require('node:assert/strict');
const { describeAppointment } = require('./index.cjs');
const names = new Map([['alice', 'Alice'], ['sarah', 'Sarah']]);
let callbacks = 0;
assert.equal(describeAppointment('alice', 'gels', 'sarah', id => {
    callbacks++;
    return names.get(id);
}), 'Alice: gels with Sarah');
assert.equal(callbacks, 2);
assert.throws(() => describeAppointment('alice', 'gels', 'sarah', () => { throw new Error('Name lookup failed'); }), /Name lookup failed/);
console.log('PASS: Native AOT npm addon called JavaScript for names.');
