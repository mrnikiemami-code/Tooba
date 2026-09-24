const fs = require("fs");

const TASK_ID = "TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001";
const TASK_ROW_ID = "bf52b1f7-20bd-4e63-a9cb-37ad28b54398";
const BRIDGE = "http://127.0.0.1:17321";

const content = fs.readFileSync(
  "docs/evidence/TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001/RESULT.bridge.txt",
  "utf8",
);

async function main() {
  const results = await fetch(`${BRIDGE}/api/results`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ channelId: "tooba-main", taskId: TASK_ID, content }),
  });
  console.log("RESULTS", results.status, await results.text());

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
