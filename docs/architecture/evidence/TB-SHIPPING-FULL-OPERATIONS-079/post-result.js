// TB-SHIPPING-FULL-OPERATIONS-079 — post the complete Task Result to Bridge and close the lifecycle.
const fs = require("fs");
const path = require("path");

const TASK_ROW_ID = "52e6312b-e84c-4f50-92b4-c2f4d0250ab8";
const TASK_ID = "TB-SHIPPING-FULL-OPERATIONS-079";

const resultPath = path.join(__dirname, "RESULT.bridge.txt");
const content = fs.readFileSync(resultPath, "utf8").trim();

async function main() {
    if (!content.includes("BEGIN_TOOBA_WORKER_RESULT") || !content.includes("END_TOOBA_WORKER_RESULT") || !content.includes(`Task-ID: ${TASK_ID}`)) {
        throw new Error("result contract markers missing");
    }
    if (/<[A-Za-z_]+>/.test(content) || /__[A-Z_]+__/.test(content)) {
        throw new Error("unresolved placeholder in transmitted result");
    }
    if (!content.trimEnd().endsWith("END_TOOBA_TASK")) {
        throw new Error("result must end with END_TOOBA_TASK");
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
