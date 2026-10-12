// Posts the complete TB-P0-PACKAGE-DISPATCH-W6-RECONCILIATION-005 worker result to Bridge /api/results.
// Only the real Result contract is posted; no probe/dummy payload.
const fs = require('fs');
const path = require('path');

const bridge = 'http://127.0.0.1:17321';
const resultPath = path.join(__dirname, 'RESULT.bridge.txt');

async function main() {
  const content = fs.readFileSync(resultPath, 'utf8');
  const body = {
    channelId: 'tooba-main',
    workerId: 'tooba-worker-01',
    agentType: 'cursor',
    taskId: 'TB-P0-PACKAGE-DISPATCH-W6-RECONCILIATION-005',
    bridgeTaskId: '406edba1-a002-4c4a-8b58-808dfb795c55',
    status: 'Completed',
    content,
  };

  const res = await fetch(`${bridge}/api/results`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  console.log(res.status, await res.text());
}

main().catch((err) => {
  console.error('POST /api/results failed:', err);
  process.exit(1);
});
