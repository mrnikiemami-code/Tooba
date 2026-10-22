// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1 — claim the single Pending task and persist the received artifact.
const fs = require("fs");
const path = require("path");

const BRIDGE = process.env.TOOBA_BRIDGE || "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1";
const MAX_WAIT_MS = 12 * 60 * 1000;
const POLL_MS = 15 * 1000;

const repoRoot = path.resolve(__dirname, "../../../..");
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function claimOnce() {
  const res = await fetch(
    `${BRIDGE}/api/tasks/next?channelId=${encodeURIComponent(CHANNEL)}&workerId=tooba-worker-01&agentType=cursor`,
  );
  if (res.status === 204) return null;
  const text = await res.text();
  if (!res.ok) throw new Error(`claim ${res.status}: ${text.slice(0, 300)}`);
  if (!text.trim()) return null;
  let row = JSON.parse(text);
  if (Array.isArray(row)) row = row[0];
  if (!row) return null;
  if (row.taskId !== TASK_ID) throw new Error(`unexpected claimed task: ${row.taskId}`);
  return row;
}

(async () => {
  const deadline = Date.now() + MAX_WAIT_MS;
  let row = null;
  while (!row && Date.now() < deadline) {
    row = await claimOnce();
    if (!row) await sleep(POLL_MS);
  }
  if (!row) throw new Error("task not claimable within the wait window");

  const content = row.content || "";
  if (!content.includes("BEGIN_TOOBA_TASK")) {
    throw new Error(`task content looks incomplete: ${content.length} bytes`);
  }

  fs.writeFileSync(path.join(repoRoot, "docs/ai/tasks", `${TASK_ID}.task.md`), content, "utf8");
  fs.writeFileSync(
    path.join(__dirname, "task-receipt.json"),
    JSON.stringify(
      {
        id: row.id,
        taskId: row.taskId,
        channelId: row.channelId || CHANNEL,
        createdAtUtc: row.createdAtUtc,
        claimedAtUtc: row.claimedAtUtc || new Date().toISOString(),
        status: row.status,
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
