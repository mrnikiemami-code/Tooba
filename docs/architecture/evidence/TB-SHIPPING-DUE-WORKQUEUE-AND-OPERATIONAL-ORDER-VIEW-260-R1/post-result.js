// Posts the TB-260-R1 worker result, completes the task row and returns the heartbeat to Idle.
// Run ONLY after every code/doc/commit/push step of this task is finished.
const fs = require("node:fs");
const path = require("node:path");
const body = fs.readFileSync(path.join(__dirname, "result-contract.txt"), "utf8");
const TASK_ROW = "6c1b4b24-d883-4703-ae1d-9d10bdeb7afa";
const BASE = "http://127.0.0.1:17321";
const post = async (url, payload) => {
  const r = await fetch(BASE + url, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: payload === undefined ? undefined : JSON.stringify(payload),
  });
  const text = await r.text();
  console.log(url, r.status, text.slice(0, 400));
  return r.status;
};
(async () => {
  await post("/api/workers/heartbeat", {
    workerId: "tooba-worker-01",
    channelId: "tooba-main",
    agentType: "cursor",
    status: "Working",
  });
  const s = await post("/api/results", {
    channelId: "tooba-main",
    taskId: TASK_ROW,
    content: body,
  });
  if (s >= 300) return;
  await post(`/api/tasks/${TASK_ROW}/complete`, undefined);
  await post("/api/workers/heartbeat", {
    workerId: "tooba-worker-01",
    channelId: "tooba-main",
    agentType: "cursor",
    status: "Idle",
  });
})();
