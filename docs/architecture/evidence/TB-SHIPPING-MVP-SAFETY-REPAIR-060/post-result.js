const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-MVP-SAFETY-REPAIR-060";
const TASK_ROW_ID = "a2480cca-10d1-478a-be33-4a3425d2d492";
const RESULT_FILE = path.join(__dirname, "RESULT.bridge.txt");

async function main() {
    const content = fs.readFileSync(RESULT_FILE, "utf8");
    if (!content.includes("BEGIN_TOOBA_WORKER_RESULT")
        || !content.includes("END_TOOBA_WORKER_RESULT")
        || !content.includes(`Task-ID: ${TASK_ID}`)
        || content.includes("(filled after push)")
        || content.includes("<actual")) {
        throw new Error("result contract markers missing or unresolved");
    }

    const results = await fetch(`${BRIDGE}/api/results`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
    });
    console.log("RESULTS", results.status, await results.text());

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
            channelId: CHANNEL,
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
