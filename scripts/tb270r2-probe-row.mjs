const base = "http://127.0.0.1:17321";
const r = await fetch(`${base}/api/tasks?channelId=tooba-main`);
const rows = await r.json();
const row = rows.find((x) => x.taskId === "TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2");
console.log(JSON.stringify(row, null, 2).slice(0, 2000));
console.log("keys of all rows:", [...new Set(rows.flatMap((x) => Object.keys(x)))].join(","));
