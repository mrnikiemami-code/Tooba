// TB-SHIPPING-ONE-LEVEL-TABS-086 — re-claim after the previous claim timed out, and persist the artifact.
// The first claim's payload was lost when the claimer process was terminated mid-flight; the Bridge
// releases a Claimed row after Bridge:ClaimTimeoutSeconds (300s), so this script waits for that release
// and then claims the same single Pending row again. No row is created, mutated or deleted here.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ID = 'TB-SHIPPING-ONE-LEVEL-TABS-086';
const OUT = path.join('docs', 'architecture', 'evidence', TASK_ID);

async function main() {
    const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: 'tooba-worker-01', channelId: CHANNEL, agentType: 'cursor', status: 'Working' }),
    });
    console.log('WORKING', hb.status, await hb.text());

    let task = null;
    const deadline = Date.now() + 12 * 60 * 1000;
    while (task === null && Date.now() < deadline) {
        const res = await fetch(`${BRIDGE}/api/tasks/next?channelId=${CHANNEL}`);
        if (!res.ok && res.status !== 204) {
            throw new Error(`task claim failed ${res.status}`);
        }
        const text = await res.text();
        if (text && text.trim().length > 0) {
            task = JSON.parse(text);
            break;
        }
        await new Promise((r) => setTimeout(r, 5000));
    }

    if (task === null) {
        throw new Error('no task payload received from Bridge');
    }
    if (task.taskId !== TASK_ID || task.channelId !== CHANNEL) {
        throw new Error(`unexpected task ${JSON.stringify(task).slice(0, 500)}`);
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
