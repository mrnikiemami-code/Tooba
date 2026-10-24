// Posts the complete TB-209-R2 worker result to Bridge, completes the claimed task row, and returns the
// worker heartbeat to Idle. Single call, no probes, no test payloads.
const fs = require("node:fs");
const body = fs.readFileSync(__dirname + "/result-contract.txt", "utf8");
const TASK_ROW = "fd1d9932-7f72-4fff-9e8b-73a5ac1d4696";
(async () => {
  const r = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ taskId: TASK_ROW, channelId: "tooba-main", workerId: "tooba-worker-01", agentType: "cursor", content: body }),
  });
  console.log("result", r.status, (await r.text()).slice(0, 300));
  const c = await fetch(`http://127.0.0.1:17321/api/tasks/${TASK_ROW}/complete`, {
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
  console.log("hb", hb.status);
})();
