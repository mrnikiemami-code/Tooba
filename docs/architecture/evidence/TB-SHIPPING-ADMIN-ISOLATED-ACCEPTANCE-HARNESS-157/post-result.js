// TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157 — post the complete Task Result to Bridge and close the lifecycle.
const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const TASK_ROW_ID = "b14563ef-a320-4c9d-89cd-e4b9f6dcce21";
const TASK_ID = "TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157";
const RESULT_FILE = path.join(__dirname, "RESULT.bridge.txt");
const POST_LOG = path.join(__dirname, "result-post-157.json");

async function main() {
    const content = fs.readFileSync(RESULT_FILE, "utf8");
    if (!content.includes("BEGIN_TOOBA_WORKER_RESULT")
        || !content.includes("END_TOOBA_WORKER_RESULT")
        || !content.includes(`Task-ID: ${TASK_ID}`)
        || !content.includes("Status: PARTIAL")
        || !content.includes("automaticNextImplementationTask: NONE")) {
        throw new Error("result contract markers missing");
    }

    const results = await fetch(`${BRIDGE}/api/results`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: "tooba-main", taskId: TASK_ID, content }),
    });
    const resultsBody = await results.text();
    console.log("RESULTS", results.status, resultsBody);
    fs.writeFileSync(POST_LOG, resultsBody);
    if (results.status < 200 || results.status >= 300) {
        throw new Error("result post failed");
    }

    const complete = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: "{}",
    });
    console.log("COMPLETE", complete.status, await complete.text());

    const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            workerId: "tooba-worker-01",
            channelId: "tooba-main",
            agentType: "cursor",
            status: "Idle",
        }),
    });
    console.log("IDLE", hb.status, await hb.text());
}

main().catch((err) => {
    console.error(err);
    process.exit(1);
});
