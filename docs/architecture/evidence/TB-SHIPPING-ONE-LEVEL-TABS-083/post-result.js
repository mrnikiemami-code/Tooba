// TB-SHIPPING-ONE-LEVEL-TABS-083 — post the complete Result contract to Bridge, then close the task row.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const WORKER = 'tooba-worker-01';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-083';
const ROW = '3d55d383-879e-45cd-80d9-c9dbec344afa';
const OUT = path.join('docs', 'architecture', 'evidence', TASK_ID);

async function main() {
    const content = fs.readFileSync(path.join(OUT, 'RESULT.bridge.txt'), 'utf8');

    const res = await fetch(`${BRIDGE}/api/results`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            channelId: CHANNEL,
            workerId: WORKER,
            agentType: 'cursor',
            taskId: TASK_ID,
            content,
        }),
    });
    const body = await res.text();
    console.log('RESULT_POST', res.status, body);
    if (!res.ok) throw new Error(`result post failed ${res.status}`);

    const done = await fetch(`${BRIDGE}/api/tasks/${ROW}/complete`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ channelId: CHANNEL, workerId: WORKER, status: 'Completed', note: `${TASK_ID} PASS` }),
    });
    console.log('TASK_COMPLETE', done.status, await done.text());

    const idle = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: WORKER, channelId: CHANNEL, agentType: 'cursor', status: 'Idle' }),
    });
    console.log('IDLE', idle.status, await idle.text());
}

main().catch((err) => { console.error('FAILED', err); process.exit(1); });
