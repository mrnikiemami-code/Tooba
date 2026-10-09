const fs = require('fs');
const path = require('path');

const repo = process.argv[2] || '.';
const statePath = path.join(repo, 'docs/architecture/tmar-current-state.json');
const sot = JSON.parse(fs.readFileSync(statePath, 'utf8'));

const w3 = sot.taxAmsc001W3;
if (!w3) {
  throw new Error('taxAmsc001W3 missing');
}

// No self-referential placeholder may live in the SoT (Story R1 guard). The cert commit is reported
// through the Bridge Result only and reconciled by the Architect, exactly as the Support/Story waves did.
delete w3.commit;
delete w3.commitFull;
w3.commitState = 'REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER';

fs.writeFileSync(statePath, JSON.stringify(sot, null, 2) + '\n');
console.log('taxAmsc001W3.commit removed; commitState =', w3.commitState);
