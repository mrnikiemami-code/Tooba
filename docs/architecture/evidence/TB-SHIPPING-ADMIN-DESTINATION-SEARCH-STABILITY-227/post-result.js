// Posts the TB-227 worker result, completes the claimed task row and returns the heartbeat to Idle.
// Run ONLY after every code/doc/commit/push step of this task is finished.
const fs = require("node:fs");
const path = require("node:path");
const body = fs.readFileSync(path.join(__dirname, "result-contract.txt"), "utf8");
const TASK_ROW = "cbb3e006-3c73-453e-b9d4-936fea05d854";
(async () => {
  const r = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ taskId: TASK_ROW, channelId: "tooba-main", workerId: "tooba-worker-01", agentType: "cursor", content: body }),
  });
  console.log("result", r.status, (await r.text()).slice(0, 300));
  if (r.status >= 300) return;
  const c = await fetch(`http://127.0.0.1:17321/api/tasks/${TASK_ROW}/complete`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ workerId: "tooba-worker-01" }),
  });
  console.log("complete", c.status, (await c.text()).slice(0, 160));
  const hb = await fetch("http://127.0.0.1:17321/api/workers/heartbeat", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ workerId: "tooba-worker-01", channelId: "tooba-main", agentType: "cursor", status: "Idle" }),
  });
  console.log("hb", hb.status);
})();
