const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const TASK_ROW_ID = "78429a83-863b-4839-a06c-4d879432b96f";
const TASK_ID = "TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190";
const RESULT_FILE = path.join(
  __dirname,
  "../../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190/worker-result.md",
);

async function main() {
  const content = fs.readFileSync(path.resolve(RESULT_FILE), "utf8");
  if (
    !content.startsWith("PIPELINE-PROTOCOL: BRIDGE-WAKE-V1") ||
    !content.includes("BEGIN_TOOBA_WORKER_RESULT") ||
    !content.includes("END_TOOBA_WORKER_RESULT") ||
    !content.includes(`Task-ID: ${TASK_ID}`) ||
    !content.includes("Status: PASS")
  ) {
    throw new Error("result contract markers invalid");
  }

  const results = await fetch(`${BRIDGE}/api/results`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ channelId: "tooba-main", taskId: TASK_ID, content }),
  });
  const resultsBody = await results.text();
  console.log("RESULTS", results.status, resultsBody);
  if (results.status < 200 || results.status >= 300) throw new Error("result post failed");

  const complete = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: "{}",
  });
  const completeBody = await complete.text();
  console.log("COMPLETE", complete.status, completeBody);
  if (complete.status < 200 || complete.status >= 300) throw new Error("task completion failed");
}

main().catch((err) => {
  console.error("POST_FAILED", err.message);
  process.exit(1);
});
