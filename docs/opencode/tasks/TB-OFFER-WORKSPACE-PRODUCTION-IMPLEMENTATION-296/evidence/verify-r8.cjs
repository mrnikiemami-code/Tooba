const { chromium } = require("playwright");
const fs = require("fs");
const path = require("path");

const FRONT = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = process.argv[2] || path.join(__dirname, "r8-runtime");
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER_HEADER = "X-Tooba-Seller-Party-Id";

async function jsonApi(url, init) {
  const response = await fetch(url, init);
  const text = await response.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { status: response.status, body };
}

async function main() {
  fs.mkdirSync(OUT, { recursive: true });
  const result = { startedAt: new Date().toISOString(), checks: {}, errors: [], screenshots: [], metrics: {}, ids: {} };
  const check = (name, ok, detail = "") => {
    result.checks[name] = { ok: Boolean(ok), detail };
    if (!ok) result.errors.push(`${name}: ${detail}`);
  };
  const dev = await jsonApi(`${HOST}/v1/admin/dev-context`);
  const adminActor = dev.body.actorUserId;
  const contexts = await jsonApi(`${HOST}/v1/seller/dev-contexts`);
  const sellerA = contexts.body.actors[0];
  const sellerB = contexts.body.actors[1];
  result.ids.adminActor = adminActor;
  result.ids.sellerA = sellerA;
  result.ids.sellerB = sellerB;

  const adminHeaders = { [ADMIN_HEADER]: adminActor, "Accept-Language": "fa-IR", "Content-Type": "application/json" };
  const sellerAHeaders = { [ADMIN_HEADER]: sellerA.actorUserId, [SELLER_HEADER]: sellerA.sellerPartyId, "Accept-Language": "fa-IR", "Content-Type": "application/json" };
  const sellerBHeaders = { [ADMIN_HEADER]: sellerB.actorUserId, [SELLER_HEADER]: sellerB.sellerPartyId, "Accept-Language": "fa-IR", "Content-Type": "application/json" };
  // Temporary, fully-restored seller-B capability fixture for the seller-panel create proof.
  const fixtureBase = `${HOST}/v1/admin/sellers/${sellerB.sellerPartyId}/access-control`;
  const fixture = { roleId: null, assignmentId: null };
  async function setupSellerFixture() {
    const ceiling = await jsonApi(`${fixtureBase}/ceiling`, { headers: adminHeaders });
    const entries = (ceiling.body ?? []).map((row) => ({
      permissionId: row.permissionId,
      enabled: ["product.create", "product.view", "product.edit"].includes(row.permissionId),
      scopeKind: row.scopeKind,
      scopeResourceId: row.scopeResourceId ?? null,
    }));
    await jsonApi(`${fixtureBase}/ceiling`, { method: "PUT", headers: adminHeaders, body: JSON.stringify({ entries }) });
    const role = await jsonApi(`${fixtureBase}/roles`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ name: `R8 Fixture ${Date.now()}`, code: `r8-fixture-${Date.now()}`, description: "temporary R8 seller-create proof" }) });
    fixture.roleId = role.body?.id;
    if (!fixture.roleId) throw new Error(`fixture role creation failed: ${role.status} ${JSON.stringify(role.body)}`);
    await jsonApi(`${fixtureBase}/roles/${fixture.roleId}/permissions`, {
      method: "PUT",
      headers: adminHeaders,
      body: JSON.stringify([
        { permissionId: "product.create", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
        { permissionId: "product.view", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
        { permissionId: "product.edit", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
      ]),
    });
    const assignment = await jsonApi(`${fixtureBase}/assignments`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ userId: sellerB.actorUserId, roleId: fixture.roleId }) });
    fixture.assignmentId = assignment.body?.id;
    if (!fixture.assignmentId) throw new Error(`fixture assignment failed: ${assignment.status} ${JSON.stringify(assignment.body)}`);
    const caps = await jsonApi(`${HOST}/v1/seller/access-control/me/capabilities`, { headers: sellerBHeaders });
    check("fixture.sellerCreateGranted", (caps.body?.permissions ?? []).some((p) => p.permissionId === "product.create"), JSON.stringify(caps.body?.permissions));
  }
  async function cleanupSellerFixture() {
    if (fixture.assignmentId) await jsonApi(`${fixtureBase}/assignments/${fixture.assignmentId}`, { method: "DELETE", headers: adminHeaders });
    if (fixture.roleId) await jsonApi(`${fixtureBase}/roles/${fixture.roleId}`, { method: "DELETE", headers: adminHeaders });
    const ceiling = await jsonApi(`${fixtureBase}/ceiling`, { headers: adminHeaders });
    const entries = (ceiling.body ?? []).map((row) => ({
      permissionId: row.permissionId,
      enabled: false,
      scopeKind: row.scopeKind,
      scopeResourceId: row.scopeResourceId ?? null,
    }));
    await jsonApi(`${fixtureBase}/ceiling`, { method: "PUT", headers: adminHeaders, body: JSON.stringify({ entries }) });
    const caps = await jsonApi(`${HOST}/v1/seller/access-control/me/capabilities`, { headers: sellerBHeaders });
    check("fixture.sellerCreateGrantRemoved", (caps.body?.permissions ?? []).length === 0, JSON.stringify(caps.body?.permissions));
  }
  async function archiveR8TestOffers() {
    const listed = await jsonApi(`${HOST}/v1/admin/offers/query`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ page: 1, pageSize: 100, filters: [], sorts: [], search: "TB296-R8-" }) });
    for (const item of listed.body?.items ?? []) {
      await jsonApi(`${HOST}/v1/admin/offers/${item.offerId}`, { method: "PATCH", headers: adminHeaders, body: JSON.stringify({ sellerSku: item.sellerSku, status: "Archived", returnPolicyChoice: "Default", minimumOrderQuantity: 1, maximumOrderQuantity: 20 }) });
    }
    const after = await jsonApi(`${HOST}/v1/admin/offers/query`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ page: 1, pageSize: 10, filters: [], sorts: [], search: "TB296-R8-" }) });
    check("fixture.r8OffersArchived", (after.body?.items ?? []).length === 0, JSON.stringify({ remaining: after.body?.totalCount }));
  }
  await setupSellerFixture();

  // ---------- API contract checks ----------
  const productsFa = await jsonApi(`${HOST}/v1/admin/catalog/products?page=0&pageSize=1000&locale=fa-IR`, { headers: adminHeaders });
  check("api.productPicker.pageAndLimit", productsFa.status === 200 && productsFa.body.page === 1 && productsFa.body.pageSize === 100 && Array.isArray(productsFa.body.items), JSON.stringify({ status: productsFa.status, page: productsFa.body?.page, size: productsFa.body?.pageSize, count: productsFa.body?.items?.length }));
  const samsung = (productsFa.body?.items ?? []).find((item) => item.title.includes("سامسونگ هدفون"));
  check("api.productPicker.realProduct", Boolean(samsung?.productId), JSON.stringify(samsung));
  const variantsFa = samsung ? await jsonApi(`${HOST}/v1/admin/catalog/products/${samsung.productId}/variants?locale=fa-IR`, { headers: adminHeaders }) : { status: 0, body: [] };
  const variantsEn = samsung ? await jsonApi(`${HOST}/v1/admin/catalog/products/${samsung.productId}/variants?locale=en-US`, { headers: adminHeaders }) : { status: 0, body: [] };
  check("api.variantPicker.humanLabels", Array.isArray(variantsFa.body) && variantsFa.body.length >= 2 && variantsFa.body.every((v) => Array.isArray(v.axisLabels) && v.axisLabels.length > 0 && v.displayLabel.includes(":")), JSON.stringify(variantsFa.body?.slice(0, 2)));
  check("api.variantPicker.localizedEn", Array.isArray(variantsEn.body) && variantsEn.body.some((v) => /Color|Size|Storage|Capacity/i.test(v.displayLabel)), JSON.stringify(variantsEn.body?.slice(0, 2)));
  const sellerProducts = await jsonApi(`${HOST}/v1/seller/catalog-products?page=1&pageSize=3&locale=fa-IR`, { headers: sellerAHeaders });
  check("api.sellerProductPicker", sellerProducts.status === 200 && Array.isArray(sellerProducts.body.items) && sellerProducts.body.items.length > 0, JSON.stringify({ status: sellerProducts.status, count: sellerProducts.body?.items?.length }));

  const blue = variantsFa.body?.find((v) => v.displayLabel.includes("آبی"));
  const white = variantsFa.body?.find((v) => v.displayLabel.includes("سفید"));
  const duplicateA = blue ? await jsonApi(`${HOST}/v1/admin/offers/active-listing?sellerPartyId=${sellerA.sellerPartyId}&catalogVariantId=${blue.catalogVariantId}`, { headers: adminHeaders }) : { status: 0, body: null };
  check("api.duplicateLookup.existing", duplicateA.status === 200 && duplicateA.body?.offerId, JSON.stringify(duplicateA.body));
  async function findFreeVariant(sellerId, excludeProductId) {
    for (const product of productsFa.body.items) {
      if (excludeProductId && product.productId === excludeProductId) continue;
      const variants = await jsonApi(`${HOST}/v1/admin/catalog/products/${product.productId}/variants?locale=fa-IR`, { headers: adminHeaders });
      for (const variant of variants.body ?? []) {
        const active = await jsonApi(`${HOST}/v1/admin/offers/active-listing?sellerPartyId=${sellerId}&catalogVariantId=${variant.catalogVariantId}`, { headers: adminHeaders });
        if (active.status === 200 && active.body == null) {
          return { product, variant };
        }
      }
    }
    return null;
  }
  const adminFree = await findFreeVariant(sellerB.sellerPartyId);
  const sellerFree = adminFree ? await findFreeVariant(sellerB.sellerPartyId, adminFree.product.productId) : null;
  check("api.fixture.adminFree", Boolean(adminFree), JSON.stringify(adminFree?.product?.title));
  check("api.fixture.sellerFree", Boolean(sellerFree), JSON.stringify(sellerFree?.product?.title));

  const adminOffersFa = await jsonApi(`${HOST}/v1/admin/offers/query`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ page: 1, pageSize: 5, filters: [{ field: "status", operator: "equals", value: "Active" }], sorts: [{ field: "sellerSku", direction: "asc" }] }) });
  const firstOffer = adminOffersFa.body?.items?.[0];
  const channelReal = firstOffer ? await jsonApi(`${HOST}/v1/admin/offers/${firstOffer.offerId}`, { headers: adminHeaders }) : { body: null };
  check("api.list.channelAndLabel", Boolean(firstOffer?.channel) && firstOffer.channel === channelReal.body?.channel && Boolean(firstOffer?.variantLabel), JSON.stringify({ listChannel: firstOffer?.channel, detailChannel: channelReal.body?.channel, label: firstOffer?.variantLabel }));
  const adminOffersEn = await jsonApi(`${HOST}/v1/admin/offers/query`, { method: "POST", headers: { ...adminHeaders, "Accept-Language": "en-US" }, body: JSON.stringify({ page: 1, pageSize: 3, filters: [], sorts: [] }) });
  check("api.list.localeLabels", Array.isArray(adminOffersEn.body?.items) && adminOffersEn.body.items.some((item) => /Color|Size|Storage|Capacity/.test(item.variantLabel ?? "")), JSON.stringify(adminOffersEn.body?.items?.slice(0, 2)?.map((x) => x.variantLabel)));

  // Security: seller A cannot read or write seller B's offer, nor call admin fulfillment.
  const sellerBOffers = await jsonApi(`${HOST}/v1/seller/offers/query`, { method: "POST", headers: sellerBHeaders, body: JSON.stringify({ page: 1, pageSize: 1, filters: [], sorts: [] }) });
  const sellerBOffer = sellerBOffers.body?.items?.[0];
  if (sellerBOffer) {
    const crossRead = await jsonApi(`${HOST}/v1/seller/offers/${sellerBOffer.offerId}`, { headers: sellerAHeaders });
    check("security.crossSellerRead", crossRead.status === 403 || crossRead.status === 404, `status=${crossRead.status}`);
    const crossWrite = await jsonApi(`${HOST}/v1/seller/offers/${sellerBOffer.offerId}/price`, { method: "PUT", headers: sellerAHeaders, body: JSON.stringify({ amount: 111, currency: "IRR" }) });
    check("security.crossSellerWrite", crossWrite.status === 403 || crossWrite.status === 404, `status=${crossWrite.status}`);
    const adminOnly = await jsonApi(`${HOST}/v1/admin/offers/${sellerBOffer.offerId}/fulfillment`, { method: "PUT", headers: sellerAHeaders, body: JSON.stringify({ responsibility: "Seller", shippingTypeId: null }) });
    check("security.sellerShippingWriteDenied", adminOnly.status === 401 || adminOnly.status === 403, `status=${adminOnly.status}`);
  }

  // ---------- Browser: admin create/edit real flow ----------
  const browser = await chromium.launch();
  const adminContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await adminContext.addInitScript(({ adminActor }) => {
    localStorage.setItem("tooba.adminActorUserId", adminActor);
    for (const key of Object.keys(localStorage)) {
      if (key.startsWith("tooba.adminOfferCreateIntent")) localStorage.removeItem(key);
    }
  }, { adminActor });
  const page = await adminContext.newPage();
  const consoleErrors = [];
  page.on("console", (msg) => { if (msg.type() === "error") consoleErrors.push(msg.text()); });
  page.on("pageerror", (error) => consoleErrors.push(String(error)));

  await page.goto(`${FRONT}/admin/offers/new`, { waitUntil: "load" });
  await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
  await page.waitForTimeout(500);
  result.metrics.formLoadMs = await page.evaluate(() => Math.round(performance.now()));
  check("ui.adminCreate.tabsDisabled", await page.locator("[data-testid=offer-tab-button-pricing]").isDisabled(), "pricing tab");
  check("ui.adminCreate.noAutoSelection", (await page.locator("[data-testid=offer-seller-dialog-option][aria-pressed=true], [data-testid=offer-product-dialog-option][aria-pressed=true], [data-testid=offer-variant-option][aria-pressed=true]").count()) === 0, "no auto selection");

  await page.screenshot({ path: path.join(OUT, "admin-create-1440.png"), fullPage: true });

  await page.click("[data-testid=offer-seller-picker-toggle]");
  await page.waitForSelector("[data-testid=offer-seller-dialog-option]", { timeout: 20000 });
  await page.fill("[data-testid=offer-seller-dialog-search]", "دیجی");
  await page.keyboard.press("Enter");
  await page.waitForTimeout(800);
  const sellerOption = page.locator("[data-testid=offer-seller-dialog-option]").filter({ hasText: "دیجی" }).first();
  check("ui.adminCreate.sellerSearch", (await sellerOption.count()) > 0, "seller search result");
  await sellerOption.click();

  const pickerStart = Date.now();
  await page.click("[data-testid=offer-product-picker-toggle]");
  await page.fill("[data-testid=offer-product-dialog-search]", adminFree.product.title);
  await page.keyboard.press("Enter");
  await page.waitForSelector("[data-testid=offer-product-dialog-option]", { timeout: 20000 });
  const productOption = page.locator("[data-testid=offer-product-dialog-option]").filter({ hasText: adminFree.product.title }).first();
  await productOption.click();
  result.metrics.productPickerMs = Date.now() - pickerStart;
  await page.waitForTimeout(900);
  await page.click("[data-testid=offer-variant-picker-toggle]");
  await page.waitForSelector("[data-testid=offer-variant-option]", { timeout: 20000 });
  const variantDialogText = await page.locator("[data-testid=offer-variant-dialog]").innerText();
  check("ui.adminCreate.variantHumanLabels", /:/.test(variantDialogText) && variantDialogText.includes(adminFree.variant.displayLabel), variantDialogText.slice(0, 200));
  await page.locator("[data-testid=offer-variant-option]").filter({ hasText: adminFree.variant.displayLabel }).first().click();
  await page.waitForTimeout(300);

  const adminSku = `TB296-R8-ADMIN-${Date.now()}`;
  await page.fill("[data-testid=offer-seller-sku]", adminSku);
  await page.click("[data-testid=admin-offer-submit]");
  await page.waitForURL(/\/admin\/offers\/[0-9a-f-]+$/, { timeout: 30000 });
  const adminOfferId = page.url().split("/").pop();
  result.ids.adminOfferId = adminOfferId;
  await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
  await page.waitForTimeout(500);
  check("ui.adminCreate.url", Boolean(adminOfferId), page.url());
  check("ui.adminCreate.tabsEnabled", !(await page.locator("[data-testid=offer-tab-button-pricing]").isDisabled()), "pricing tab enabled");
  const adminReread = await jsonApi(`${HOST}/v1/admin/offers/${adminOfferId}`, { headers: adminHeaders });
  check("ui.adminCreate.persisted", adminReread.status === 200 && adminReread.body?.sellerSku === adminSku && adminReread.body?.status === "Draft", JSON.stringify({ status: adminReread.status, sku: adminReread.body?.sellerSku, status: adminReread.body?.status }));
  async function clickSaveAndWait(pattern, responseMethod) {
    const response = page.waitForResponse((res) =>
      res.request().method() !== "GET"
      && res.url().includes(pattern)
      && res.url().includes(adminOfferId)
      && (!responseMethod || res.request().method() === responseMethod),
      { timeout: 20000 },
    ).catch(() => null);
    await page.click("[data-testid=admin-offer-save]");
    await response;
    await page.waitForTimeout(500);
  }

  // Price: Persian digits, save, empty never becomes zero.
  await page.click("[data-testid=offer-tab-button-pricing]");
  const priceInput = page.locator("[data-testid=offer-price-input]");
  await priceInput.fill("۱۲۳۴۵۶۷");
  await page.waitForTimeout(300);
  const displayedPrice = await priceInput.inputValue();
  check("ui.price.persianDigits", /[۰-۹]/.test(displayedPrice) && /[٬,]/.test(displayedPrice), displayedPrice);
  await clickSaveAndWait("/price");
  let priceRead = await jsonApi(`${HOST}/v1/admin/offers/${adminOfferId}`, { headers: adminHeaders });
  check("ui.price.persisted", priceRead.body?.amount === 1234567, String(priceRead.body?.amount));
  await priceInput.fill("");
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-save-error]", { timeout: 20000 });
  const emptyError = await page.locator("[data-testid=offer-save-error]").innerText();
  priceRead = await jsonApi(`${HOST}/v1/admin/offers/${adminOfferId}`, { headers: adminHeaders });
  check("ui.price.emptyNotZero", /مبلغ را وارد کنید/.test(emptyError) && priceRead.body?.amount === 1234567, `${emptyError} amount=${priceRead.body?.amount}`);
  await priceInput.fill("1234567");
  await page.waitForTimeout(250);

  // Conditions: return policy, custom window, min/max.
  await page.click("[data-testid=offer-tab-button-conditions]");
  await page.selectOption("[data-testid=offer-return-policy]", "Custom");
  await page.fill("[data-testid=offer-custom-return-days]", "9");
  await page.fill("[data-testid=offer-min-qty]", "2");
  await page.fill("[data-testid=offer-max-qty]", "6");
  await clickSaveAndWait("/offers/", "PATCH");
  const conditionsRead = await jsonApi(`${HOST}/v1/admin/offers/${adminOfferId}`, { headers: adminHeaders });
  check("ui.conditions.persisted", conditionsRead.body?.returnPolicyChoice === "Custom" && conditionsRead.body?.customReturnWindowDays === 9 && Number(conditionsRead.body?.minimumOrderQuantity) === 2 && Number(conditionsRead.body?.maximumOrderQuantity) === 6, JSON.stringify({ policy: conditionsRead.body?.returnPolicyChoice, days: conditionsRead.body?.customReturnWindowDays, min: conditionsRead.body?.minimumOrderQuantity, max: conditionsRead.body?.maximumOrderQuantity }));
  check("ui.buyability.displayHeuristic", /نمایشی/.test(await page.locator("[data-testid=offer-workspace-meta]").innerText()) || /نمایشی/.test(await page.locator("[data-testid=offer-tab-conditions]").innerText()), "display heuristic label");

  // Language navigation preserves unsaved SKU and switches direction.
  await page.click("[data-testid=offer-tab-button-identity]");
  const langSku = `TB296-R8-LANG-${Date.now()}`;
  await page.fill("[data-testid=offer-seller-sku]", langSku);
  await page.waitForTimeout(400);
  check("ui.language.dirty", (await page.locator("[data-testid=offer-workspace-dirty]").count()) > 0, "dirty before language switch");
  await page.goto(`${FRONT}/en/admin/offers/${adminOfferId}`, { waitUntil: "load" });
  await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
  await page.waitForTimeout(700);
  const enValue = await page.locator("[data-testid=offer-seller-sku]").inputValue();
  const enDir = await page.locator("[data-testid=offer-workspace]").getAttribute("dir");
  const enTabs = await page.locator("[role=tab]").allInnerTexts();
  check("ui.language.restoresDraft", enValue === langSku, enValue);
  check("ui.language.direction", enDir === "ltr", String(enDir));
  check("ui.language.copy", enTabs.some((text) => /Offer identity/i.test(text)), enTabs.join("|"));
  await page.screenshot({ path: path.join(OUT, "admin-edit-en-1440.png"), fullPage: true });
  await page.goto(`${FRONT}/fa/admin/offers/${adminOfferId}`, { waitUntil: "load" });
  await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
  await page.waitForTimeout(700);
  check("ui.language.restoresBack", (await page.locator("[data-testid=offer-seller-sku]").inputValue()) === langSku, "fa restore");
  await clickSaveAndWait("/offers/", "PATCH");
  const langRead = await jsonApi(`${HOST}/v1/admin/offers/${adminOfferId}`, { headers: adminHeaders });
  check("ui.language.draftSaved", langRead.body?.sellerSku === langSku, String(langRead.body?.sellerSku));

  // Inventory: real save + independent restore on a seeded offer with stock. New drafts may have no stock position.
  const seedOfferId = "457e375f-ceec-4b66-b81e-3829c4e15893";
  result.ids.seedOfferId = seedOfferId;
  let inventoryTarget = adminOfferId;
  let beforeInventory = await jsonApi(`${HOST}/v1/admin/offers/${inventoryTarget}`, { headers: adminHeaders });
  if (!Array.isArray(beforeInventory.body?.inventoryLocations) || beforeInventory.body.inventoryLocations.length === 0) {
    inventoryTarget = seedOfferId;
    await page.goto(`${FRONT}/fa/admin/offers/${seedOfferId}`, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    await page.waitForTimeout(500);
    beforeInventory = await jsonApi(`${HOST}/v1/admin/offers/${inventoryTarget}`, { headers: adminHeaders });
  }
  await page.click("[data-testid=offer-tab-button-inventory]");
  let inventoryRows = page.locator("[data-testid=offer-inventory-row]");
  let locationCount = await inventoryRows.count();
  result.metrics.inventoryLocations = locationCount;
  check("ui.inventory.hasLocation", locationCount > 0, String(locationCount));
  const firstStock = beforeInventory.body.inventoryLocations[0];
  const originalOnHand = Number(firstStock.onHand);
  const targetOnHand = originalOnHand + 1;
  await inventoryRows.first().locator("[data-testid=offer-inventory-input]").fill(String(targetOnHand));
  await page.fill("[data-testid=offer-inventory-reason]", "شمارش R8");
  await clickSaveAndWait("/inventory");
  const inventoryRead = await jsonApi(`${HOST}/v1/admin/offers/${inventoryTarget}`, { headers: adminHeaders });
  check("ui.inventory.persisted", Number(inventoryRead.body?.inventoryLocations?.[0]?.onHand) === targetOnHand, JSON.stringify({ expected: targetOnHand, actual: inventoryRead.body?.inventoryLocations?.[0]?.onHand }));
  inventoryRows = page.locator("[data-testid=offer-inventory-row]");
  await inventoryRows.first().locator("[data-testid=offer-inventory-input]").fill(String(originalOnHand));
  await page.fill("[data-testid=offer-inventory-reason]", "بازگردانی R8");
  await clickSaveAndWait("/inventory");
  const restoredRead = await jsonApi(`${HOST}/v1/admin/offers/${inventoryTarget}`, { headers: adminHeaders });
  check("ui.inventory.restored", Number(restoredRead.body?.inventoryLocations?.[0]?.onHand) === originalOnHand, JSON.stringify({ expected: originalOnHand, actual: restoredRead.body?.inventoryLocations?.[0]?.onHand }));

  // Disconnect during price save -> uncertain, no retry, reload offered and draft kept.
  await page.click("[data-testid=offer-tab-button-pricing]");
  const disconnectPrice = 7654321;
  await priceInput.fill(String(disconnectPrice));
  await page.route("**/v1/admin/offers/*/price", (route) => route.abort("failed"), { times: 1 });
  await page.click("[data-testid=admin-offer-save]");
  await page.waitForSelector("[data-testid=offer-save-error]", { timeout: 20000 });
  const networkText = await page.locator("[data-testid=offer-save-error]").innerText();
  check("ui.network.uncertain", /نتیجه ذخیره قطعی نیست/.test(networkText), networkText);
  check("ui.network.noBlindRetry", (await page.locator("[data-testid=offer-save-retry]").count()) === 0, "no retry");
  check("ui.network.reloadAction", (await page.locator("[data-testid=offer-save-reload]").count()) > 0, "reload action");
  const keptPrice = await priceInput.inputValue();
  check("ui.network.draftKept", keptPrice.replace(/[^0-9۰-۹]/g, "").replace(/[۰-۹]/g, (d) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(d))) === String(disconnectPrice), keptPrice);
  await page.unroute("**/v1/admin/offers/*/price");
  await page.click("[data-testid=offer-save-reload]");
  await page.waitForTimeout(1000);

  // Responsive overflow and screenshots.
  for (const width of [360, 768, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.waitForTimeout(350);
    const overflow = await page.evaluate(() => ({
      document: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      body: document.body.scrollWidth - document.body.clientWidth,
    }));
    check(`ui.overflow.${width}`, overflow.document <= 0, JSON.stringify(overflow));
    await page.screenshot({ path: path.join(OUT, `admin-edit-${width}.png`), fullPage: true });
  }
  await page.setViewportSize({ width: 1440, height: 1000 });

  check("ui.console.noErrors", consoleErrors.filter((text) => !text.includes("ERR_FAILED")).length === 0, consoleErrors.filter((text) => !text.includes("ERR_FAILED")).slice(0, 5).join(" | "));
  await adminContext.close();

  // ---------- Seller duplicate + successful create ----------
  const sellerAContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await sellerAContext.addInitScript(({ seller }) => {
    localStorage.setItem("tooba.sellerActorUserId", seller.actorUserId);
    localStorage.setItem("tooba.sellerPartyId", seller.sellerPartyId);
  }, { seller: sellerA });
  const sellerPage = await sellerAContext.newPage();
  await sellerPage.goto(`${FRONT}/vendor-panel/products/new`, { waitUntil: "load" });
  await sellerPage.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
  await sellerPage.click("[data-testid=offer-product-picker-toggle]");
  await sellerPage.fill("[data-testid=offer-product-dialog-search]", "سامسونگ هدفون");
  await sellerPage.keyboard.press("Enter");
  await sellerPage.waitForSelector("[data-testid=offer-product-dialog-option]", { timeout: 20000 });
  await sellerPage.locator("[data-testid=offer-product-dialog-option]").filter({ hasText: "سامسونگ هدفون" }).first().click();
  await sellerPage.waitForTimeout(900);
  await sellerPage.click("[data-testid=offer-variant-picker-toggle]");
  await sellerPage.waitForSelector("[data-testid=offer-variant-option]", { timeout: 20000 });
  await sellerPage.locator("[data-testid=offer-variant-option]").filter({ hasText: "آبی" }).first().click();
  await sellerPage.waitForSelector("[data-testid=offer-duplicate-conflict]", { timeout: 20000 });
  const conflictText = await sellerPage.locator("[data-testid=offer-duplicate-conflict]").innerText();
  const conflictHref = await sellerPage.locator("[data-testid=offer-duplicate-conflict] a").getAttribute("href");
  check("ui.sellerDuplicate.explained", /آفر فعال/.test(conflictText), conflictText);
  check("ui.sellerDuplicate.link", /\/(vendor-panel\/products|admin\/offers)\//.test(conflictHref ?? ""), String(conflictHref));
  check("ui.sellerDuplicate.submitDisabled", await sellerPage.locator("[data-testid=seller-create-submit]").isDisabled(), "submit disabled");
  await sellerPage.screenshot({ path: path.join(OUT, "seller-duplicate-1440.png"), fullPage: true });
  await sellerAContext.close();

  const sellerBContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await sellerBContext.addInitScript(({ seller }) => {
    localStorage.setItem("tooba.sellerActorUserId", seller.actorUserId);
    localStorage.setItem("tooba.sellerPartyId", seller.sellerPartyId);
  }, { seller: sellerB });
  const sellerBPage = await sellerBContext.newPage();
  await sellerBPage.goto(`${FRONT}/vendor-panel/products/new`, { waitUntil: "load" });
  await sellerBPage.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
  await sellerBPage.click("[data-testid=offer-product-picker-toggle]");
  await sellerBPage.fill("[data-testid=offer-product-dialog-search]", sellerFree.product.title);
  await sellerBPage.keyboard.press("Enter");
  await sellerBPage.waitForSelector("[data-testid=offer-product-dialog-option]", { timeout: 20000 });
  await sellerBPage.locator("[data-testid=offer-product-dialog-option]").filter({ hasText: sellerFree.product.title }).first().click();
  await sellerBPage.waitForTimeout(900);
  await sellerBPage.click("[data-testid=offer-variant-picker-toggle]");
  await sellerBPage.waitForSelector("[data-testid=offer-variant-option]", { timeout: 20000 });
  await sellerBPage.locator("[data-testid=offer-variant-option]").filter({ hasText: sellerFree.variant.displayLabel }).first().click();
  const sellerSku = `TB296-R8-SELLER-${Date.now()}`;
  await sellerBPage.fill("[data-testid=offer-seller-sku]", sellerSku);
  await sellerBPage.click("[data-testid=seller-create-submit]");
  await sellerBPage.waitForURL(/\/vendor-panel\/products\/[0-9a-f-]+$/, { timeout: 30000 });
  const sellerOfferId = sellerBPage.url().split("/").pop();
  result.ids.sellerOfferId = sellerOfferId;
  await sellerBPage.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
  check("ui.sellerCreate.tabsEnabled", !(await sellerBPage.locator("[data-testid=offer-tab-button-pricing]").isDisabled()), "pricing tab enabled");
  const sellerReread = await jsonApi(`${HOST}/v1/seller/offers/${sellerOfferId}`, { headers: sellerBHeaders });
  check("ui.sellerCreate.persisted", sellerReread.status === 200 && sellerReread.body?.sellerSku === sellerSku && sellerReread.body?.sellerPartyId === sellerB.sellerPartyId, JSON.stringify({ status: sellerReread.status, sku: sellerReread.body?.sellerSku, seller: sellerReread.body?.sellerPartyId }));
  await sellerBPage.click("[data-testid=offer-tab-button-pricing]");
  await sellerBPage.fill("[data-testid=offer-price-input]", "۹۸۷۶۵۴");
  await sellerBPage.click("[data-testid=offer-save]");
  await sellerBPage.waitForSelector("[data-testid=offer-save-notice]", { timeout: 20000 });
  const sellerPriceRead = await jsonApi(`${HOST}/v1/seller/offers/${sellerOfferId}`, { headers: sellerBHeaders });
  check("ui.sellerCreate.pricePersisted", sellerPriceRead.body?.amount === 987654, String(sellerPriceRead.body?.amount));
  await sellerBPage.screenshot({ path: path.join(OUT, "seller-edit-1440.png"), fullPage: true });
  await sellerBContext.close();

  // ---------- Lists: search, sort, page, export, channel label ----------
  const listContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await listContext.addInitScript(({ adminActor }) => localStorage.setItem("tooba.adminActorUserId", adminActor), { adminActor });
  const listPage = await listContext.newPage();
  const listRequests = [];
  listPage.on("request", (request) => {
    if (request.url().includes("/v1/admin/offers/query")) listRequests.push(request.postDataJSON());
  });
  await listPage.goto(`${FRONT}/admin/offers`, { waitUntil: "load" });
  await listPage.waitForSelector("[data-app-grid-shell]", { timeout: 30000 });
  await listPage.waitForTimeout(1000);
  check("ui.list.channelReal", /بازارگاه/.test(await listPage.locator("[data-app-grid-shell]").innerText()), "channel label");
  await listPage.fill("[data-app-grid-search]", langSku);
  await listPage.keyboard.press("Enter");
  await listPage.waitForTimeout(1200);
  const searchRequest = listRequests.find((body) => body?.search === langSku);
  check("ui.list.search", Boolean(searchRequest), JSON.stringify(searchRequest ?? null));
  const searchBody = await listPage.locator("[data-app-grid-shell]").innerText();
  check("ui.list.searchRow", searchBody.includes(langSku), "created SKU visible");

  await listPage.fill("[data-app-grid-search]", "");
  await listPage.keyboard.press("Enter");
  await listPage.waitForTimeout(800);
  const skuHeader = listPage.locator('.ag-header-cell[col-id="sellerSku"]');
  await skuHeader.click();
  await listPage.waitForTimeout(1000);
  const sortRequest = listRequests.filter((body) => body?.sort?.some((s) => s.field === "sellerSku" || s.columnId === "sellerSku")).pop();
  check("ui.list.sort", Boolean(sortRequest), JSON.stringify(sortRequest ?? null));

  const nextButton = listPage.locator("[data-app-grid-page-btn]").filter({ hasText: /بعدی|Next/i }).first();
  if (await nextButton.count()) {
    await nextButton.click();
    await listPage.waitForTimeout(1000);
    const pageRequest = listRequests.filter((body) => body?.page === 2).pop();
    check("ui.list.page", Boolean(pageRequest), JSON.stringify(pageRequest ?? null));
  }
  const downloadPromise = listPage.waitForEvent("download", { timeout: 15000 }).catch(() => null);
  await listPage.click("[data-testid=app-grid-export-csv]");
  const download = await downloadPromise;
  check("ui.list.export", Boolean(download), download ? await download.suggestedFilename() : "no download");
  await listPage.screenshot({ path: path.join(OUT, "admin-list-1440.png"), fullPage: true });
  await listContext.close();

  // ---------- Seller list search ----------
  const sellerListContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
  await sellerListContext.addInitScript(({ seller }) => {
    localStorage.setItem("tooba.sellerActorUserId", seller.actorUserId);
    localStorage.setItem("tooba.sellerPartyId", seller.sellerPartyId);
  }, { seller: sellerB });
  const sellerListPage = await sellerListContext.newPage();
  await sellerListPage.goto(`${FRONT}/vendor-panel/products`, { waitUntil: "load" });
  await sellerListPage.waitForSelector("[data-app-grid-shell]", { timeout: 30000 });
  await sellerListPage.waitForTimeout(1000);
  await sellerListPage.fill("[data-app-grid-search]", sellerSku);
  await sellerListPage.keyboard.press("Enter");
  await sellerListPage.waitForTimeout(1200);
  check("ui.sellerList.search", (await sellerListPage.locator("[data-app-grid-shell]").innerText()).includes(sellerSku), "seller list search");
  await sellerListContext.close();

  await archiveR8TestOffers();
  await cleanupSellerFixture();
  await browser.close();
  result.finishedAt = new Date().toISOString();
  result.ok = result.errors.length === 0;
  fs.writeFileSync(path.join(OUT, "verify-r8.json"), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ ok: result.ok, checks: Object.keys(result.checks).length, errors: result.errors, ids: result.ids, metrics: result.metrics }, null, 2));
  process.exit(result.ok ? 0 : 1);
}

main().catch((error) => {
  console.error(error);
  process.exit(2);
});
