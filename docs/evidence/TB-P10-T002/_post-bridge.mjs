import { readFileSync, writeFileSync } from "node:fs";

const content = readFileSync(new URL("./RESULT.bridge.txt", import.meta.url), "utf8");
const body = {
  channelId: "tooba-main",
  workerId: "tooba-worker-01",
  taskId: "TB-P10-T002",
  status: "PASS",
  content,
};
writeFileSync("../../../../.tmp-t002-result-post.json", JSON.stringify(body));
const res = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
console.log("RESULT", res.status, await res.text());
const done = await fetch("http://127.0.0.1:17321/api/tasks/0fa6b153-93ca-4a56-a8f2-2b06e1e0a6cc/complete", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: "{}",
});
console.log("COMPLETE", done.status, await done.text());
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
