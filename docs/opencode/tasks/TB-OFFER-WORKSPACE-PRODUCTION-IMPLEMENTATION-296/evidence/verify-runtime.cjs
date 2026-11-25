const { chromium } = require("playwright");
const fs = require("fs");
const path = require("path");

const FRONT = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = process.argv[2] || path.join(__dirname, "runtime");

async function jsonApi(url, init) {
  const response = await fetch(url, init);
  const text = await response.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { status: response.status, body };
}

async function main() {
  fs.mkdirSync(OUT, { recursive: true });
  const dev = await jsonApi(`${HOST}/v1/admin/dev-context`);
  const adminActor = dev.body.actorUserId;
  const contexts = await jsonApi(`${HOST}/v1/seller/dev-contexts`);
  const seller = (contexts.body.actors || [])[0];
  if (!seller) throw new Error("no seller dev context");

  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await context.addInitScript(({ adminActor, seller }) => {
    window.localStorage.setItem("tooba.adminActorUserId", adminActor);
    window.localStorage.setItem("tooba.sellerActorUserId", seller.actorUserId);
    window.localStorage.setItem("tooba.sellerPartyId", seller.sellerPartyId); for (const key of Object.keys(window.localStorage)) { if (key.startsWith("tooba.adminOfferCreateIntent")) window.localStorage.removeItem(key); }
  }, { adminActor, seller });
  const page = await context.newPage();
  const result = { adminActor, seller, checks: {}, screenshots: [], errors: [] };
  const fail = (name, detail) => { result.errors.push(`${name}: ${detail}`); };
  const check = (name, ok, detail = "") => {
    result.checks[name] = { ok: Boolean(ok), detail };
    if (!ok) fail(name, detail || "failed");
  };

  async function tabLabels() {
    return page.$$eval("[role=tab]", (els) => els.map((el) => el.textContent.trim()));
  }
  async function waitWorkspace() {
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 20000 });
  }
  async function clickTab(id) {
    await page.click(`[data-testid=offer-tab-button-${id}]`);
    await page.waitForTimeout(150);
  }
  async function shot(name) {
    const file = path.join(OUT, name);
    await page.screenshot({ path: file, fullPage: true });
    result.screenshots.push(file);
  }

  // ---------- CREATE (real draft) ----------
  await page.goto(`${FRONT}/admin/offers/new`, { waitUntil: "load" });
  await waitWorkspace();
  check("create.tabs7", (await tabLabels()).join("|") === "مشخصات آفر|قیمت‌گذاری|تخفیفات|موجودی|ارسال|شرایط فروش|تاریخچه", (await tabLabels()).join("|"));
  const pricingDisabled = await page.getAttribute("[data-testid=offer-tab-button-pricing]", "disabled");
  check("create.dependentTabsDisabled", pricingDisabled !== null);
  const saveDisabled = await page.getAttribute("[data-testid=admin-offer-submit]", "disabled");
  check("create.saveDisabledWithoutIdentity", saveDisabled !== null);
  await page.waitForSelector("[data-testid=offer-seller-option]", { timeout: 20000 });
  await page.waitForSelector("[data-testid=offer-variant-option]", { timeout: 20000 });
  const preselected = await page.$$eval("[data-testid=offer-seller-option],[data-testid=offer-variant-option]", (els) => els.filter((el) => el.getAttribute("aria-pressed") === "true").length);
  check("create.noAutoSelect", preselected === 0, String(preselected));
  await page.click("[data-testid=offer-seller-option]");
  const createSku = `TB296-CREATE-${Date.now()}`;
  await page.fill("[data-testid=offer-seller-sku]", createSku);
  let createdId = "";
  const attemptLog = [];
  const variantCount = await page.$$eval("[data-testid=offer-variant-option]", (els) => els.length);
  for (let index = 0; index < variantCount && !createdId; index += 1) {
    const variant = (await page.$$("[data-testid=offer-variant-option]"))[index];
    if (!variant) break;
    await variant.click();
    const saveEnabled = await page.getAttribute("[data-testid=admin-offer-submit]", "disabled");
    check(`create.saveEnabledAfterIdentity.${index}`, saveEnabled === null);
    if (saveEnabled !== null) break;
    await page.click("[data-testid=admin-offer-submit]");
    try {
      await page.waitForURL(/\/admin\/offers\/[0-9a-f-]{36}/, { timeout: 3000 });
      createdId = page.url().split("/").pop();
      attemptLog.push({ index, createdId });
    } catch {
      const error = await page.$("[data-testid=offer-save-error]");
      attemptLog.push({ index, error: error ? (await error.textContent()) : "timeout" });
      await page.waitForTimeout(300);
    }
  }
  result.createAttempts = attemptLog;
  result.createdOfferId = createdId;
  check("create.navigatedToEdit", Boolean(createdId), JSON.stringify(attemptLog));
  if (!createdId) throw new Error("create did not succeed: " + JSON.stringify(attemptLog));
  await waitWorkspace();

  // ---------- ADMIN EDIT (real persistence) ----------
  await clickTab("pricing");
  await page.fill("[data-testid=offer-price-input]", "1234567");
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  await clickTab("conditions");
  await page.selectOption("[data-testid=offer-return-policy]", "Custom");
  await page.fill("[data-testid=offer-custom-return-days]", "9");
  await page.fill("[data-testid=offer-min-qty]", "2");
  await page.fill("[data-testid=offer-max-qty]", "6");
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  await clickTab("identity");
  await page.fill("[data-testid=offer-seller-sku]", "TB296-VERIFY");
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });

  const reread = await jsonApi(`${HOST}/v1/admin/offers/${createdId}`, { headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, Accept: "application/json" } });
  result.adminReread = reread;
  check("admin.persistedPrice", Number(reread.body.amount) === 1234567, String(reread.body.amount));
  check("admin.persistedSku", reread.body.sellerSku === "TB296-VERIFY", String(reread.body.sellerSku));
  check("admin.persistedReturns", reread.body.returnPolicyChoice === "Custom" && Number(reread.body.customReturnWindowDays) === 9, `${reread.body.returnPolicyChoice}/${reread.body.customReturnWindowDays}`);
  check("admin.persistedQty", Number(reread.body.minimumOrderQuantity) === 2 && Number(reread.body.maximumOrderQuantity) === 6);

  await clickTab("discounts");
  const discountsText = await page.textContent("[data-testid=offer-tab-discounts]");
  check("admin.discountsGatedNoFake", /Promotion/.test(discountsText) && /ساختگی/.test(discountsText));
  await clickTab("history");
  const historyText = await page.textContent("[data-testid=offer-tab-history]");
  check("admin.historyGatedNoFake", /قرارداد/.test(historyText));

  for (const id of ["identity", "pricing", "discounts", "inventory", "shipping", "conditions", "history"]) {
    await clickTab(id);
    await shot(`admin-tab-${id}.png`);
  }

  // shipping write contract (no read contract exists)
  await clickTab("shipping");
  await page.selectOption("[data-testid=offer-shipping-responsibility]", "Seller");
  const typeDisabled = await page.getAttribute("[data-testid=offer-shipping-type]", "disabled");
  check("admin.shippingTypeEnabledAfterResponsibility", typeDisabled === null);
  await page.click("[data-testid=offer-shipping-save]");
  await page.waitForSelector("[data-testid=offer-shipping-notice]", { timeout: 20000 });
  check("admin.fulfillmentWriteAccepted", true);

  // ---------- SELLER EDIT (real persistence + role gate) ----------
  const sellerOfferId = "457e375f-ceec-4b66-b81e-3829c4e15893";
  const beforeSeller = await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}`, { headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, Accept: "application/json" } });
  await page.goto(`${FRONT}/vendor-panel/products/${sellerOfferId}?sellerPartyId=${seller.sellerPartyId}`, { waitUntil: "load" });
  await waitWorkspace();
  check("seller.tabs7", (await tabLabels()).join("|") === "مشخصات آفر|قیمت‌گذاری|تخفیفات|موجودی|ارسال|شرایط فروش|تاریخچه");
  await clickTab("shipping");
  const sellerShipping = await page.textContent("[data-testid=offer-tab-shipping]");
  check("seller.shippingAdminOnly", /مدیر پلتفرم/.test(sellerShipping));
  check("seller.noShippingSave", (await page.$("[data-testid=offer-shipping-save]")) === null);
  await clickTab("pricing");
  const newPrice = String(Number(beforeSeller.body.amount) + 1);
  await page.fill("[data-testid=offer-price-input]", newPrice);
  await page.click("[data-testid=offer-save]");
  await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  const sellerReread = await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}`, { headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, Accept: "application/json" } });
  result.sellerReread = sellerReread.body;
  check("seller.persistedPrice", Number(sellerReread.body.amount) === Number(newPrice), String(sellerReread.body.amount));
  await clickTab("inventory");
  const stockInput = (await page.$$("[data-testid=offer-inventory-input]"))[0];
  const oldOnHand = beforeSeller.body.inventoryLocations[0].onHand;
  check("seller.inventoryHasPosition", Boolean(stockInput));
  if (stockInput) {
    await stockInput.fill(String(Number(oldOnHand) - 1));
    await page.fill("[data-testid=offer-inventory-reason]", "TB296 runtime verify");
    await page.click("[data-testid=offer-save]");
    await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
    const stockRead = await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}`, { headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, Accept: "application/json" } });
    check("seller.persistedInventory", Number(stockRead.body.inventoryLocations[0].onHand) === Number(oldOnHand) - 1, String(stockRead.body.inventoryLocations[0].onHand));
  }
  await shot("seller-edit.png");

  // restore seller seed values
  await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}/price`, {
    method: "PUT",
    headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, "Content-Type": "application/json" },
    body: JSON.stringify({ amount: Number(beforeSeller.body.amount), currency: "IRR", market: "IR" }),
  });
  await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}/inventory`, {
    method: "PUT",
    headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, "Content-Type": "application/json" },
    body: JSON.stringify({ onHand: Number(oldOnHand), reason: "TB296 restore", stockItemId: beforeSeller.body.inventoryLocations[0].stockItemId }),
  });

  // ---------- Responsive overflow ----------
  await page.goto(`${FRONT}/admin/offers/${createdId}`, { waitUntil: "load" });
  await waitWorkspace();
  result.overflow = {};
  for (const width of [360, 390, 768, 1280, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.waitForTimeout(200);
    const delta = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    result.overflow[width] = delta;
    check(`responsive.${width}.noOverflow`, delta <= 0, String(delta));
    if (width === 360) await shot("admin-mobile-360.png");
  }

  result.pass = result.errors.length === 0;
  fs.writeFileSync(path.join(OUT, "verify-runtime.json"), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ pass: result.pass, createdOfferId: result.createdOfferId, checks: result.checks, errors: result.errors, screenshotCount: result.screenshots.length }, null, 2));
  await browser.close();
  if (!result.pass) process.exitCode = 1;
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 2;
});
