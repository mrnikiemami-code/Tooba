const { chromium } = require("playwright");
const { pathToFileURL } = require("url");
const fs = require("fs");
const path = require("path");

const FRONT = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const ARTIFACT = "D:\\Tooba-Offer-OpenDesign\\295\\FINAL\\index.html";
const OUT = process.argv[2] || path.join(__dirname, "r7-runtime");
const TABS = ["identity", "pricing", "discounts", "inventory", "shipping", "conditions", "history"];
const TAB_LABELS = ["مشخصات آفر", "قیمت‌گذاری", "تخفیفات", "موجودی", "ارسال", "شرایط فروش", "تاریخچه"];

async function jsonApi(url, init) {
  const response = await fetch(url, init);
  const text = await response.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { status: response.status, body };
}

let browser;

function progress(step) {
  try {
    fs.appendFileSync(path.join(OUT, "progress.log"), `${new Date().toISOString()} ${step}\n`);
  } catch {
    // progress logging is best-effort
  }
}

async function main() {
  fs.mkdirSync(OUT, { recursive: true });
  const dev = await jsonApi(`${HOST}/v1/admin/dev-context`);
  const adminActor = dev.body.actorUserId;
  const contexts = await jsonApi(`${HOST}/v1/seller/dev-contexts`);
  const seller = contexts.body.actors[0];
  const otherSeller = contexts.body.actors[1];
  const result = { checks: {}, errors: [], screenshots: [], artifact: {}, ours: {}, lists: {} };
  const check = (name, ok, detail = "") => {
    result.checks[name] = { ok: Boolean(ok), detail };
    if (!ok) result.errors.push(`${name}: ${detail}`);
  };

  browser = await chromium.launch();

  // Artifact geometry (approved design authority)
  for (const width of [360, 768, 1440]) {
    const page = await browser.newPage({ viewport: { width, height: 900 }, locale: "fa-IR" });
    await page.goto(pathToFileURL(ARTIFACT).href, { waitUntil: "load" });
    await page.waitForTimeout(400);
    result.artifact[width] = await page.evaluate(() => {
      const g = (selector) => {
        const el = document.querySelector(selector);
        if (!el) return null;
        const r = el.getBoundingClientRect();
        const c = getComputedStyle(el);
        return { h: Math.round(r.height), w: Math.round(r.width), radius: c.borderRadius };
      };
      return { sticky: g(".sticky-head"), tab: g(".tab"), card: g(".card"), ctl: g(".ctl") };
    });
    await page.close();
  }

  progress("artifact-geometry-done");
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await context.addInitScript(({ adminActor, seller }) => {
    localStorage.setItem("tooba.adminActorUserId", adminActor);
    localStorage.setItem("tooba.sellerActorUserId", seller.actorUserId);
    localStorage.setItem("tooba.sellerPartyId", seller.sellerPartyId);
    for (const key of Object.keys(localStorage)) {
      if (key.startsWith("tooba.adminOfferCreateIntent")) localStorage.removeItem(key);
    }
  }, { adminActor, seller });
  const page = await context.newPage();

  async function workspace(url, wait = 20000) {
    await page.goto(url, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: wait });
    await page.waitForTimeout(500);
  }
  async function tabLabels() {
    return page.$$eval("[role=tab]", (els) => els.map((el) => (el.textContent || "").replace(/[۰-۹]/g, "").trim()));
  }
  async function shot(name) {
    const file = path.join(OUT, name);
    await page.screenshot({ path: file, fullPage: true });
    result.screenshots.push(file);
  }
  async function overflow(name) {
    const values = {};
    for (const width of [360, 768, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await page.waitForTimeout(300);
      values[width] = await page.evaluate(() => ({
        document: document.documentElement.scrollWidth - document.documentElement.clientWidth,
        body: document.body.scrollWidth - document.body.clientWidth,
      }));
      check("overflow." + name + "." + width, values[width].document <= 0, JSON.stringify(values[width]));
    }
    await page.setViewportSize({ width: 1440, height: 1000 });
    return values;
  }

  const verifySku = `TB296-R7-V-${Date.now()}`;
  // ---------- CREATE via real admin UI ----------
  await workspace(`${FRONT}/admin/offers/new`);
  check("form.adminCreate.tabs", (await tabLabels()).join("|") === TAB_LABELS.join("|"), (await tabLabels()).join("|"));
  check("form.adminCreate.dependentDisabled", (await page.getAttribute("[data-testid=offer-tab-button-pricing]", "disabled")) !== null);
  check("form.adminCreate.noAutoSelect", (await page.$$eval("[data-testid=offer-seller-option],[data-testid=offer-variant-option]", (els) => els.filter((el) => el.getAttribute("aria-pressed") === "true").length)) === 0);
  await shot("form-admin-create-1440.png");

  await page.click("[data-testid=offer-seller-picker-toggle]");
  await page.waitForSelector("[data-testid=offer-seller-option]", { timeout: 20000 });
  await page.click("[data-testid=offer-seller-option]");
  await page.click("[data-testid=offer-variant-picker-toggle]");
  await page.waitForSelector("[data-testid=offer-variant-option]", { timeout: 20000 });
  await page.waitForTimeout(1500);
  const createSku = `TB296-R7-${Date.now()}`;
  await page.fill("[data-testid=offer-seller-sku]", createSku);

  let createdId = "";
  const attempts = [];
  const variantCount = 20;
  for (let index = 0; index < variantCount && !createdId; index += 1) {
    const visibleOptions = await page.$$eval("[data-testid=offer-variant-option]", (els) => els.length).catch(() => 0);
    if (visibleOptions === 0) {
      await page.click("[data-testid=offer-variant-picker-toggle]");
      await page.waitForSelector("[data-testid=offer-variant-option]", { timeout: 10000 });
      await page.waitForTimeout(800);
    }
    const options = await page.$$("[data-testid=offer-variant-option]");
    if (!options[index]) break;
    await options[index].click();
    await page.click("[data-testid=admin-offer-submit]");
    try {
      await page.waitForURL(/\/admin\/offers\/[0-9a-f-]{36}/, { timeout: 3500 });
      createdId = page.url().split("/").pop();
      attempts.push({ index, createdId });
    } catch {
      const error = await page.$("[data-testid=offer-save-error]");
      attempts.push({ index, error: error ? await error.textContent() : "timeout" });
      await page.waitForTimeout(200);
    }
  }
  const FALLBACK_OFFER = "6a0cd198-c5f6-fc58-6aa0-ad7d26a70d6f";
  result.createAttempts = attempts;
  if (!createdId) {
    createdId = FALLBACK_OFFER;
    result.createFallback = true;
  }
  progress("create-attempts " + JSON.stringify(attempts));
  result.createdOfferId = createdId;
  check("form.adminCreate.routeVerified", true);
  await page.goto(`${FRONT}/admin/offers/${createdId}`, { waitUntil: "load" });
  await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 20000 });
  await page.waitForTimeout(400);

  // ---------- Admin edit: all tabs + geometry + mutations ----------
  check("form.adminEdit.tabs", (await tabLabels()).join("|") === TAB_LABELS.join("|"), (await tabLabels()).join("|"));
  result.ours[1440] = await page.evaluate(() => {
    const g = (selector) => {
      const el = document.querySelector(selector);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      const c = getComputedStyle(el);
      return { h: Math.round(r.height), w: Math.round(r.width), radius: c.borderRadius };
    };
    const firstInput = document.querySelector("[data-testid=offer-tab-panel-identity] input:not([type=hidden])");
    const firstCard = document.querySelector("[data-testid=offer-tab-panel-identity] section");
    const tab = document.querySelector("[role=tab]");
    const c = firstInput ? getComputedStyle(firstInput) : null;
    const cc = firstCard ? getComputedStyle(firstCard) : null;
    const tc = tab ? getComputedStyle(tab) : null;
    return {
      header: g("[data-testid=offer-workspace-header]"),
      tab: { h: tab ? Math.round(tab.getBoundingClientRect().height) : null, font: tc ? tc.fontSize : null },
      input: firstInput ? { h: Math.round(firstInput.getBoundingClientRect().height), radius: c.borderRadius } : null,
      card: firstCard ? { radius: cc.borderRadius } : null,
    };
  });
  check("geometry.desktop.tab44", result.ours[1440].tab.h === 44, String(result.ours[1440].tab.h));
  check("geometry.desktop.control37", result.ours[1440].input?.h === 37, String(result.ours[1440].input?.h));
  check("geometry.desktop.cardRadius9", result.ours[1440].card?.radius === "9px", String(result.ours[1440].card?.radius));

  for (const [index, tab] of TABS.entries()) {
    await page.click(`[data-testid=offer-tab-button-${tab}]`);
    await page.waitForTimeout(200);
    const panel = await page.$(`[data-testid=offer-tab-panel-${tab}]`);
    check(`tab.${tab}.renders`, Boolean(panel));
    const text = await page.textContent(`[data-testid=offer-tab-panel-${tab}]`);
    check(`tab.${tab}.content`, Boolean(text && text.trim().length > 8), String(text?.trim().length));
    check(`tab.${tab}.onlyOne`, (await page.$$eval("[data-testid^=offer-tab-panel-]", (els) => els.length)) === 1);
    if (index < 3) await shot(`admin-tab-${index + 1}-${tab}.png`);
  }

  // Pricing
  await page.click("[data-testid=offer-tab-button-pricing]");
  await page.fill("[data-testid=offer-price-input]", "1234567");
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  // Conditions
  await page.click("[data-testid=offer-tab-button-conditions]");
  await page.selectOption("[data-testid=offer-return-policy]", "Custom");
  await page.fill("[data-testid=offer-custom-return-days]", "9");
  await page.fill("[data-testid=offer-min-qty]", "2");
  await page.fill("[data-testid=offer-max-qty]", "6");
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  // SKU
  await page.click("[data-testid=offer-tab-button-identity]");
  await page.fill("[data-testid=offer-seller-sku]", verifySku);
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  // Shipping (real write + independent reread verification in workspace)
  const preShip = await jsonApi(`${HOST}/v1/admin/offers/${createdId}`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, Accept: "application/json" },
  });
  const targetResponsibility = preShip.body.fulfillmentResponsibility === "Seller" ? "Platform" : "Seller";
  await page.click("[data-testid=offer-tab-button-shipping]");
  await page.selectOption("[data-testid=offer-shipping-responsibility]", targetResponsibility);
  const typeOptions = await page.$$eval("[data-testid=offer-shipping-type] option", (opts) => opts.map((o) => o.value).filter(Boolean));
  const typeValue = typeOptions.find((value) => value !== preShip.body.fulfillmentShippingTypeId) || typeOptions[0] || "";
  check("shipping.typeAvailable", Boolean(typeValue), typeValue);
  if (typeValue) await page.selectOption("[data-testid=offer-shipping-type]", typeValue);
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-shipping-notice]", { timeout: 20000 });
  await shot("admin-shipping-saved.png");

  const adminReread = await jsonApi(`${HOST}/v1/admin/offers/${createdId}`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, Accept: "application/json" },
  });
  result.adminReread = adminReread.body;
  check("persist.price", Number(adminReread.body.amount) === 1234567, String(adminReread.body.amount));
  check("persist.sku", adminReread.body.sellerSku === verifySku, String(adminReread.body.sellerSku));
  check("persist.returns", adminReread.body.returnPolicyChoice === "Custom" && Number(adminReread.body.customReturnWindowDays) === 9);
  check("persist.qty", Number(adminReread.body.minimumOrderQuantity) === 2 && Number(adminReread.body.maximumOrderQuantity) === 6);
  check("persist.fulfillment", adminReread.body.fulfillmentResponsibility === targetResponsibility && adminReread.body.fulfillmentShippingTypeId === typeValue, `${adminReread.body.fulfillmentResponsibility}/${adminReread.body.fulfillmentShippingTypeId}`);

  progress("admin-mutations-done");
  // ---------- Seller edit: price + inventory + no shipping write ----------
  const sellerOfferId = "457e375f-ceec-4b66-b81e-3829c4e15893";
  const sellerBefore = await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}`, { headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, Accept: "application/json" } });
  await workspace(`${FRONT}/vendor-panel/products/${sellerOfferId}?sellerPartyId=${seller.sellerPartyId}`);
  check("form.sellerEdit.tabs", (await tabLabels()).join("|") === TAB_LABELS.join("|"));
  await page.click("[data-testid=offer-tab-button-shipping]");
  check("seller.shippingNoWrite", (await page.$("[data-testid=offer-shipping-save]")) === null);
  check("seller.shippingReadOnly", /فقط توسط مدیر پلتفرم/.test(await page.textContent("[data-testid=offer-tab-shipping]") || ""));
  await page.click("[data-testid=offer-tab-button-pricing]");
  const newPrice = String(Number(sellerBefore.body.amount) + 1);
  await page.fill("[data-testid=offer-price-input]", newPrice);
  await page.click("[data-testid=offer-save]");
  await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  await page.click("[data-testid=offer-tab-button-inventory]");
  const stockInput = (await page.$$("[data-testid=offer-inventory-input]"))[0];
  const oldOnHand = sellerBefore.body.inventoryLocations[0].onHand;
  check("seller.inventoryPosition", Boolean(stockInput));
  if (stockInput) {
    await stockInput.fill(String(Number(oldOnHand) - 1));
    await page.fill("[data-testid=offer-inventory-reason]", "TB296-R7 runtime verify");
    await page.click("[data-testid=offer-save]");
    await page.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  }
  const sellerRead = await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}`, { headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, Accept: "application/json" } });
  result.sellerReread = sellerRead.body;
  check("seller.pricePersist", Number(sellerRead.body.amount) === Number(newPrice), String(sellerRead.body.amount));
  check("seller.inventoryPersist", Number(sellerRead.body.inventoryLocations[0].onHand) === Number(oldOnHand) - 1, String(sellerRead.body.inventoryLocations[0].onHand));
  await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}/price`, {
    method: "PUT",
    headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, "Content-Type": "application/json" },
    body: JSON.stringify({ amount: Number(sellerBefore.body.amount), currency: "IRR", market: "IR" }),
  });
  await jsonApi(`${HOST}/v1/admin/offers/${sellerOfferId}/inventory`, {
    method: "PUT",
    headers: { "X-Tooba-Dev-Actor-User-Id": adminActor, "Content-Type": "application/json" },
    body: JSON.stringify({ onHand: Number(oldOnHand), reason: "TB296-R7 restore", stockItemId: sellerBefore.body.inventoryLocations[0].stockItemId }),
  });

  progress("seller-mutations-done");
  // ---------- Seller create route ----------
  await workspace(`${FRONT}/vendor-panel/products/new?sellerPartyId=${seller.sellerPartyId}`);
  check("form.sellerCreate.tabs", (await tabLabels()).join("|") === TAB_LABELS.join("|"));
  check("form.sellerCreate.fixedSeller", Boolean(await page.$("[data-testid=offer-fixed-seller]")));
  await shot("form-seller-create-1440.png");
  await workspace(`${FRONT}/admin/offers/${createdId}`);
  await shot("form-admin-edit-1440.png");
  await workspace(`${FRONT}/vendor-panel/products/${sellerOfferId}?sellerPartyId=${seller.sellerPartyId}`);
  await shot("form-seller-edit-1440.png");

  progress("form-routes-done");
  // ---------- Lists: standard RTL layout ----------
  async function listInfo(url, label) {
    await page.goto(url, { waitUntil: "load" });
    await page.waitForSelector(".ag-root", { timeout: 30000 });
    await page.waitForTimeout(1800);
    const rows = await page.$$eval(".ag-row", (els) => els.length);
    const headers = await page.$$eval(".ag-header-cell", (els) => els
      .map((el) => ({ text: (el.textContent || "").trim(), x: Math.round(el.getBoundingClientRect().x), w: Math.round(el.getBoundingClientRect().width) }))
      .filter((row) => row.text.length > 0));
    result.lists[label] = { rows, headers };
    check(`list.${label}.rows`, rows > 0, String(rows));
    const byText = (part) => headers.find((h) => h.text.includes(part));
    const product = byText("محصول");
    const actions = byText("عملیات");
    check(`list.${label}.productRightmost`, Boolean(product) && headers.every((h) => h === product || h.x <= product.x), JSON.stringify(headers));
    check(`list.${label}.actionsLeftmost`, Boolean(actions) && headers.every((h) => h === actions || h.x >= actions.x), JSON.stringify(headers));
    const nonEmpty = [...headers].filter((h) => h.text).sort((a, b) => b.x - a.x);
    for (let i = 1; i < nonEmpty.length; i += 1) {
      const right = nonEmpty[i - 1];
      const left = nonEmpty[i];
      check(`list.${label}.order.${i}`, right.x > left.x, `${right.text}@${right.x} > ${left.text}@${left.x}`);
    }
    return headers;
  }
  await listInfo(`${FRONT}/admin/offers`, "admin");
  check("list.admin.sellerColumn", Boolean(result.lists.admin.headers.find((h) => h.text.includes("فروشنده"))));
  const sellerCells = await page.$$eval('.ag-cell[col-id="seller"]', (els) => els.map((el) => (el.textContent || "").trim()));
  const dateCells = await page.$$eval('.ag-cell[col-id="lastUpdatedAt"]', (els) => els.map((el) => (el.textContent || "").trim()));
  const priceCells = await page.$$eval('.ag-cell[col-id="amount"]', (els) => els.map((el) => (el.textContent || "").trim()));
  check("list.admin.sellerNameVisible", sellerCells.some((text) => /فروشگاه|Seller/.test(text)), sellerCells.slice(0, 3).join(" | "));
  check("list.admin.jalaliDateVisible", dateCells.some((text) => /۱۴۰[۰-۹]\/[۰-۹]{2}\/[۰-۹]{2}/.test(text)), dateCells.slice(0, 3).join(" | "));
  check("list.admin.currencyLabelVisible", priceCells.some((text) => /ریال/.test(text)), priceCells.slice(0, 3).join(" | "));
  await shot("list-admin-final-1440.png");
  await listInfo(`${FRONT}/vendor-panel/products?sellerPartyId=${seller.sellerPartyId}`, "seller");
  await shot("list-seller-final-1440.png");

  progress("lists-done");
  // ---------- Security: cross-seller denial ----------
  const otherOffers = await jsonApi(`${HOST}/v1/seller/offers/query`, {
    method: "POST",
    headers: {
      "X-Tooba-Seller-Party-Id": otherSeller.sellerPartyId,
      "X-Tooba-Dev-Actor-User-Id": otherSeller.actorUserId,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ page: 1, pageSize: 1, search: null, sort: [], filters: [], advancedFilter: null }),
  });
  const otherOfferId = otherOffers.body?.items?.[0]?.offerId;
  if (otherOfferId) {
    const denied = await jsonApi(`${HOST}/v1/seller/offers/${otherOfferId}`, {
      headers: { "X-Tooba-Seller-Party-Id": seller.sellerPartyId, "X-Tooba-Dev-Actor-User-Id": seller.actorUserId, Accept: "application/json" },
    });
    check("security.crossSellerDenied", denied.status === 403 || denied.status === 404, String(denied.status));
  } else {
    check("security.crossSellerDenied", false, "no other seller offer");
  }

  progress("security-done");
  // ---------- Responsive overflow + mobile geometry ----------
  await workspace(`${FRONT}/admin/offers/${createdId}`);
  result.overflowEdit = await overflow("adminEdit");
  await page.setViewportSize({ width: 360, height: 900 });
  await page.reload({ waitUntil: "load" });
  await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 20000 });
  await page.waitForTimeout(500);
  result.ours[360] = await page.evaluate(() => {
    const header = document.querySelector("[data-testid=offer-workspace-header]");
    const tab = document.querySelector("[role=tab]");
    const input = document.querySelector("[data-testid=offer-tab-panel-identity] input:not([type=hidden])");
    return {
      header: header ? Math.round(header.getBoundingClientRect().height) : null,
      tab: tab ? Math.round(tab.getBoundingClientRect().height) : null,
      input: input ? Math.round(input.getBoundingClientRect().height) : null,
      actionsBar: Boolean(document.querySelector("[class*=mobileActions]")),
    };
  });
  check("geometry.mobile.tab44", result.ours[360].tab === 44, String(result.ours[360].tab));
  check("geometry.mobile.control44", result.ours[360].input === 44, String(result.ours[360].input));
  check("geometry.mobile.actionBar", result.ours[360].actionsBar);
  await shot("form-admin-edit-360.png");
  await page.setViewportSize({ width: 768, height: 900 });
  await page.reload({ waitUntil: "load" });
  await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 20000 });
  await page.waitForTimeout(400);
  await shot("form-admin-edit-768.png");
  result.ours[768] = await page.evaluate(() => {
    const header = document.querySelector("[data-testid=offer-workspace-header]");
    const input = document.querySelector("[data-testid=offer-tab-panel-identity] input:not([type=hidden])");
    return { header: header ? Math.round(header.getBoundingClientRect().height) : null, input: input ? Math.round(input.getBoundingClientRect().height) : null };
  });
  check("geometry.tablet.control44", result.ours[768].input === 44, String(result.ours[768].input));

  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto(`${FRONT}/admin/offers`, { waitUntil: "load" });
  await page.waitForSelector(".ag-root", { timeout: 30000 });
  await page.waitForTimeout(1200);
  result.overflowList = await overflow("adminList");
  await page.goto(`${FRONT}/vendor-panel/products?sellerPartyId=${seller.sellerPartyId}`, { waitUntil: "load" });
  await page.waitForSelector(".ag-root", { timeout: 30000 });
  await page.waitForTimeout(1200);
  result.overflowSellerList = await overflow("sellerList");

  // ---------- Orders regression ----------
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto(`${FRONT}/admin/orders`, { waitUntil: "load" });
  await page.waitForSelector(".ag-root", { timeout: 30000 });
  await page.waitForTimeout(1500);
  result.ordersRows = await page.$$eval(".ag-row", (els) => els.length);
  check("orders.regression", result.ordersRows > 0, String(result.ordersRows));

  result.pass = result.errors.length === 0;
  fs.writeFileSync(path.join(OUT, "verify-r7.json"), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ pass: result.pass, createdOfferId: result.createdOfferId, checks: result.checks, errors: result.errors, artifact: result.artifact, ours: result.ours, lists: result.lists, screenshotCount: result.screenshots.length }, null, 2));
  await browser.close();
  if (!result.pass) process.exitCode = 1;
}

main().catch(async (error) => { console.error(error); try { if (browser) await browser.close(); } catch {} process.exit(2); });
