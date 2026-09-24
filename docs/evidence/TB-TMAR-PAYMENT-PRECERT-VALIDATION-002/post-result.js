const fs = require("fs");

const TASK_ID = "TB-TMAR-PAYMENT-PRECERT-VALIDATION-002";
const TASK_ROW_ID = "0d8d7764-8c9d-4371-b767-da09dc88e99b";
const BRIDGE = "http://127.0.0.1:17321";

const content = fs.readFileSync(
  "docs/evidence/TB-TMAR-PAYMENT-PRECERT-VALIDATION-002/RESULT.bridge.txt",
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
    body: JSON.stringify({}),
  });
  console.log("COMPLETE", complete.status, await complete.text());
}

main().catch((error) => {
  console.error("FAILED", error);
  process.exit(1);
});
