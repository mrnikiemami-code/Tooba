// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3 — post the complete Task Result to Bridge and close the lifecycle.
const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const TASK_ROW_ID = "d0b47ce2-ff32-4d6d-b537-39c11b87a982";
const TASK_ID = "TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3";
const RESULT_FILE = path.join(__dirname, "RESULT.md");

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
  if (complete.status < 200 || complete.status >= 300) {
    throw new Error("task completion failed");
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
