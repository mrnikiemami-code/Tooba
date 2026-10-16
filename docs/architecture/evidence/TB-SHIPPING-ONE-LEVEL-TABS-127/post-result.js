const fs = require('fs');
const path = require('path');
const dir = __dirname;
const content = fs.readFileSync(path.join(dir, 'RESULT.bridge.txt'), 'utf8');
const body = {
  channelId: 'tooba-main',
  workerId: 'tooba-worker-01',
  agentType: 'cursor',
  taskId: 'TB-SHIPPING-ONE-LEVEL-TABS-127',
  taskRowId: 'faa27b3b-5ecf-4577-a279-33dc2116bc67',
  status: 'PASS',
  content,
};
(async () => {
  const res = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  const text = await res.text();
  console.log(res.status, text.slice(0, 600));
})();
