// TB-TMAR-STORY-AMSC-001-W3-R2 — recover the already-claimed Bridge task payload from the recorded
// terminal transcript of the successful 200 claim response, and persist the task artifacts.
const fs = require('fs');
const path = require('path');

const SRC = 'C:/Users/User/.cursor/projects/d-Users-User-source-repos-SarvNewVer/agent-tools/585e80dd-fedc-4a13-91f1-43675347e9e0.txt';
const OUT = path.join('docs', 'architecture', 'evidence', 'TB-TMAR-STORY-AMSC-001-W3-R2');

const raw = fs.readFileSync(SRC, 'utf8');

// The terminal output wraps the payload; unwrap soft line breaks that PowerShell inserted after the
// "bytes\", with value: \"" marker and before "\", for \"GetString\"".
const start = raw.indexOf('with value: "{');
if (start < 0) {
    throw new Error('payload start marker not found');
}
const from = start + 'with value: "'.length;
const end = raw.indexOf('", for "GetString"', from);
if (end < 0) {
    throw new Error('payload end marker not found');
}

// Rejoin the display-wrapped payload: strip newlines and the "<spaces>NNN|" continuation gutters.
let body = raw.slice(from, end);
body = body.replace(/\r?\n\s*\d+\|\s?/g, '');
body = body.replace(/\r?\n/g, '');

const task = JSON.parse(body);
if (task.taskId !== 'TB-TMAR-STORY-AMSC-001-W3-R2' || task.channelId !== 'tooba-main') {
    throw new Error('unexpected task ' + JSON.stringify(task).slice(0, 200));
}

fs.mkdirSync(OUT, { recursive: true });
fs.mkdirSync(path.join('docs', 'ai', 'tasks'), { recursive: true });
fs.writeFileSync(path.join('docs', 'ai', 'tasks', task.taskId + '.task.md'), task.content, 'utf8');
fs.writeFileSync(path.join(OUT, 'task.md'), task.content, 'utf8');
fs.writeFileSync(path.join(OUT, 'task-receipt.json'),
    JSON.stringify({
        id: task.id,
        channelId: task.channelId,
        taskId: task.taskId,
        createdAtUtc: task.createdAtUtc,
    }, null, 2) + '\n', 'utf8');

console.log('TASK_ROW_ID', task.id);
console.log('TASK_ID', task.taskId);
console.log('CONTENT_LENGTH', task.content.length);
