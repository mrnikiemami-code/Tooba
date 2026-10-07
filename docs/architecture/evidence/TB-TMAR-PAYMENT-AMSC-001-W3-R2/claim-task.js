// TB-TMAR-PAYMENT-AMSC-001-W3-R2 — persist the received downloadable task artifact for audit.
const fs = require('fs');
const path = require('path');

const TASK_ID = 'TB-TMAR-PAYMENT-AMSC-001-W3-R2';
const RECEIPT = path.join('.git', 'wake-task.json');
const OUT = path.join('docs', 'architecture', 'evidence', TASK_ID);

const task = JSON.parse(fs.readFileSync(RECEIPT, 'utf8'));
if (!task || task.taskId !== TASK_ID || task.channelId !== 'tooba-main') {
    throw new Error('unexpected task ' + JSON.stringify(task).slice(0, 500));
}

fs.mkdirSync(path.join('docs', 'ai', 'tasks'), { recursive: true });
fs.writeFileSync(path.join('docs', 'ai', 'tasks', TASK_ID + '.task.md'), task.content, 'utf8');

fs.mkdirSync(OUT, { recursive: true });
fs.writeFileSync(path.join(OUT, 'task-receipt.json'),
    JSON.stringify({ id: task.id, taskId: task.taskId, channelId: task.channelId, createdAtUtc: task.createdAtUtc }, null, 2) + '\n', 'utf8');
fs.writeFileSync(path.join(OUT, 'task.md'), task.content, 'utf8');

console.log('TASK_ROW_ID', task.id);
console.log('TASK_ID', task.taskId);
console.log('CREATED', task.createdAtUtc);
