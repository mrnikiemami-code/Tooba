// TB-SHIPPING-ONE-LEVEL-TABS-089 — post the complete Worker Result to Bridge and close the task row.
// No probe/dummy payloads: this posts the final contract once, after validation, commit and push.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ROW = 'd0a999ff-652f-4c6b-b8fd-97d32249ea5a';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-089';
const OUT = path.join('docs', 'architecture', 'evidence', TASK_ID);

async function main() {
    const content = fs.readFileSync(path.join(OUT, 'RESULT.bridge.txt'), 'utf8');

    const res = await fetch(`${BRIDGE}/api/results`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, workerId: 'tooba-worker-01', content }),
    });
    console.log('RESULT', res.status, await res.text());
    if (!res.ok) throw new Error(`result post failed ${res.status}`);

    const done = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW}/complete`, { method: 'POST' });
    console.log('COMPLETE', done.status, await done.text());

    const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: 'tooba-worker-01', channelId: CHANNEL, agentType: 'cursor', status: 'Idle' }),
    });
    console.log('IDLE', hb.status, await hb.text());
}

main().catch((err) => { console.error(err); process.exit(1); });
