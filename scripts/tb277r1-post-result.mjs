import fs from "node:fs";

const base = "http://127.0.0.1:17321";
const recordId = "14671841-a144-4d59-ad9e-350b3c693b23";
const taskName = "TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1";
const content = fs.readFileSync(
  "docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/RESULT.bridge.txt",
  "utf8",
);

const postResult = await fetch(`${base}/api/results`, {
  method: "POST",
  headers: { "content-type": "application/json" },
  body: JSON.stringify({ channelId: "tooba-main", taskId: taskName, content }),
});
const postBody = await postResult.text();
console.log("POST /api/results", postResult.status, postBody.slice(0, 400));

const complete = await fetch(`${base}/api/tasks/${recordId}/complete`, {
  method: "POST",
  headers: { "content-type": "application/json" },
});
console.log("POST /api/tasks/{id}/complete", complete.status, (await complete.text()).slice(0, 400));

const beat = await fetch(`${base}/api/workers/heartbeat`, {
  method: "POST",
  headers: { "content-type": "application/json" },
  body: JSON.stringify({
    workerId: "tooba-worker-01",
    channelId: "tooba-main",
    agentType: "cursor",
    status: "Idle",
  }),
});
console.log("POST /api/workers/heartbeat", beat.status, (await beat.text()).slice(0, 300));

const check = await fetch(`${base}/api/tasks?channelId=tooba-main`);
const rows = await check.json();
const row = rows.find((x) => x.id === recordId);
console.log("FINAL", JSON.stringify(row));
const results = await (await fetch(`${base}/api/results?channelId=tooba-main&limit=3`)).json();
console.log("LATEST RESULTS", JSON.stringify(results.slice(0, 2)));
