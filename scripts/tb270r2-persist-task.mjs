import fs from "node:fs";

const base = "http://127.0.0.1:17321";
const r = await fetch(`${base}/api/tasks?channelId=tooba-main&status=Claimed`);
const rows = await r.json();
const row = rows.find((x) => x.taskId === "TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2");
if (!row) throw new Error("claim row not found");
console.log("rowId", row.id, "contentBytes", Buffer.byteLength(row.content ?? "", "utf8"));
const dir = "docs/ai/tasks/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2";
fs.mkdirSync(dir, { recursive: true });
fs.writeFileSync(`${dir}/TASK.md`, row.content ?? "", "utf8");
