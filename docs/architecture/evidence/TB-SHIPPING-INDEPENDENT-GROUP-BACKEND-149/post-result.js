// TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149 — post the complete Task Result to Bridge and close the lifecycle.
const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const TASK_ROW_ID = "04cebb83-3df1-4300-882b-f1a80af4270c";
const TASK_ID = "TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149";
const RESULT_FILE = path.join(__dirname, "RESULT.bridge.txt");
const CLAIMED_AT_UTC = new Date("2026-10-16T17:40:19Z");

async function main() {
    const elapsedMinutes = Math.round((Date.now() - CLAIMED_AT_UTC.getTime()) / 60000);
    // Final HEAD is injected at post time so the posted result carries the exact SHAs
    // (the evidence/result artifact itself is a later, evidence-only commit).
    const finalSha = (process.argv[2] || "").trim();
    let content = fs.readFileSync(RESULT_FILE, "utf8").replace("{ELAPSED}", `${elapsedMinutes} minutes`);
    if (finalSha) {
        content = content
            .replace(
                "Head-SHA: 813c8b933a01ce7a049d4a03ec45d26469651318",
                `Head-SHA-at-post: ${finalSha}`)
            .replace(
                "Push: origin/main updated 47acebe16..90e2dc166..813c8b933 ; HEAD == origin/main ; scoped working tree clean",
                `Push: origin/main updated 47acebe16..90e2dc166 -> ${finalSha} (last commit is this evidence/result artifact only); HEAD == origin/main ; scoped working tree clean`);
    }
    if (!content.includes("BEGIN_TOOBA_WORKER_RESULT")
        || !content.includes("END_TOOBA_WORKER_RESULT")
        || !content.includes(`Task-ID: ${TASK_ID}`)) {
        throw new Error("result contract markers missing");
    }

    const results = await fetch(`${BRIDGE}/api/results`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: "tooba-main", taskId: TASK_ID, content }),
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
