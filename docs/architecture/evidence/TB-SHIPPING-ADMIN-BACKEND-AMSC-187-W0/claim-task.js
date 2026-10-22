// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0 — persist the received task artifact once.
const fs = require("fs");
const path = require("path");

const BRIDGE = process.env.TOOBA_BRIDGE || "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0";

const repoRoot = path.resolve(__dirname, "../../../..");

(async () => {
  const res = await fetch(
    `${BRIDGE}/api/tasks?channelId=${encodeURIComponent(CHANNEL)}&includeContent=true`,
  );
  if (!res.ok) throw new Error(`tasks list ${res.status}`);
  const list = await res.json();
  const rows = Array.isArray(list) ? list : list.items || list.tasks || [];
  const row = rows.find((r) => r.taskId === TASK_ID);
  if (!row) throw new Error("task row not found");

  const taskPath = path.join(repoRoot, "docs/ai/tasks", `${TASK_ID}.task.md`);
  fs.writeFileSync(taskPath, row.content, "utf8");

  const receiptPath = path.join(__dirname, "task-receipt.json");
  fs.writeFileSync(
    receiptPath,
    JSON.stringify(
      {
        id: row.id,
        taskId: row.taskId,
        channelId: row.channelId,
        createdAtUtc: row.createdAtUtc,
        claimedAtUtc: new Date().toISOString(),
        status: row.status,
      },
      null,
      2,
    ) + "\n",
    "utf8",
  );
  console.log("TASK_ROW", row.id, row.status);
  console.log("TASK_BYTES", Buffer.byteLength(row.content, "utf8"));
})().catch((err) => {
  console.error("CLAIM_PERSIST_FAILED", err.message);
  process.exit(1);
});
