const fs = require("fs");
const content = fs.readFileSync("docs/ai/tasks/TB-P07-T041-R1.result.md", "utf8");
fs.writeFileSync("docs/evidence/TB-P07-T041/RESULT-R1.bridge.txt", content);
const payload = {
  channelId: "tooba-main",
  taskId: "TB-P07-T041-R1",
  workerId: "tooba-worker-01",
  status: "PASS",
  content,
};
fs.writeFileSync("docs/evidence/TB-P07-T041/result-r1-post.json", JSON.stringify(payload));

async function main() {
  const results = await fetch("http://127.0.0.1:17321/api/results", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  console.log("RESULTS", results.status, await results.text());

  const complete = await fetch(
    "http://127.0.0.1:17321/api/tasks/03887560-1aa2-47c2-bdc8-e7640544354a/complete",
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
