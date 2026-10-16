const fs = require('fs');
const path = require('path');

const B = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const WORKER = 'tooba-worker-01';
const ROW = '3bc91c66-6844-4f92-8338-275d15518c4d';
const TASK = 'TB-SHIPPING-ONE-LEVEL-TABS-120';
const OUT = path.join('docs', 'architecture', 'evidence', TASK);

const content = fs.readFileSync(path.join(OUT, 'RESULT.bridge.txt'), 'utf8');

(async () => {
    const res = await fetch(`${B}/api/results`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            channelId: CHANNEL,
            workerId: WORKER,
            agentType: 'cursor',
            taskId: TASK,
            status: 'PASS',
            content,
        }),
    });
    console.log('RESULT_POST=' + res.status + ' ' + (await res.text()).slice(0, 300));

    const done = await fetch(`${B}/api/tasks/${ROW}/complete`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: WORKER, channelId: CHANNEL }),
    });
    console.log('TASK_COMPLETE=' + done.status + ' ' + (await done.text()).slice(0, 300));

    const hb = await fetch(`${B}/api/workers/heartbeat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: WORKER, channelId: CHANNEL, agentType: 'cursor', status: 'Idle' }),
    });
    console.log('IDLE=' + hb.status);
})().catch((err) => { console.error(err); process.exit(1); });
