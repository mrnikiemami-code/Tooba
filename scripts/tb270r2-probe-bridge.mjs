const base = "http://127.0.0.1:17321";
const id = "55334e35-0b18-4131-9761-cdd9ae64524c";
const urls = [
  `/api/tasks/${id}`,
  `/api/tasks/${id}/content`,
  `/api/tasks/${id}/detail`,
  `/api/tasks/by-id/${id}`,
  `/api/task/${id}`,
  `/api/tasks?channelId=tooba-main&status=Claimed`,
  `/api/inbox?channelId=tooba-main`,
  `/api/results?channelId=tooba-main&limit=3`,
  `/api/instructions?channelId=tooba-main`,
];
for (const u of urls) {
  for (const method of ["GET", "POST"]) {
    try {
      const r = await fetch(base + u, { method, headers: { "content-type": "application/json" }, body: method === "POST" ? "{}" : undefined });
      const t = await r.text();
      console.log(method, u, r.status, t.length, t.slice(0, 120).replace(/\n/g, " "));
    } catch (e) {
      console.log(method, u, "ERR", e.message);
    }
  }
}
