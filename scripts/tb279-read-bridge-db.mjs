import fs from "node:fs";
import { DatabaseSync } from "node:sqlite";

const dbPath = "D:/Users/User/source/repos/Bridge/CursorChatBridge/bin/Debug/net8.0/cursor-chat-bridge.db";
const db = new DatabaseSync(dbPath, { readOnly: true });
const cols = db.prepare("select name from pragma_table_info('BridgeTasks')").all();
console.log("cols:", cols.map((c) => c.name).join(","));
const rows = db
  .prepare("select * from BridgeTasks where TaskId like 'TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279%'")
  .all();
console.log("rows:", rows.length);
for (const row of rows) {
  for (const [k, v] of Object.entries(row)) {
    if (typeof v === "string" && v.length > 300) {
      fs.writeFileSync(`D:/temp/tb279-col-${k}.txt`, v, "utf8");
      console.log(k, "len", v.length, "-> D:/temp/tb279-col-" + k + ".txt");
    } else {
      console.log(k, "=", typeof v === "string" ? v : JSON.stringify(v));
    }
  }
}
