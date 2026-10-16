// TB-SHIPPING-ONE-LEVEL-TABS-106 — claim the single Pending task and persist the received artifact.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-106';
const OUT = path.join('docs', 'architecture', 'evidence', TASK_ID);

async function main() {
    const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: 'tooba-worker-01', channelId: CHANNEL, agentType: 'cursor', status: 'Working' }),
    });
    console.log('WORKING', hb.status);

    const res = await fetch(`${BRIDGE}/api/tasks/next?channelId=${CHANNEL}`);
    const text = await res.text();
    if (!text || text.trim().length === 0) {
        console.log('NO_TASK', res.status);
        return;
    }
    const task = JSON.parse(text);
    if (task.taskId !== TASK_ID || task.channelId !== CHANNEL) {
        throw new Error(`unexpected task ${JSON.stringify(task).slice(0, 400)}`);
    }

    fs.mkdirSync(path.join('docs', 'ai', 'tasks'), { recursive: true });
    fs.writeFileSync(path.join('docs', 'ai', 'tasks', `${TASK_ID}.task.md`), task.content, 'utf8');

    fs.mkdirSync(OUT, { recursive: true });
    fs.writeFileSync(path.join(OUT, 'task-receipt.json'),
        JSON.stringify({
            id: task.id,
            taskId: task.taskId,
            channelId: task.channelId,
            createdAtUtc: task.createdAtUtc,
            claimedAtUtc: new Date().toISOString(),
        }, null, 2) + '\n', 'utf8');
    fs.writeFileSync(path.join(OUT, 'task.md'), task.content, 'utf8');

    console.log('TASK_ROW_ID', task.id);
    console.log('TASK_ID', task.taskId);
    console.log('CREATED', task.createdAtUtc);
    console.log('CONTENT_LENGTH', task.content.length);
}

main().catch((err) => { console.error(err); process.exit(1); });
