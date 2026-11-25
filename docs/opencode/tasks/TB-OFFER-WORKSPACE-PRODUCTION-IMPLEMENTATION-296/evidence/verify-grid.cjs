const { chromium } = require("playwright");
const fs = require("fs");
const path = require("path");

const FRONT = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";

async function main() {
  const out = process.argv[2] || path.join(__dirname, "runtime");
  fs.mkdirSync(out, { recursive: true });
  const dev = await (await fetch(`${HOST}/v1/admin/dev-context`)).json();
  const contexts = await (await fetch(`${HOST}/v1/seller/dev-contexts`)).json();
  const seller = contexts.actors[0];

  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await context.addInitScript(({ adminActor, seller }) => {
    localStorage.setItem("tooba.adminActorUserId", adminActor);
    localStorage.setItem("tooba.sellerActorUserId", seller.actorUserId);
    localStorage.setItem("tooba.sellerPartyId", seller.sellerPartyId);
  }, { adminActor: dev.actorUserId, seller });
  const page = await context.newPage();
  const checks = {};

  async function countRows(url, readySelector) {
    await page.goto(url, { waitUntil: "load" });
    await page.waitForSelector(readySelector, { timeout: 30000 });
    await page.waitForTimeout(1200);
    return page.$$eval(".ag-row", (els) => els.length);
  }

  const orders = await countRows(`${FRONT}/admin/orders`, ".ag-root");
  checks.adminOrdersRows = orders;
  const adminOffers = await countRows(`${FRONT}/admin/offers`, ".ag-root");
  checks.adminOffersRows = adminOffers;
  const sellerOffers = await countRows(`${FRONT}/vendor-panel/products?sellerPartyId=${seller.sellerPartyId}`, ".ag-root");
  checks.sellerOffersRows = sellerOffers;
  checks.pass = orders > 0 && adminOffers > 0 && sellerOffers > 0;

  fs.writeFileSync(path.join(out, "verify-grid.json"), JSON.stringify(checks, null, 2));
  console.log(JSON.stringify(checks, null, 2));
  await browser.close();
  if (!checks.pass) process.exitCode = 1;
}

main().catch((error) => { console.error(error); process.exitCode = 2; });
