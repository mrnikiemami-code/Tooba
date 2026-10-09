const fs = require("fs");

const TASK_ID = "TB-TMAR-CONTENT-AMSC-001-W3-R1";
const TASK_ROW_ID = "a4c85b03-352a-48b0-9550-6f36f7c84561";
const BRIDGE = "http://127.0.0.1:17321";

const content = fs.readFileSync(
  "docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W3-R1/RESULT.bridge.txt",
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
