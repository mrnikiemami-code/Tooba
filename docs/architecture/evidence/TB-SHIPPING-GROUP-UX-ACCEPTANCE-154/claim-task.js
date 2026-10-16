// TB-SHIPPING-GROUP-UX-ACCEPTANCE-154 — claim the Bridge task, persist the artifact, set Working heartbeat.
const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const WORKER = "tooba-worker-01";
const TASK_ID = "TB-SHIPPING-GROUP-UX-ACCEPTANCE-154";
const OUT = path.join("docs", "ai", "tasks", `${TASK_ID}.task.md`);

async function main() {
    const hb = await fetch(`${BRIDGE}/api/workers/heartbeat`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ workerId: WORKER, channelId: CHANNEL, agentType: "cursor", status: "Working" }),
    });
    console.log("HEARTBEAT", hb.status, await hb.text());

    const res = await fetch(`${BRIDGE}/api/tasks/next?channelId=${CHANNEL}&workerId=${WORKER}&agentType=cursor`);
    const task = await res.json();
    if (!task || task.taskId !== TASK_ID) {
        throw new Error(`unexpected task: ${task && task.taskId}`);
    }
    if (!task.content.includes("BEGIN_TOOBA_TASK") || !task.content.includes("END_TOOBA_TASK")) {
        throw new Error("task envelope markers missing");
    }
    const header = [
        `Persisted claim artifact — full Architect body received via Bridge`,
        `\`GET /api/tasks/next?channelId=${CHANNEL}&workerId=${WORKER}&agentType=cursor\``,
        `at Bridge task UUID \`${task.id}\` (createdAtUtc ${task.createdAtUtc}).`,
        "",
        "",
    ].join("\n");
    fs.writeFileSync(OUT, header + task.content + "\n", { encoding: "utf8" });
    console.log("CLAIMED", task.id, task.taskId, "bytes", task.content.length);
}

main().catch((err) => {
    console.error(err);
    process.exit(1);
});
