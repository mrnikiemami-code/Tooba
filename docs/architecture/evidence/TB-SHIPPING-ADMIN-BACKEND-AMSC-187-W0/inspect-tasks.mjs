const url = "http://127.0.0.1:17321/api/tasks?channelId=tooba-main&includeContent=true";
(async () => {
  const res = await fetch(url);
  const body = await res.json();
  const rows = Array.isArray(body) ? body : body.items || body.tasks || body.value || [];
  console.log("STATUS", res.status, "COUNT", rows.length);
  console.log("KEYS", JSON.stringify(Object.keys(rows[0] || {})));
  for (const r of rows) {
    console.log(
      JSON.stringify({ id: r.id, taskId: r.taskId, status: r.status, contentLen: (r.content || "").length }),
    );
  }
})();
