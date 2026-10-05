// TB-TMAR-IDENTITY-AMSC-001-W3-R1 — claim + persist the received downloadable task artifact.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ID = 'TB-TMAR-IDENTITY-AMSC-001-W3-R1';

async function main() {
    const res = await fetch(`${BRIDGE}/api/tasks/next?channelId=${CHANNEL}`);
    if (!res.ok) {
        throw new Error(`task claim failed ${res.status}`);
    }

    const task = await res.json();
    if (!task || task.taskId !== TASK_ID || task.channelId !== CHANNEL) {
        throw new Error(`unexpected task ${JSON.stringify(task)}`);
    }

    const repo = path.join(__dirname, '..', '..', '..');
    const outDir = path.join(repo, 'docs', 'ai', 'tasks');
    fs.mkdirSync(outDir, { recursive: true });
    fs.writeFileSync(path.join(outDir, `${TASK_ID}.task.md`), task.content, 'utf8');
    fs.writeFileSync(
        path.join(__dirname, 'task-receipt.json'),
        JSON.stringify({ id: task.id, taskId: task.taskId, channelId: task.channelId, createdAtUtc: task.createdAtUtc }, null, 2) + '\n',
        'utf8');

    console.log('TASK_ROW_ID', task.id);
    console.log('ARTIFACT docs/ai/tasks/' + TASK_ID + '.task.md');
}

main().catch((err) => {
    console.error(err);
    process.exit(1);
});
