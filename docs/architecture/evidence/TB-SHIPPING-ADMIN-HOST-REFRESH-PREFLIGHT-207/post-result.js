const fs = require("node:fs");
const path = require("node:path");
const body = fs.readFileSync(path.join(__dirname, "result-contract.txt"), "utf8");
const taskRow = "0a4468c8-3aae-4cef-b958-4e559d4db5be";
(async () => {
  const r = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ taskId: taskRow, channelId: "tooba-main", workerId: "tooba-worker-01", agentType: "cursor", content: body }),
  });
  console.log("result", r.status, (await r.text()).slice(0, 300));
  const c = await fetch(`http://127.0.0.1:17321/api/tasks/${taskRow}/complete`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ workerId: "tooba-worker-01" }),
  });
  console.log("complete", c.status, (await c.text()).slice(0, 200));
  const hb = await fetch("http://127.0.0.1:17321/api/workers/heartbeat", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ workerId: "tooba-worker-01", channelId: "tooba-main", agentType: "cursor", status: "Idle" }),
  });
  console.log("heartbeat", hb.status);
})();
