// TB-SHIPPING-ONE-LEVEL-TABS-101 — post the complete Worker Result to Bridge and close the task row.
// No probe/dummy payloads: this posts the final contract once, after validation, commit and push.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ROW = '59b6ee46-6ee8-4006-ac83-e61aed0beb31';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-101';
const OUT = path.join('docs', 'architecture', 'evidence', TASK_ID);

async function main() {
    let content = fs.readFileSync(path.join(OUT, 'RESULT.bridge.txt'), 'utf8');

    const head = require('child_process').execSync('git rev-parse HEAD').toString().trim();
    content = content.replace('<EVIDENCE_SHA>', head);

    const res = await fetch(`${BRIDGE}/api/results`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
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
