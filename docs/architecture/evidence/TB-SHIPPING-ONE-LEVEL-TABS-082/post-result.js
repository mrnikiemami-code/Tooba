// TB-SHIPPING-ONE-LEVEL-TABS-082 — post the complete worker result to Bridge, then complete the task row.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ROW = '5d7f2d27-5ed4-4146-a679-adfc5207dc10';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-082';
const EVIDENCE = path.join('docs', 'architecture', 'evidence', TASK_ID);

async function post(url, body) {
    const res = await fetch(`${BRIDGE}${url}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
    });
    const text = await res.text();
    return { status: res.status, text };
}

async function main() {
    const content = fs.readFileSync(path.join(EVIDENCE, 'RESULT.bridge.txt'), 'utf8');

    const result = await post('/api/results', {
        channelId: CHANNEL,
        taskId: TASK_ID,
        workerId: 'tooba-worker-01',
        agentType: 'cursor',
        content,
    });
    console.log('RESULT', result.status, result.text.slice(0, 400));

    const complete = await post(`/api/tasks/${TASK_ROW}/complete`, {
        channelId: CHANNEL,
        workerId: 'tooba-worker-01',
    });
    console.log('COMPLETE', complete.status, complete.text.slice(0, 400));

    const idle = await post('/api/workers/heartbeat', {
        workerId: 'tooba-worker-01',
        channelId: CHANNEL,
        agentType: 'cursor',
        status: 'Idle',
    });
    console.log('IDLE', idle.status, idle.text.slice(0, 300));
}

main().catch((err) => { console.error(err); process.exit(1); });
