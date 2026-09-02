const fs = require("fs");

const content = fs.readFileSync("docs/evidence/TB-P08-T005/RESULT.bridge.txt", "utf8");
const payload = { channelId: "tooba-main", taskId: "TB-P08-T005", content };

async function main() {
  const results = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  console.log("RESULTS", results.status, await results.text());

  const complete = await fetch(
    "http://127.0.0.1:17321/api/tasks/c2a3a44d-ce94-47b0-902d-09aec30b911c/complete",
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
      status: "Waiting",
    }),
  });
  console.log("WAITING", hb.status, await hb.text());
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
