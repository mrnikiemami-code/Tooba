const fs = require("fs");
const content = fs.readFileSync("docs/evidence/TB-P07-T038/RESULT-R1.bridge.txt", "utf8");
const payload = {
  channelId: "tooba-main",
  taskId: "TB-P07-T038-R1",
  workerId: "tooba-worker-01",
  status: "PASS",
  content,
};
fs.writeFileSync("docs/evidence/TB-P07-T038/result-r1-post.json", JSON.stringify(payload));

async function main() {
  const results = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  console.log("RESULTS", results.status, await results.text());

  const complete = await fetch(
    "http://127.0.0.1:17321/api/tasks/1c6599cd-7bc0-4663-b22f-3ac717992d30/complete",
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
