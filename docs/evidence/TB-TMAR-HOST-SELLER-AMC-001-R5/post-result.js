const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, 'worker-result.txt');
const content = fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n').trimEnd();

const payload = {
  channelId: 'tooba-main',
  taskId: 'TB-TMAR-HOST-SELLER-AMC-001-R5',
  content,
};

(async () => {
  const res = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  const text = await res.text();
  fs.writeFileSync(path.join(__dirname, 'result-post.json'), `${res.status}\n${text}\n`);
  const list = await fetch('http://127.0.0.1:17321/api/results');
  fs.writeFileSync(path.join(__dirname, 'results-list.json'), `${list.status}\n${await list.text()}\n`);
})().catch((e) => {
  fs.writeFileSync(path.join(__dirname, 'result-post.json'), `ERR\n${e.stack}\n`);
});
