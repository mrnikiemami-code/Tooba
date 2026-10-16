// TB-SHIPPING-ONE-LEVEL-TABS-111 — post the complete Task Result to Bridge and close the task lifecycle.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const WORKER = 'tooba-worker-01';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-111';
const ROW_ID = 'b1671960-37f2-4e51-b282-56367148f59c';
const CONTENT_PATH = path.join('docs', 'architecture', 'evidence', TASK_ID, 'RESULT.bridge.txt');

async function post(url, body) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: body === undefined ? undefined : JSON.stringify(body),
    });
    const text = await res.text();
    return { status: res.status, text };
}

async function main() {
    let content = fs.readFileSync(CONTENT_PATH, 'utf8');
    if (!content.includes('BEGIN_TOOBA_WORKER_RESULT') || !content.includes('END_TOOBA_WORKER_RESULT')) {
        throw new Error('RESULT contract markers missing; refusing to post');
    }
    if (!content.includes(`Task-ID: ${TASK_ID}`)) {
        throw new Error('RESULT contract Task-ID mismatch; refusing to post');
    }

    // Evidence-SHA is already the commit that contains this evidence tree; no substitution needed.
    const result = await post(`${BRIDGE}/api/results`, {
        taskId: TASK_ID,
        channelId: CHANNEL,
        workerId: WORKER,
        agentType: 'cursor',
        content,
    });
    console.log('POST_RESULT', result.status, result.text);

    const complete = await post(`${BRIDGE}/api/tasks/${encodeURIComponent(ROW_ID)}/complete`,
        { taskId: TASK_ID, channelId: CHANNEL, workerId: WORKER });
    console.log('COMPLETE', complete.status, complete.text);

    const idle = await post(`${BRIDGE}/api/workers/heartbeat`, {
        workerId: WORKER, channelId: CHANNEL, agentType: 'cursor', status: 'Idle',
    });
    console.log('IDLE', idle.status, idle.text);
}

main().catch((err) => { console.error(err); process.exit(1); });
