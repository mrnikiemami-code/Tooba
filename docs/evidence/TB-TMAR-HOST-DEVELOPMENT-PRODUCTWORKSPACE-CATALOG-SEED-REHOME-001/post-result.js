const fs = require('fs');
const path = 'docs/evidence/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001/worker-result.txt';
const content = fs.readFileSync(path, 'utf8');
const payload = { channelId: 'tooba-main', taskId: 'TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001', content };

async function main() {
  const res = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  const text = await res.text();
  fs.writeFileSync('docs/evidence/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001/result-post.json', JSON.stringify({ status: res.status, text }));
}

main();
