// Posts the complete TB-SHIPPING-ONE-LEVEL-TABS-142 worker result to Bridge /api/results.
// Only the real Result contract is posted; no probe/dummy payload.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-142';
const TASK_ROW_ID = 'a26229d9-4f4b-4ada-8e2d-5369769b7e25';
const OUT = __dirname;

async function main() {
  const content = fs.readFileSync(path.join(OUT, 'RESULT.bridge.txt'), 'utf8');
  if (!content.includes('BEGIN_TOOBA_WORKER_RESULT')
    || !content.includes('END_TOOBA_WORKER_RESULT')
    || !content.includes(`Task-ID: ${TASK_ID}`)
    || /__[A-Z_]+__/.test(content)) {
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
