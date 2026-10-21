const BRIDGE = 'http://127.0.0.1:17321';
(async () => {
    const res = await fetch(`${BRIDGE}/api/tasks?channelId=tooba-main`);
    const t = await res.json();
    const x = t.filter((y) => String(y.taskId).includes('186'));
    console.log(JSON.stringify(x.map((y) => ({ id: y.id, taskId: y.taskId, status: y.status, hasContent: !!y.content, len: (y.content || '').length, created: y.createdAtUtc })), null, 2));
})().catch((e) => { console.error(e.message); process.exit(1); });
