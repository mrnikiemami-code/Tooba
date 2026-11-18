import fs from "node:fs";

const base = "http://127.0.0.1:17321";
const recordId = "650b8b3d-83d1-4d39-a8fb-1b9be2640416";
const taskName = "TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278";
const content = fs.readFileSync("D:/temp/tb278-result.bridge.txt", "utf8");

const postResult = await fetch(`${base}/api/results`, {
  method: "POST",
  headers: { "content-type": "application/json" },
  body: JSON.stringify({ channelId: "tooba-main", taskId: taskName, content }),
});
console.log("POST /api/results", postResult.status, (await postResult.text()).slice(0, 300));

const complete = await fetch(`${base}/api/tasks/${recordId}/complete`, {
  method: "POST",
  headers: { "content-type": "application/json" },
});
console.log("POST /api/tasks/{id}/complete", complete.status, (await complete.text()).slice(0, 300));
