// TB-TMAR-PRICING-AMSC-001-W3-R3 — claim + persist the received downloadable task artifact.
// The Bridge /api/tasks/next endpoint intermittently returns an empty body, so retry until a
// well-formed task payload is received before persisting.
const fs = require('fs');
const path = require('path');

const BRIDGE = 'http://127.0.0.1:17321';
const CHANNEL = 'tooba-main';
const TASK_ID = 'TB-TMAR-PRICING-AMSC-001-W3-R3';
const OUT = path.join('docs', 'architecture', 'evidence', TASK_ID);

async function fetchOnce() {
    const res = await fetch(`${BRIDGE}/api/tasks/next?channelId=${CHANNEL}`);
    if (!res.ok) {
        throw new Error(`task claim failed ${res.status}`);
    }
    const text = await res.text();
    if (!text || text.trim().length === 0) {
        return null;
    }
    return JSON.parse(text);
}

async function main() {
    let task = null;
    for (let attempt = 1; attempt <= 20 && task === null; attempt++) {
        try {
            task = await fetchOnce();
        } catch (err) {
            console.error(`attempt ${attempt} failed: ${err.message}`);
        }
        if (task === null) {
            await new Promise((resolve) => setTimeout(resolve, 500));
        }
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
        }, null, 2) + '\n', 'utf8');
    fs.writeFileSync(path.join(OUT, 'task.md'), task.content, 'utf8');

    console.log('TASK_ROW_ID', task.id);
    console.log('TASK_ID', task.taskId);
    console.log('CREATED', task.createdAtUtc);
    console.log('CONTENT_LENGTH', task.content.length);
}

main().catch((err) => {
    console.error(err);
    process.exit(1);
});
