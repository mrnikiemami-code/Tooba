const fs = require('fs');
const path = 'docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196/worker-result.md';
const content = fs.readFileSync(path, 'utf8');
const payload = {
  channelId: 'tooba-main',
  taskRowId: 'f2efd856-a9b1-4f7a-b8ae-3a9a17759379',
  taskId: 'TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196',
  workerId: 'tooba-worker-01',
  agentType: 'cursor',
  status: 'PARTIAL',
  content
};
fetch('http://127.0.0.1:17321/api/results', {
  method: 'POST',
  headers: { 'content-type': 'application/json' },
  body: JSON.stringify(payload)
}).then(async r => {
  const text = await r.text();
  console.log(r.status, text.slice(0, 400));
}).catch(e => { console.error('ERR', e.message); process.exit(1); });
