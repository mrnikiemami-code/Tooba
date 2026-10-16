// TB-SHIPPING-ONE-LEVEL-TABS-121 — post the validated Result contract through Bridge and close the task.
// Reads the committed RESULT.bridge.txt verbatim; no probe payload is ever sent.
const fs = require('fs');
const path = require('path');

const B = 'http://127.0.0.1:17321';
const ROW = '6cff6dd6-701f-4e79-a3e3-b708249fced9';
const TASK = 'TB-SHIPPING-ONE-LEVEL-TABS-121';
const OUT = path.join('docs', 'architecture', 'evidence', TASK);

const result = fs.readFileSync(path.join(OUT, 'RESULT.bridge.txt'), 'utf8');
if (!result.includes('BEGIN_TOOBA_WORKER_RESULT') || !result.includes('END_TOOBA_WORKER_RESULT')) {
    console.error('ASSERT_FAIL: result contract markers missing');
    process.exit(2);
}

(async () => {
    const res = await fetch(`${B}/api/results`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            channelId: 'tooba-main',
            workerId: 'tooba-worker-01',
            agentType: 'cursor',
            taskId: TASK,
            status: 'PASS',
            content: result,
        }),
    });
    console.log('RESULT_POST=' + res.status + ' ' + (await res.text()).slice(0, 300));

    const done = await fetch(`${B}/api/tasks/${ROW}/complete`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: 'tooba-worker-01', channelId: 'tooba-main' }),
    });
    console.log('TASK_COMPLETE=' + done.status + ' ' + (await done.text()).slice(0, 300));

    const hb = await fetch(`${B}/api/workers/heartbeat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: 'tooba-worker-01', channelId: 'tooba-main', agentType: 'cursor', status: 'Idle' }),
    });
    console.log('IDLE=' + hb.status);
})().catch((err) => { console.error(err); process.exit(1); });
