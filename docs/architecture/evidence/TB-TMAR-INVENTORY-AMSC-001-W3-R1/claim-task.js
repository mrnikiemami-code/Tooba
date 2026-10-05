// TB-TMAR-INVENTORY-AMSC-001-W3-R1 — claim + persist the received downloadable task artifact.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ID = 'TB-TMAR-INVENTORY-AMSC-001-W3-R1';
const OUT = path.join('docs', 'architecture', 'evidence', 'TB-TMAR-INVENTORY-AMSC-001-W3-R1');

async function main() {
    const res = await fetch(`${BRIDGE}/api/tasks/next?channelId=${CHANNEL}`);
    if (!res.ok) {
        throw new Error(`task claim failed ${res.status}`);
    }
    const task = await res.json();
    if (!task || task.taskId !== TASK_ID || task.channelId !== CHANNEL) {
        throw new Error(`unexpected task ${JSON.stringify(task).slice(0, 500)}`);
    }

    fs.mkdirSync(path.join('docs', 'ai', 'tasks'), { recursive: true });
    fs.writeFileSync(path.join('docs', 'ai', 'tasks', `${TASK_ID}.task.md`), task.content, 'utf8');

    fs.mkdirSync(OUT, { recursive: true });
    fs.writeFileSync(path.join(OUT, 'task-receipt.json'),
        JSON.stringify({ id: task.id, taskId: task.taskId, channelId: task.channelId, createdAtUtc: task.createdAtUtc }, null, 2) + '\n', 'utf8');
    fs.writeFileSync(path.join(OUT, 'task.md'), task.content, 'utf8');

    console.log('TASK_ROW_ID', task.id);
    console.log('TASK_ID', task.taskId);
    console.log('CREATED', task.createdAtUtc);
}

main().catch((err) => {
    console.error(err);
    process.exit(1);
});
