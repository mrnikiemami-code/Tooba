import { readFileSync, writeFileSync } from "node:fs";

const content = readFileSync(new URL("./RESULT.bridge.txt", import.meta.url), "utf8");
const body = {
  channelId: "tooba-main",
  workerId: "tooba-worker-01",
  taskId: "TB-P10-T001",
  status: "PASS",
  content,
};
writeFileSync("../../../../.tmp-t001-result-post.json", JSON.stringify(body));
const res = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const text = await res.text();
console.log("RESULT", res.status, text);
const done = await fetch("http://127.0.0.1:17321/api/tasks/f2942de6-a098-4ee6-9e01-80cec943fc3d/complete", {
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
