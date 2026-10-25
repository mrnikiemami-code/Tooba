const fs = require('fs');

const content = fs.readFileSync(__dirname + '/result-contract.txt', 'utf8');

(async () => {
  const response = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      channelId: 'tooba-main',
      taskId: 'TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199',
      content,
    }),
  });
  console.log('RESULT', response.status, (await response.text()).slice(0, 200));
  if (response.status >= 200 && response.status < 300) {
    const done = await fetch('http://127.0.0.1:17321/api/tasks/3bf8097d-160a-49ee-9dd1-fda6d5ab7fe9/complete', { method: 'POST' });
    console.log('COMPLETE', done.status, (await done.text()).slice(0, 200));
  }
})();
