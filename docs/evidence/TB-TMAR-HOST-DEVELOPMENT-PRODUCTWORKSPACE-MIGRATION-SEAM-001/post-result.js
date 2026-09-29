const fs = require('fs');
const dir = 'docs/evidence/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001';
const content = fs.readFileSync(dir + '/worker-result.txt', 'utf8');
const payload = { channelId: 'tooba-main', taskId: 'TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001', content };

async function main() {
  const res = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  const text = await res.text();
  fs.writeFileSync(dir + '/result-post.json', JSON.stringify({ status: res.status, text }));
  console.log(res.status, text);

  const done = await fetch('http://127.0.0.1:17321/api/tasks/7ae4d1ce-df9a-4b1f-b694-f52c5aa0598f/complete', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({}),
  });
  const doneText = await done.text();
  fs.writeFileSync(dir + '/complete-post.json', JSON.stringify({ status: done.status, text: doneText }));
  console.log(done.status, doneText);
}

main();
