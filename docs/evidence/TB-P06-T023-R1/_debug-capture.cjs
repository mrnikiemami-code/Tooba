const { chromium } = require("playwright");
(async () => {
  const browser = await chromium.launch({ headless: true, channel: "msedge" });
  const page = await browser.newPage();
  page.on("console", (m) => console.log("CONSOLE", m.type(), m.text()));
  page.on("pageerror", (e) => console.log("PAGEERROR", e.message));
  page.on("response", (r) => {
    if (r.url().includes("notification") || r.url().includes("/v1/")) {
      console.log("RESP", r.status(), r.url());
    }
  });
  await page.addInitScript(() => {
    window.localStorage.setItem("tooba.customerActorUserId", "aaaaaaaa-aaaa-4aaa-8aaa-000000000009");
  });
  await page.goto("http://127.0.0.1:3000/customer-panel/notifications", { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.waitForTimeout(8000);
  const html = await page.content();
  console.log("HAS_INBOX", html.includes("notifications-inbox-customer\""));
  console.log("HAS_LOADING", html.includes("notifications-inbox-customer-loading"));
  console.log("HAS_TOASTIFY", html.includes("Toastify"));
  console.log("SNIP", html.match(/data-testid=\"notifications[^\"]+\"/)?.[0]);
  await page.screenshot({ path: "captures/debug-customer.png", fullPage: true });
  await browser.close();
})().catch((e) => { console.error(e); process.exit(1); });
