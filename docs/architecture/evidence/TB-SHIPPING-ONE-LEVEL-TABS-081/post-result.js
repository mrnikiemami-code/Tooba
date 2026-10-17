// TB-SHIPPING-ONE-LEVEL-TABS-081 — post the complete Result envelope to Bridge, complete the task row, then idle.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const WORKER = 'tooba-worker-01';
const TASK_ROW_ID = 'f21b0561-2f0a-4f00-a21b-3e24188249a6';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-081';

async function postJson(url, body) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
    });
    const text = await res.text();
    return { status: res.status, text };
}

async function main() {
    const content = fs.readFileSync(
        path.join('docs', 'architecture', 'evidence', TASK_ID, 'RESULT.bridge.txt'), 'utf8');

    if (!content.startsWith('BEGIN_TOOBA_WORKER_RESULT') || !content.trimEnd().endsWith('END_TOOBA_WORKER_RESULT')) {
        throw new Error('result envelope is not complete; refusing to post');
    }

    const result = await postJson(`${BRIDGE}/api/results`, {
        channelId: CHANNEL,
        workerId: WORKER,
        taskId: TASK_ID,
        content,
    });
    console.log('RESULT', result.status, result.text.slice(0, 500));
    if (result.status < 200 || result.status >= 300) {
        throw new Error(`result post failed with ${result.status}`);
    }

    const complete = await postJson(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
        channelId: CHANNEL,
        workerId: WORKER,
        taskId: TASK_ID,
    });
    console.log('COMPLETE', complete.status, complete.text.slice(0, 500));

    const idle = await postJson(`${BRIDGE}/api/workers/heartbeat`, {
        workerId: WORKER,
        channelId: CHANNEL,
        agentType: 'cursor',
        status: 'Idle',
    });
    console.log('IDLE', idle.status, idle.text.slice(0, 500));
}

main().catch((err) => { console.error(err); process.exit(1); });
