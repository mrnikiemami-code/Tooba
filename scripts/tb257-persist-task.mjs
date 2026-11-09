import fs from "node:fs";
import path from "node:path";

const BRIDGE = "http://127.0.0.1:17321";
const TASK_ID = "TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257";

const targets = [
  `docs/ai/tasks/${TASK_ID}/TASK.md`,
  `docs/architecture/evidence/${TASK_ID}/TASK.md`,
];

async function main() {
  const res = await fetch(`${BRIDGE}/api/tasks/next?channelId=tooba-main`);
  if (!res.ok) throw new Error(`next ${res.status}`);
  const buf = Buffer.from(await res.arrayBuffer());
  const text = buf.toString("utf8").trim();
  if (!text) throw new Error("empty body");
  const parsed = JSON.parse(text);
  const content = parsed.content;
  if (!content) throw new Error("no content");
  for (const t of targets) {
    fs.mkdirSync(path.dirname(t), { recursive: true });
    fs.writeFileSync(t, content.replace(/\r\n/g, "\n"), "utf8");
    console.log("WROTE", t, content.length);
  }
}

main().catch((e) => {
  console.error("FAILED", e.message);
  process.exit(1);
});
