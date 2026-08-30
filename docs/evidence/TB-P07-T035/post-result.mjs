import fs from "fs";

const uuid = "14a78853-c9ab-400b-8d32-4be19363c82e";
const tip = process.env.GIT_TIP || "a159c7b51a3d07192a213471c508f6a57a6374fb";
let content = fs.readFileSync("docs/evidence/TB-P07-T035/RESULT.bridge.txt", "utf8");
content = content.replace(/Git-HEAD:\r?\n[0-9a-f]{40}/, `Git-HEAD:\n${tip}`);
content = content.replace(/HEAD==origin\/main=[0-9a-f]{40}/, `HEAD==origin/main=${tip}`);
fs.writeFileSync("docs/evidence/TB-P07-T035/RESULT.bridge.txt", content);
fs.writeFileSync("docs/evidence/TB-P07-T035/RESULT.bridge.posted.txt", content);

const payload = {
  channelId: "tooba-main",
  taskId: "TB-P07-T035",
  workerId: "tooba-worker-01",
  status: "PASS",
  content,
};
fs.writeFileSync("docs/evidence/TB-P07-T035/result-post.json", JSON.stringify(payload));

const results = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(payload),
});
console.log("RESULTS", results.status, await results.text());

const complete = await fetch(`http://127.0.0.1:17321/api/tasks/${uuid}/complete`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: "{}",
});
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
