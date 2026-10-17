const fs = require("fs");
const path = require("path");

const BRIDGE = "http://127.0.0.1:17321";
const CHANNEL = "tooba-main";
const TASK_ID = "TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169";
const TASK_ROW_ID = "63739b09-3929-41a5-9bf6-e41ebeb1b951";
const HEAD = "b5012e8c17bccee3a42fa22e12de5ec696f7f63e";
const EVIDENCE = "1417d41dd6521e387faf6d671115d7c961a9b63f";

const file = path.join(__dirname, "RESULT.bridge.txt");
let content = fs.readFileSync(file, "utf8");
content = content
  .replace(/^Evidence-SHA:.*$/m, `Evidence-SHA: ${EVIDENCE}`)
  .replace(/^HEAD-SHA:.*$/m, `HEAD-SHA: ${HEAD}`)
  .replace(/^Origin-SHA:.*$/m, `Origin-SHA: ${HEAD}`)
  .replace(/^Push:.*$/m, `Push: fast-forward 814ae78561..${HEAD.slice(0, 9)} (main -> main)`);

(async () => {
  const results = await fetch(`${BRIDGE}/api/results`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ channelId: CHANNEL, taskId: TASK_ID, content }),
  });
  console.log("results", results.status, (await results.text()).slice(0, 200));

  const complete = await fetch(`${BRIDGE}/api/tasks/${TASK_ROW_ID}/complete`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: "{}",
  });
  console.log("complete", complete.status, (await complete.text()).slice(0, 200));

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
  console.log("heartbeat", hb.status, (await hb.text()).slice(0, 200));
})();
