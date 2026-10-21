// TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1 — post the single complete Worker Result contract.
// No probing: only the full BEGIN/END_TOOBA_WORKER_RESULT payload is sent.
const fs = require("fs");
const path = require("path");
const { execSync } = require("child_process");

const BRIDGE = process.env.TOOBA_BRIDGE || "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1";
const TASK_ROW_ID = "65dad519-05d5-44fe-8632-47344763349b";
const CODE_SHA = "b0ebe77985df14e771c869e093f3d203481d01a4";

const file = path.join(__dirname, "RESULT.bridge.txt");

(async () => {
    const head = execSync("git rev-parse HEAD", { encoding: "utf8" }).trim();
    const origin = execSync("git rev-parse origin/main", { encoding: "utf8" }).trim();
    const status = execSync("git status --porcelain", { encoding: "utf8" }).trim();
    if (head !== origin) throw new Error(`HEAD ${head} != origin/main ${origin}`);
    if (status.length > 0) throw new Error(`working tree not clean:\n${status}`);

    let content = fs.readFileSync(file, "utf8");
    content = content
        .replace(/@@CODE_SHA@@/g, CODE_SHA)
        .replace(/@@EVIDENCE_SHA@@/g, head)
        .replace(/@@HEAD@@/g, head);
    if (content.includes("@@")) throw new Error("unresolved placeholder in result content");
    if (!content.includes("BEGIN_TOOBA_WORKER_RESULT") || !content.includes("END_TOOBA_WORKER_RESULT")) {
        throw new Error("result contract markers missing");
    }
    if (!content.includes(`Task-ID: ${TASK_ID}`)) throw new Error("task id missing");

    const results = await fetch(`${BRIDGE}/api/results`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
    });
    const body = await results.text();
    console.log("RESULT_POST", results.status, body.slice(0, 400));
    if (results.status >= 300) process.exit(1);

    const complete = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: "{}",
    });
    console.log("TASK_COMPLETE", complete.status, (await complete.text()).slice(0, 200));
})().catch((err) => {
    console.error("POST_FAILED", err.message);
    process.exit(1);
});
