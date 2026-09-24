const fs = require("fs");

const TASK_ID = "TB-TMAR-PAYMENT-PRECERT-VALIDATION-001";
const TASK_ROW_ID = "d3aae3ff-9006-4abf-a844-b2dcb5a568c1";
const BRIDGE = "http://127.0.0.1:17321";

const content = fs.readFileSync(
  "docs/evidence/TB-TMAR-PAYMENT-PRECERT-VALIDATION-001/RESULT.bridge.txt",
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
