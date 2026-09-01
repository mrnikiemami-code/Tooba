import fs from "node:fs";

const content = fs.readFileSync("docs/ai/tasks/TB-P08-T001-R2.result.md", "utf8");
const payload = { channelId: "tooba-main", taskId: "TB-P08-T001-R2", content };
const results = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(payload),
});
console.log("results", results.status, await results.text());
const complete = await fetch(
  "http://127.0.0.1:17321/api/tasks/95eb1301-8e1f-4bc4-9b0a-258cf9720837/complete",
  { method: "POST" },
);
console.log("complete", complete.status, await complete.text());
