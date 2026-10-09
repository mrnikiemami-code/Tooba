const fs = require('fs');

const base = 'http://127.0.0.1:17321';
const taskRowId = 'a4ecce17-a928-4d7c-9eda-d2947fa2500a';
const channelId = 'tooba-main';
const resultPath = 'docs/architecture/evidence/TB-TMAR-SUPPORT-AMSC-001-W3-R2/RESULT.bridge.txt';

const content = fs.readFileSync(resultPath, 'utf8');

async function main() {
  const post = await fetch(base + '/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      taskRowId,
      taskId: 'TB-TMAR-SUPPORT-AMSC-001-W3-R2',
      channelId,
      workerId: 'tooba-worker-01',
      agentType: 'cursor',
      status: 'PASS',
      content,
    }),
  });
  console.log('RESULTS', post.status, (await post.text()).slice(0, 300));

  const complete = await fetch(base + '/api/tasks/' + taskRowId + '/complete', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ workerId: 'tooba-worker-01', channelId }),
  });
  console.log('COMPLETE', complete.status, (await complete.text()).slice(0, 300));

  const beat = await fetch(base + '/api/workers/heartbeat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ workerId: 'tooba-worker-01', channelId, status: 'Idle' }),
  });
  console.log('HEARTBEAT', beat.status, (await beat.text()).slice(0, 200));
}

main().catch((e) => { console.error('ERROR', e.message); process.exit(1); });
