const fs = require("fs");
const content = fs.readFileSync("docs/ai/tasks/TB-P07-T039.result.md", "utf8");
fs.writeFileSync("docs/evidence/TB-P07-T039/RESULT.bridge.txt", content);
const payload = {
  channelId: "tooba-main",
  taskId: "TB-P07-T039",
  workerId: "tooba-worker-01",
  status: "PASS",
  content,
};
fs.writeFileSync("docs/evidence/TB-P07-T039/result-post.json", JSON.stringify(payload));

async function main() {
  const results = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  console.log("RESULTS", results.status, await results.text());

  const complete = await fetch(
    "http://127.0.0.1:17321/api/tasks/04e1217b-530c-4fa9-87e2-debcf318a3c2/complete",
    { method: "POST", headers: { "Content-Type": "application/json" }, body: "{}" },
  );
  console.log("COMPLETE", complete.status, await complete.text());

  for (const status of ["Waiting", "Idle"]) {
    const hb = await fetch("http://127.0.0.1:17321/api/workers/heartbeat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        workerId: "tooba-worker-01",
        channelId: "tooba-main",
        agentType: "cursor",
        status,
      }),
    });
    console.log(status.toUpperCase(), hb.status, await hb.text());
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
