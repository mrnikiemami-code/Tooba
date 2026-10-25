const fs = require("node:fs");
const body = fs.readFileSync(__dirname + "/result-contract.txt", "utf8");
(async () => {
  const r = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ taskId: "9f91a3b1-cbc0-4d59-a29d-47cfacdff594", channelId: "tooba-main", workerId: "tooba-worker-01", agentType: "cursor", content: body }),
  });
  console.log(r.status, (await r.text()).slice(0, 400));
  const c = await fetch("http://127.0.0.1:17321/api/tasks/9f91a3b1-cbc0-4d59-a29d-47cfacdff594/complete", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ workerId: "tooba-worker-01" }) });
  console.log("complete", c.status, (await c.text()).slice(0, 200));
  const hb = await fetch("http://127.0.0.1:17321/api/workers/heartbeat", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ workerId: "tooba-worker-01", channelId: "tooba-main", agentType: "cursor", status: "Idle" }) });
  console.log("hb", hb.status);
})();
