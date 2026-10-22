// TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2 — post the validated worker result and complete the row.
const fs = require("fs");
const path = require("path");

const BRIDGE = process.env.TOOBA_BRIDGE || "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2";
const ROW_ID = "80a440d4-0324-4805-aa2f-9c5f46fb2b8b";

const content = fs.readFileSync(path.join(__dirname, "worker-result.md"), "utf8");
if (!content.includes("BEGIN_TOOBA_WORKER_RESULT") || !content.includes("END_TOOBA_WORKER_RESULT")) {
  throw new Error("worker result is incomplete");
}

(async () => {
  const res = await fetch(`${BRIDGE}/api/results`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
  });
  const text = await res.text();
  console.log("POST_RESULT", res.status, text.slice(0, 200));
  if (!res.ok) process.exit(1);

  const complete = await fetch(`${BRIDGE}/api/tasks/${ROW_ID}/complete`, { method: "POST" });
  console.log("COMPLETE_TASK", complete.status, (await complete.text()).slice(0, 200));
  if (!complete.ok) process.exit(1);
})().catch((err) => {
  console.error("POST_FAILED", err.message);
  process.exit(1);
});
