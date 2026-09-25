const fs = require("fs");

const BRIDGE = "http://127.0.0.1:17321";
const TASK_ID = "TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001";
const TASK_ROW_ID = "7cb83b6c-259c-48e9-a253-ffabcd60effc";
const content = fs.readFileSync(
  `docs/evidence/${TASK_ID}/RESULT.bridge.txt`,
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
