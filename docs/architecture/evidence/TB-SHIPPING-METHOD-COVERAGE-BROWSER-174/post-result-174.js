// TB-174 — post the complete Worker Result to the Bridge and complete the task lifecycle.
const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-METHOD-COVERAGE-BROWSER-174";
const HEAD_SHA = process.env.TOOBA_174_HEAD_SHA || "";
const ORIGIN_SHA = process.env.TOOBA_174_ORIGIN_SHA || HEAD_SHA;
const evidenceDir = path.join(process.cwd(), "docs", "architecture", "evidence", TASK_ID);

let content = fs.readFileSync(path.join(evidenceDir, "RESULT.bridge.txt"), "utf8");

if (HEAD_SHA) {
    content = content.replace(
        "Evidence-Commit: this commit (HEAD == origin/main)",
        `Evidence-Commit: ${HEAD_SHA}\nFinal-HEAD-SHA: ${HEAD_SHA}\nFinal-Origin-Main-SHA: ${ORIGIN_SHA}`,
    );
}

(async () => {
    const post = await fetch(`${BRIDGE}/api/results`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
    });
    const body = await post.text();
    console.log("RESULT_POST", post.status, body.slice(0, 600));
    if (post.status >= 300) process.exit(1);

    const tasks = await (await fetch(`${BRIDGE}/api/tasks?channelId=${CHANNEL}`)).json();
    const task = tasks.find((t) => t.taskId === TASK_ID);
    if (task) {
        const done = await fetch(`${BRIDGE}/api/tasks/${task.id}/complete`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ status: "Completed" }),
        });
        console.log("TASK_COMPLETE", done.status, (await done.text()).slice(0, 300));
    } else {
        console.log("TASK_NOT_FOUND_FOR_COMPLETE");
    }
})().catch((err) => { console.error("POST_ERROR", err.message); process.exit(2); });
