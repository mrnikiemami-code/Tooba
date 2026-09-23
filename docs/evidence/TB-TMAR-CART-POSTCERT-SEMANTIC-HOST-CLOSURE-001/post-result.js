const fs = require("fs");

const content = fs.readFileSync(
  "docs/evidence/TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001/RESULT.bridge.txt",
  "utf8",
);
const payload = {
  channelId: "tooba-main",
  taskId: "TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001",
  content,
};

async function main() {
  const results = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  console.log("RESULTS", results.status, await results.text());

  const complete = await fetch(
    "http://127.0.0.1:17321/api/tasks/8ee0c133-5459-4ecf-954c-7b14bb62335c/complete",
    { method: "POST", headers: { "Content-Type": "application/json" }, body: "{}" },
  );
  console.log("COMPLETE", complete.status, await complete.text());

  const hb = await fetch("http://127.0.0.1:17321/api/workers/heartbeat", {
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
