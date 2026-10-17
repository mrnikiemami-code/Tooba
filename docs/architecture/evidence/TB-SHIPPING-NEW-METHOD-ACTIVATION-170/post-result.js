const fs = require("fs");
const path = require("path");

const BRIDGE = process.env.TOOBA_BRIDGE || "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-NEW-METHOD-ACTIVATION-170";
const TASK_ROW_ID = "91dacb11-d357-42b2-9eb8-3ed7307f67ab";

(async () => {
    const content = fs.readFileSync(path.join(__dirname, "RESULT.bridge.txt"), "utf8");

    const results = await fetch(`${BRIDGE}/api/results`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
    });
    console.log("RESULT_POST=" + results.status + " " + (await results.text()).slice(0, 300));

    const complete = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: "{}",
    });
    console.log("TASK_COMPLETE=" + complete.status + " " + (await complete.text()).slice(0, 200));

    const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ workerId: "tooba-worker-01", channelId: CHANNEL, agentType: "cursor", status: "Idle" }),
    });
    console.log("HEARTBEAT=" + hb.status);
})().catch((e) => {
    console.error("POST_ERROR", e && e.message);
    process.exitCode = 1;
});
