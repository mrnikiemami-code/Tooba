// TB-SHIPPING-FULL-OPERATIONS-073 — post the complete Task Result to Bridge and close the lifecycle.
const fs = require("fs");
const path = require("path");

const TASK_ROW_ID = "f6ef6068-9e40-4bf9-936c-384ec6b756fc";
const TASK_ID = "TB-SHIPPING-FULL-OPERATIONS-073";

const resultPath = path.join(__dirname, "RESULT.bridge.txt");
const content = fs.readFileSync(resultPath, "utf8").trim();

async function main() {
    if (!content.includes("BEGIN_TOOBA_WORKER_RESULT") || !content.includes("END_TOOBA_WORKER_RESULT") || !content.includes(`Task-ID: ${TASK_ID}`)) {
        throw new Error("result contract markers missing");
    }
    if (content.includes("<EVIDENCE_SHA>") || content.includes("<actual")) {
        throw new Error("unresolved placeholder in transmitted result");
    }

    const results = await fetch("http://127.0.0.1:17321/api/results", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: "tooba-main", taskId: TASK_ID, content }),
    });
    console.log("RESULTS", results.status, await results.text());
    if (results.status < 200 || results.status >= 300) {
        throw new Error("result post failed");
    }

    const complete = await fetch(`http://127.0.0.1:17321/api/tasks/${TASK_ROW_ID}/complete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: "{}",
    });
    console.log("COMPLETE", complete.status, await complete.text());

    const hb = await fetch("http://127.0.0.1:17321/api/workers/heartbeat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ workerId: "tooba-worker-01", channelId: "tooba-main", agentType: "cursor", status: "Idle" }),
    });
    console.log("IDLE", hb.status, await hb.text());
}

main().catch(err => { console.error(err); process.exit(1); });
