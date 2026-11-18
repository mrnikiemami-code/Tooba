import fs from "node:fs";

const base = "http://127.0.0.1:17321";
const recordId = "60f75a8f-52c9-4aa0-bc87-86c6dbaeaced";
const taskName = "TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279";
const content = fs.readFileSync("D:/temp/tb279-result.bridge.txt", "utf8");
console.log("payload bytes", content.length);

const postResult = await fetch(`${base}/api/results`, {
  method: "POST",
  headers: { "content-type": "application/json" },
  body: JSON.stringify({ channelId: "tooba-main", taskId: taskName, content }),
});
console.log("POST /api/results", postResult.status, (await postResult.text()).slice(0, 400));

const complete = await fetch(`${base}/api/tasks/${recordId}/complete`, {
  method: "POST",
  headers: { "content-type": "application/json" },
});
console.log("POST /api/tasks/{id}/complete", complete.status, (await complete.text()).slice(0, 400));
