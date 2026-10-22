const fs = require("fs");
const path = require("path");

const BRIDGE = process.env.TOOBA_BRIDGE || "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1";

const repoRoot = path.resolve(__dirname, "../../../..");
const evidenceDir = __dirname;

(async () => {
  const res = await fetch(
    `${BRIDGE}/api/tasks/next?channelId=${encodeURIComponent(CHANNEL)}&workerId=tooba-worker-01&agentType=cursor`,
  );
  const text = await res.text();
  if (!res.ok) throw new Error(`claim ${res.status}: ${text.slice(0, 300)}`);
  let row = text.trim() ? JSON.parse(text) : null;
  if (Array.isArray(row)) row = row[0];
  if (!row) throw new Error("no claimable task");
  if (row.taskId !== TASK_ID) throw new Error(`unexpected claimed task: ${row.taskId}`);

  const content = row.content || "";
  if (!content.includes("BEGIN_TOOBA_TASK")) throw new Error("task content incomplete");

  fs.writeFileSync(path.join(repoRoot, "docs/ai/tasks", `${TASK_ID}.task.md`), content, "utf8");
  fs.writeFileSync(
    path.join(evidenceDir, "task-receipt.json"),
    JSON.stringify(
      {
        id: row.id,
        taskId: row.taskId,
        channelId: row.channelId || CHANNEL,
        createdAtUtc: row.createdAtUtc,
        claimedAtUtc: row.claimedAtUtc || new Date().toISOString(),
        contentBytes: Buffer.byteLength(content, "utf8"),
      },
      null,
      2,
    ) + "\n",
    "utf8",
  );
  console.log("CLAIMED_ROW", row.id);
  console.log("TASK_BYTES", Buffer.byteLength(content, "utf8"));
})().catch((err) => {
  console.error("CLAIM_FAILED", err.message);
  process.exit(1);
});
