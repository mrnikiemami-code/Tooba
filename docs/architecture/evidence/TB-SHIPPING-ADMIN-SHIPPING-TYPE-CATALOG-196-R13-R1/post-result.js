const fs = require('fs');

const content = fs.readFileSync(__dirname + '/result-contract.txt', 'utf8');

(async () => {
  const response = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      channelId: 'tooba-main',
      taskId: 'TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1',
      content,
    }),
  });
  console.log('RESULT', response.status, (await response.text()).slice(0, 200));
  if (response.status >= 200 && response.status < 300) {
    const done = await fetch('http://127.0.0.1:17321/api/tasks/e14ab1aa-44fe-40f0-aa6f-16564686e5e5/complete', { method: 'POST' });
    console.log('COMPLETE', done.status, (await done.text()).slice(0, 200));
  }
})();
