import fs from "node:fs";

const content = fs.readFileSync("docs/ai/tasks/TB-P08-T001-R1.result.md", "utf8");
const payload = { channelId: "tooba-main", taskId: "TB-P08-T001-R1", content };
const results = await fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(payload),
});
console.log("results", results.status, await results.text());
const complete = await fetch(
  "http://127.0.0.1:17321/api/tasks/d4ee2590-455b-4e49-9621-3ad02dacdf7c/complete",
  { method: "POST" },
);
console.log("complete", complete.status, await complete.text());
