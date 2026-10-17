const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-MVP-OPTION-PARITY-069";
const TASK_ROW_ID = "2babab2d-f070-4ac6-943f-502c4b23faf3";
const RESULT_FILE = path.join(__dirname, "RESULT.bridge.txt");

async function main() {
    const content = fs.readFileSync(RESULT_FILE, "utf8");

    const results = await fetch(`${BRIDGE}/api/results`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
    });
    console.log("results", results.status, (await results.text()).slice(0, 400));

    const complete = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: "{}",
    });
    console.log("complete", complete.status, (await complete.text()).slice(0, 400));

    const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ workerId: "tooba-worker-01", channelId: CHANNEL, agentType: "cursor", status: "Idle" }),
    });
    console.log("heartbeat", hb.status, (await hb.text()).slice(0, 400));
}

main().catch((e) => {
    console.error("ERR", e.message);
    process.exit(1);
});
