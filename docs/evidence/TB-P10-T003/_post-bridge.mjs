import { readFileSync, writeFileSync } from "node:fs";

const content = readFileSync(new URL("./RESULT.bridge.txt", import.meta.url), "utf8");
const body = {
  channelId: "tooba-main",
  workerId: "tooba-worker-01",
  taskId: "TB-P10-T003",
  status: "PASS",
  content,
};
writeFileSync(new URL("../../../../.tmp-t003-result-post.json", import.meta.url), JSON.stringify(body));
const res = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
console.log("RESULT", res.status, await res.text());
const done = await fetch("http://127.0.0.1:17321/api/tasks/497e4587-b16b-449d-919b-20cba84727db/complete", {
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
