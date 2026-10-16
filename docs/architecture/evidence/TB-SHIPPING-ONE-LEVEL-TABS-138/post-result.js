// Posts the complete TB-SHIPPING-ONE-LEVEL-TABS-138 worker result to Bridge /api/results.
// Only the real Result contract is posted; no probe/dummy payload.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-138';
const TASK_ROW_ID = 'fdd439e2-8113-48fb-8e33-48e9ebc00092';
const OUT = path.join(__dirname);

async function main() {
  const content = fs.readFileSync(path.join(OUT, 'RESULT.bridge.txt'), 'utf8');
  if (!content.includes('BEGIN_TOOBA_WORKER_RESULT')
    || !content.includes('END_TOOBA_WORKER_RESULT')
    || !content.includes(`Task-ID: ${TASK_ID}`)
    || content.includes('<')) {
    throw new Error('result contract markers missing or unresolved');
  }

  const res = await fetch(`${BRIDGE}/api/results`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      channelId: 'tooba-main',
      taskId: TASK_ID,
      taskRowId: TASK_ROW_ID,
      workerId: 'tooba-worker-01',
      agentType: 'cursor',
      status: 'PARTIAL',
      content,
    }),
  });
  const text = await res.text();
  fs.writeFileSync(path.join(OUT, 'result-post.json'), `${res.status}\n${text}\n`);
  console.log(res.status, text.slice(0, 400));
}

main().catch((err) => {
  console.error('POST /api/results failed:', err.message);
  process.exit(1);
});
