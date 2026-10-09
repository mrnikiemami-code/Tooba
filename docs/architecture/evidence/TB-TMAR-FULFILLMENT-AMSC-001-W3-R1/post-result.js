const fs = require("fs");

const BRIDGE = "http://127.0.0.1:17321";
const TASK_ROW_ID = "49bf4c58-ecd9-4ff7-b90d-7c6d143b565b";
const TASK_ID = "TB-TMAR-FULFILLMENT-AMSC-001-W3-R1";
const RESULT_FILE =
  "docs/architecture/evidence/TB-TMAR-FULFILLMENT-AMSC-001-W3-R1/RESULT.bridge.txt";

async function main() {
  const content = fs.readFileSync(RESULT_FILE, "utf8");
  if (
    !content.includes("BEGIN_TOOBA_WORKER_RESULT") ||
    !content.includes("END_TOOBA_WORKER_RESULT") ||
    !content.includes(`Task-ID: ${TASK_ID}`)
  ) {
    throw new Error("result contract markers missing");
  }

  const results = await fetch(`${BRIDGE}/api/results`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ channelId: "tooba-main", taskId: TASK_ID, content }),
  });
  const resultsBody = await results.text();
  console.log("RESULTS", results.status, resultsBody);
  if (results.status < 200 || results.status >= 300) {
    throw new Error("result post failed");
  }

  const complete = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: "{}",
  });
  console.log("COMPLETE", complete.status, await complete.text());

  const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      workerId: "tooba-worker-01",
      channelId: "tooba-main",
      agentType: "cursor",
      status: "Idle",
    }),
  });
  console.log("IDLE", hb.status, await hb.text());
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
