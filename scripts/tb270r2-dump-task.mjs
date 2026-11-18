import fs from "node:fs";

const base = "http://127.0.0.1:17321";
const res = await fetch(
  `${base}/api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`,
);
const text = await res.text();
console.log("status", res.status, "bytes", Buffer.byteLength(text, "utf8"));
fs.writeFileSync("D:/temp/tb270r2-task.json", text, "utf8");
if (text.trim()) {
  let row = JSON.parse(text);
  if (Array.isArray(row)) row = row[0];
  if (row?.content) {
    fs.writeFileSync("D:/temp/tb270r2-task.md", row.content, "utf8");
    console.log("taskId", row.taskId, "contentBytes", Buffer.byteLength(row.content, "utf8"));
  }
}
