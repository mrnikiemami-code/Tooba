// Posts the complete TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004 worker result to Bridge /api/results.
// Only the real Result contract is posted; no probe/dummy payload.
const fs = require('fs');
const path = require('path');

const bridge = 'http://127.0.0.1:17321';
const resultPath = path.join(
  __dirname,
  'RESULT.bridge.txt');

async function main() {
  const content = fs.readFileSync(resultPath, 'utf8');
  const body = {
    channelId: 'tooba-main',
    workerId: 'tooba-worker-01',
    agentType: 'cursor',
    taskId: 'TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004',
    bridgeTaskId: '6d55ed2f-dec1-4b23-9cc3-840e5ddcd4a5',
    status: 'Completed',
    content,
  };

  const res = await fetch(`${bridge}/api/results`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  const text = await res.text();
  console.log(res.status, text);
}

main().catch((err) => {
  console.error('POST /api/results failed:', err);
  process.exit(1);
});
