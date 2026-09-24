const fs = require("fs");

async function main() {
  const res = await fetch(
    "http://127.0.0.1:17321/api/tasks/next?channelId=tooba-main",
  );
  const text = await res.text();
  console.log("STATUS", res.status, "LEN", text.length);
  if (res.status !== 200) {
    console.log(text.slice(0, 500));
    return;
  }
  const obj = JSON.parse(text);
  console.log("claimId", obj.id, "taskId", obj.taskId, "contentLen", obj.content.length);
  fs.writeFileSync(
    "docs/ai/tasks/TB-TMAR-PAYMENT-PRECERT-VALIDATION-002.task.md",
    obj.content,
    "utf8",
  );
  console.log("WROTE");
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
