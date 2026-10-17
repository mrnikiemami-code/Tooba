// TB-SHIPPING-ONE-LEVEL-TABS-143 — post the complete Worker Result to Bridge and close the task row.
// No probe/dummy payloads: this posts the final contract once, after validation, commit, push and PID-verified cleanup.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-143';
const TASK_ROW = 'c4da6b80-3799-461f-9563-ca480ca8f4b9';
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
      channelId: CHANNEL,
      taskId: TASK_ID,
      taskRowId: TASK_ROW,
      workerId: 'tooba-worker-01',
      agentType: 'cursor',
      status: 'PASS',
      content,
    }),
  });
  const text = await res.text();
  fs.writeFileSync(path.join(OUT, 'result-post.json'), `${res.status}\n${text}\n`);
  console.log('RESULT', res.status, text.slice(0, 400));
  if (!res.ok) throw new Error(`result post failed ${res.status}`);

  const done = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW}/complete`, { method: 'POST' });
  const doneText = await done.text();
  fs.appendFileSync(path.join(OUT, 'result-post.json'), `COMPLETE ${done.status} ${doneText}\n`);
  console.log('COMPLETE', done.status, doneText.slice(0, 400));

  const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ workerId: 'tooba-worker-01', channelId: CHANNEL, agentType: 'cursor', status: 'Idle' }),
  });
  console.log('IDLE', hb.status, (await hb.text()).slice(0, 200));
}

main().catch((err) => { console.error('POST FAILED:', err.message); process.exit(1); });
