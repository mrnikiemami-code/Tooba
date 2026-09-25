const fs = require('fs');
const path = 'docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1/RESULT.bridge.txt';
const content = fs.readFileSync(path, 'utf8');
const body = JSON.stringify({ channelId: 'tooba-main', taskId: 'TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1', content });

fetch('http://127.0.0.1:17321/api/results', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body,
})
  .then(async (r) => { console.log('STATUS', r.status); console.log(await r.text()); })
  .catch((e) => { console.error('ERR', e.message); process.exit(1); });
