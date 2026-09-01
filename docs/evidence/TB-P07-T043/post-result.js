const fs = require("fs");
const content = fs.readFileSync("docs/evidence/TB-P07-T043/RESULT.bridge.txt", "utf8");
const payload = { channelId: "tooba-main", taskId: "TB-P07-T043", content };
fs.writeFileSync("docs/evidence/TB-P07-T043/result-post.json", JSON.stringify(payload));

async function main() {
  const results = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  console.log("RESULTS", results.status, await results.text());

  const complete = await fetch(
    "http://127.0.0.1:17321/api/tasks/da1a09bc-fce1-4fc8-95ab-ccf9d9abd86f/complete",
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
