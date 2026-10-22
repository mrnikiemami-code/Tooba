// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1 — post the single complete Worker Result contract.
// No probing: only the full BEGIN/END_TOOBA_WORKER_RESULT payload is sent, then the Bridge task row
// is completed. Nothing runs after the result is posted.
const fs = require("fs");
const path = require("path");
const { execSync } = require("child_process");

const BRIDGE = process.env.TOOBA_BRIDGE || "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1";

const file = path.join(__dirname, "RESULT.bridge.txt");
const receipt = JSON.parse(fs.readFileSync(path.join(__dirname, "task-receipt.json"), "utf8"));

const git = (cmd) => execSync(cmd, { cwd: path.resolve(__dirname, "../../../.."), encoding: "utf8" }).trim();

(async () => {
    const head = git("git rev-parse HEAD");
    const origin = git("git rev-parse origin/main");
    const scoped = git(
        "git status --porcelain -- docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1.task.md docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W2-R1",
    );
    if (head !== origin) throw new Error(`HEAD ${head} != origin/main ${origin}`);
    if (scoped.length > 0) throw new Error(`scoped tree not clean:\n${scoped}`);

    const elapsedMinutes = Math.max(
        1,
        Math.round((Date.now() - Date.parse(receipt.claimedAtUtc)) / 60000),
    );

    let content = fs.readFileSync(file, "utf8");
    content = content.replace(/^Elapsed-Minutes:.*$/m, `Elapsed-Minutes: ${elapsedMinutes}`);
    content = content.replace(/@@EVIDENCE_SHA@@/g, head);
    if (content.includes("@@")) throw new Error("unresolved placeholder in result content");
    if (!content.includes("BEGIN_TOOBA_WORKER_RESULT") || !content.includes("END_TOOBA_WORKER_RESULT")) {
        throw new Error("result contract markers missing");
    }
    if (!content.includes(`Task-ID: ${TASK_ID}`)) throw new Error("task id missing");
    if (!content.includes("Automatic-Next-Task: NONE")) throw new Error("automatic next task missing");

    const results = await fetch(`${BRIDGE}/api/results`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
    });
    const body = await results.text();
    console.log("RESULT_POST", results.status, body.slice(0, 300));
    if (results.status >= 300) process.exit(1);

    const complete = await fetch(`${BRIDGE}/api/tasks/${receipt.id}/complete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: "{}",
    });
    console.log("TASK_COMPLETE", complete.status, (await complete.text()).slice(0, 200));
})().catch((err) => {
    console.error("POST_FAILED", err.message);
    process.exit(1);
});
