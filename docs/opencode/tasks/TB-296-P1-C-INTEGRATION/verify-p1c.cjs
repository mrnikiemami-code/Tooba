// ARCHITECT SAFETY QUARANTINE — historical runner preserved at ac192d333d3642c729d58339422ecc5a193ce309.
// Do not remove this gate. A new test-only runner must use isolated fixtures and exact
// before/after permission and stock restoration, not the destructive legacy cleanup.
throw new Error("BLOCKED_UNSAFE_LEGACY_HARNESS: use ARCHITECT-CONTINUATION.md; do not execute historical verify-r10.cjs or this runner");
const { chromium } = require("playwright");
const fs = require("fs");
const path = require("path");

const FRONT = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = process.argv[2] || path.join(__dirname, "r10-runtime");
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER_HEADER = "X-Tooba-Seller-Party-Id";

async function api(url, init) {
  const response = await fetch(url, init);
  const text = await response.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { status: response.status, body };
}

async function main() {
  fs.mkdirSync(OUT, { recursive: true });
  const result = { startedAt: new Date().toISOString(), checks: {}, errors: [], metrics: {}, ids: {} };
  const check = (name, ok, detail = "") => {
    result.checks[name] = { ok: Boolean(ok), detail };
    if (!ok) result.errors.push(`${name}: ${detail}`);
  };

  const providerSource = fs.readFileSync(path.resolve(process.cwd(), "src/frontend/app/providers.tsx"), "utf8");
  check("R9-02.sharedToastAutoClose3000", /autoClose=\{3000\}/.test(providerSource), "providers autoClose");
  const dev = await api(`${HOST}/v1/admin/dev-context`);
  const adminActor = dev.body.actorUserId;
  const contexts = await api(`${HOST}/v1/seller/dev-contexts`);
  const sellerA = contexts.body.actors[0];
  const sellerB = contexts.body.actors[1];
  const adminHeaders = { [ADMIN_HEADER]: adminActor, "Accept-Language": "fa-IR", "Content-Type": "application/json" };
  const sellerAHeaders = { [ADMIN_HEADER]: sellerA.actorUserId, [SELLER_HEADER]: sellerA.sellerPartyId, "Accept-Language": "fa-IR", "Content-Type": "application/json" };
  const sellerBHeaders = { [ADMIN_HEADER]: sellerB.actorUserId, [SELLER_HEADER]: sellerB.sellerPartyId, "Accept-Language": "fa-IR", "Content-Type": "application/json" };

  // Registry and data contracts.
  const languages = await api(`${HOST}/v1/languages`);
  const ru = (Array.isArray(languages.body) ? languages.body : []).find((row) => row.code === "ru-RU");
  check("R9-07.publicLanguageRegistry", languages.status === 200 && Boolean(ru?.isActive), JSON.stringify({ status: languages.status, ru }));
  const products = await api(`${HOST}/v1/admin/catalog/products?page=1&pageSize=100&locale=fa-IR`, { headers: adminHeaders });
  const unitProduct = (products.body.items ?? []).find((row) => row.unitCode && row.publishedVariantCount > 0);
  check("R9-06.unitMetadataRead", Boolean(unitProduct) && Number(unitProduct.quantityDecimalPlaces) >= 0, JSON.stringify(unitProduct));
  const seedId = "457e375f-ceec-4b66-b81e-3829c4e15893";
  const seed = await api(`${HOST}/v1/admin/offers/${seedId}`, { headers: adminHeaders });
  check("R9-03.humanWarehouseName", seed.status === 200 && seed.body.inventoryLocations?.[0]?.locationName, JSON.stringify(seed.body.inventoryLocations?.[0]));
  const twoId = "01a0ba10-5a31-7000-b733-4cd74ef3763a";
  const two = await api(`${HOST}/v1/admin/offers/${twoId}`, { headers: adminHeaders });
  const baselines = [
    { offerId: seedId, locations: seed.body?.inventoryLocations ?? [] },
    { offerId: twoId, locations: two.body?.inventoryLocations ?? [] },
  ];
  check("R9-10.twoRealPositions", two.status === 200 && two.body.inventoryLocations?.length >= 2, JSON.stringify(two.body.inventoryLocations?.map((row) => ({ id: row.stockItemId, name: row.locationName, onHand: row.onHand }))));

  async function findFree(sellerId, excludeProductId) {
    for (const product of products.body.items) {
      if (excludeProductId && product.productId === excludeProductId) continue;
      const variants = await api(`${HOST}/v1/admin/catalog/products/${product.productId}/variants?locale=fa-IR`, { headers: adminHeaders });
      for (const variant of variants.body ?? []) {
        const active = await api(`${HOST}/v1/admin/offers/active-listing?sellerPartyId=${sellerId}&catalogVariantId=${variant.catalogVariantId}`, { headers: adminHeaders });
        if (active.status === 200 && active.body == null) return { product, variant };
      }
    }
    return null;
  }
  const adminFree = await findFree(sellerB.sellerPartyId);
  const sellerFree = adminFree ? await findFree(sellerB.sellerPartyId, adminFree.product.productId) : null;
  check("R9-11.isolatedFreeFixtures", Boolean(adminFree) && Boolean(sellerFree), JSON.stringify({ admin: adminFree?.product?.title, seller: sellerFree?.product?.title }));

  const fixtureBase = `${HOST}/v1/admin/sellers/${sellerB.sellerPartyId}/access-control`;
  const fixture = { roleId: null, assignmentId: null };
  async function grantSellerB() {
    const ceiling = await api(`${fixtureBase}/ceiling`, { headers: adminHeaders });
    const entries = (ceiling.body ?? []).map((row) => ({
      permissionId: row.permissionId,
      enabled: ["product.create", "product.view", "product.edit"].includes(row.permissionId),
      scopeKind: row.scopeKind,
      scopeResourceId: row.scopeResourceId ?? null,
    }));
    await api(`${fixtureBase}/ceiling`, { method: "PUT", headers: adminHeaders, body: JSON.stringify({ entries }) });
    const role = await api(`${fixtureBase}/roles`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ name: `R9 Fixture ${Date.now()}`, code: `r9-fixture-${Date.now()}`, description: "temporary R9 fixture" }) });
    fixture.roleId = role.body?.id;
    if (!fixture.roleId) throw new Error(`role failed ${role.status}`);
    await api(`${fixtureBase}/roles/${fixture.roleId}/permissions`, {
      method: "PUT",
      headers: adminHeaders,
      body: JSON.stringify([
        { permissionId: "product.create", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
        { permissionId: "product.view", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
        { permissionId: "product.edit", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
      ]),
    });
    const assign = await api(`${fixtureBase}/assignments`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ userId: sellerB.actorUserId, roleId: fixture.roleId }) });
    fixture.assignmentId = assign.body?.id;
    if (!fixture.assignmentId) throw new Error(`assignment failed ${assign.status}`);
  }
  async function cleanup() {
    try {
      if (fixture.assignmentId) await api(`${fixtureBase}/assignments/${fixture.assignmentId}`, { method: "DELETE", headers: adminHeaders });
      if (fixture.roleId) await api(`${fixtureBase}/roles/${fixture.roleId}`, { method: "DELETE", headers: adminHeaders });
      const ceiling = await api(`${fixtureBase}/ceiling`, { headers: adminHeaders });
      const entries = (ceiling.body ?? []).map((row) => ({ permissionId: row.permissionId, enabled: false, scopeKind: row.scopeKind, scopeResourceId: row.scopeResourceId ?? null }));
      await api(`${fixtureBase}/ceiling`, { method: "PUT", headers: adminHeaders, body: JSON.stringify({ entries }) });
      const r9 = await api(`${HOST}/v1/admin/offers/query`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ page: 1, pageSize: 100, filters: [], sorts: [], search: "TB296-R9-" }) });
      for (const item of r9.body?.items ?? []) {
        await api(`${HOST}/v1/admin/offers/${item.offerId}`, { method: "PATCH", headers: adminHeaders, body: JSON.stringify({ sellerSku: item.sellerSku, status: "Archived", returnPolicyChoice: "Default", minimumOrderQuantity: 1, maximumOrderQuantity: 20 }) });
      }
      const verify = await api(`${HOST}/v1/admin/offers/query`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ page: 1, pageSize: 10, filters: [], sorts: [], search: "TB296-R9-" }) });
      const roles = await api(`${fixtureBase}/roles`, { headers: adminHeaders });
      const assigns = await api(`${fixtureBase}/assignments`, { headers: adminHeaders });
      const ceilingAfter = await api(`${fixtureBase}/ceiling`, { headers: adminHeaders });
      const restoredBaselines = [];
      for (const baseline of baselines) {
        const detail = await api(`${HOST}/v1/admin/offers/${baseline.offerId}`, { headers: adminHeaders });
        restoredBaselines.push({
          offerId: baseline.offerId,
          ok: baseline.locations.every((location) => Number(detail.body?.inventoryLocations?.find((row) => row.stockItemId === location.stockItemId)?.onHand) === Number(location.onHand)),
        });
      }
      const cleanupResult = {
        ok: (verify.body?.items ?? []).length === 0 && (roles.body ?? []).length === 0 && (assigns.body ?? []).length === 0 && !(ceilingAfter.body ?? []).some((row) => row.enabled) && restoredBaselines.every((row) => row.ok),
        r9Remaining: verify.body?.totalCount ?? null,
        roles: roles.body,
        assignments: assigns.body,
        ceilingEnabled: (ceilingAfter.body ?? []).filter((row) => row.enabled).length,
        baselines: restoredBaselines,
      };
      fs.writeFileSync(path.join(OUT, "cleanup.json"), JSON.stringify(cleanupResult, null, 2));
      check("R9-11.fixtureRestored", cleanupResult.ok && (roles.body ?? []).length === 0 && (assigns.body ?? []).length === 0 && !(ceilingAfter.body ?? []).some((row) => row.enabled), JSON.stringify({ r9: verify.body?.totalCount, roles: roles.body, assigns: assigns.body }));
    } catch (error) {
      check("R9-11.fixtureRestored", false, String(error));
    }
  }

  await grantSellerB();
  const browser = await chromium.launch();
  try {
    const adminContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
    await adminContext.addInitScript(({ adminActor }) => localStorage.setItem("tooba.adminActorUserId", adminActor), { adminActor });
    const page = await adminContext.newPage();
    const consoleErrors = [];
    page.on("console", (msg) => { if (msg.type() === "error") consoleErrors.push(msg.text()); });
    page.on("pageerror", (error) => consoleErrors.push(String(error)));

    // Pre-create identity and tabs.
    const formStart = Date.now();
    await page.goto(`${FRONT}/admin/offers/new`, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    const createStartUrl = page.url();
    result.metrics.formLoadMs = Date.now() - formStart;
    await page.click("[data-testid=offer-seller-picker-toggle]");
    await page.fill("[data-testid=offer-seller-dialog-search]", "دیجی");
    await page.keyboard.press("Enter");
    await page.waitForTimeout(700);
    await page.locator("[data-testid=offer-seller-dialog-option]").filter({ hasText: "دیجی" }).first().click();
    await page.click("[data-testid=offer-product-picker-toggle]");
    await page.fill("[data-testid=offer-product-dialog-search]", adminFree.product.title);
    await page.keyboard.press("Enter");
    await page.waitForSelector("[data-testid=offer-product-dialog-option]", { timeout: 20000 });
    await page.locator("[data-testid=offer-product-dialog-option]").filter({ hasText: adminFree.product.title }).first().click();
    await page.waitForTimeout(700);
    await page.click("[data-testid=offer-variant-picker-toggle]");
    await page.waitForSelector("[data-testid=offer-variant-option]", { timeout: 20000 });
    await page.locator("[data-testid=offer-variant-option]").filter({ hasText: adminFree.variant.displayLabel }).first().click();
    await page.waitForTimeout(300);
    const pricingEnabled = !(await page.locator("[data-testid=offer-tab-button-pricing]").isDisabled());
    const conditionsEnabled = !(await page.locator("[data-testid=offer-tab-button-conditions]").isDisabled());
    const inventoryDisabled = await page.locator("[data-testid=offer-tab-button-inventory]").isDisabled();
    const historyDisabled = await page.locator("[data-testid=offer-tab-button-history]").isDisabled();
    const channelSelect = page.locator("[data-testid=offer-channel-select]");
    check("R10-01.channelSelect", (await channelSelect.count()) === 1 && (await channelSelect.inputValue()) === "Marketplace", await channelSelect.inputValue().catch(() => "missing"));
    const allowed = await api(`${HOST}/v1/admin/offers/allowed-channels`, { headers: adminHeaders });
    check("R10-01.allowedChannels", allowed.status === 200 && Array.isArray(allowed.body) && allowed.body.includes("Marketplace"), JSON.stringify(allowed.body));
    const rejected = await api(`${HOST}/v1/admin/offers`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ sellerPartyId: sellerB.sellerPartyId, catalogVariantId: adminFree.variant.catalogVariantId, channel: "Direct", requestId: `r10-bad-channel-${Date.now()}` }) });
    check("R10-01.rejectUnallowedChannel", rejected.status === 400 || rejected.status === 409 || rejected.status === 403, String(rejected.status));
    check("R10-02.preCreateTabs", pricingEnabled && conditionsEnabled && !inventoryDisabled && historyDisabled, JSON.stringify({ pricingEnabled, conditionsEnabled, inventoryEnabled: !inventoryDisabled, historyDisabled }));

    // Pre-create price + conditions + actual inventory position then one explicit save.
    await page.click("[data-testid=offer-tab-button-inventory]");
    await page.waitForSelector("[data-testid=offer-inventory-row]", { timeout: 20000 });
    await page.locator("[data-testid=offer-inventory-row]").first().locator("[data-testid=offer-inventory-input]").fill("3");
    await page.fill("[data-testid=offer-inventory-reason]", "R10 create position");
    await page.click("[data-testid=offer-tab-button-pricing]");
    await page.fill("[data-testid=offer-price-input]", "۸۷۶۵۴۳");
    await page.waitForTimeout(250);
    await page.click("[data-testid=offer-tab-button-conditions]");
    await page.selectOption("[data-testid=offer-return-policy]", "Custom");
    await page.fill("[data-testid=offer-custom-return-days]", "9");
    await page.fill("[data-testid=offer-min-qty]", "2");
    await page.fill("[data-testid=offer-max-qty]", "6");
    const r9Sku = `TB296-R9-ADMIN-${Date.now()}`;
    await page.click("[data-testid=offer-tab-button-identity]");
    await page.fill("[data-testid=offer-seller-sku]", r9Sku);
    await page.click("[data-testid=admin-offer-submit]");
    await page.waitForURL(/\/admin\/offers\/[0-9a-f-]+$/, { timeout: 30000 });
    const r9OfferId = page.url().split("/").pop();
    result.ids.r9OfferId = r9OfferId;
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    await page.waitForTimeout(700);
    const r9Read = await api(`${HOST}/v1/admin/offers/${r9OfferId}`, { headers: adminHeaders });
    check("R10-03.createProvisionedInventory", Array.isArray(r9Read.body?.inventoryLocations) && r9Read.body.inventoryLocations.some((row) => Number(row.onHand) === 3), JSON.stringify(r9Read.body?.inventoryLocations));
    check("R10-04.createOneDraftApplied", r9Read.body?.amount === 876543 && r9Read.body?.returnPolicyChoice === "Custom" && r9Read.body?.customReturnWindowDays === 9 && Number(r9Read.body?.minimumOrderQuantity) === 2 && Number(r9Read.body?.maximumOrderQuantity) === 6, JSON.stringify({ amount: r9Read.body?.amount, policy: r9Read.body?.returnPolicyChoice, days: r9Read.body?.customReturnWindowDays, min: r9Read.body?.minimumOrderQuantity, max: r9Read.body?.maximumOrderQuantity }));

    // Toast 3s and no persistent success banner.
    await page.click("[data-testid=offer-tab-button-pricing]");
    await page.fill("[data-testid=offer-price-input]", "۸۷۶۵۴۴");
    await page.click("[data-testid=admin-offer-save]");
    await page.waitForSelector(".Toastify__toast", { timeout: 15000 }).catch(() => null);
    check("R9-02.noPersistentSuccessBanner", (await page.locator("[data-testid=offer-save-notice]").count()) === 0, "no success banner");
    await page.waitForTimeout(4800);
    check("R9-02.toastAutoClose3s", (await page.locator(".Toastify__toast").count()) === 0, "toast gone");

    // Inventory blank reason then correction (seed 199 -> 50 -> restore 199).
    await page.goto(`${FRONT}/admin/offers/${seedId}`, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    const originalInventory = (await api(`${HOST}/v1/admin/offers/${seedId}`, { headers: adminHeaders })).body.inventoryLocations[0];
    await page.click("[data-testid=offer-tab-button-inventory]");
    await page.fill("[data-testid=offer-inventory-input]", "50");
    await page.fill("[data-testid=offer-inventory-reason]", "");
    await page.click("[data-testid=admin-offer-save]");
    await page.waitForSelector("[data-testid=offer-save-error]", { timeout: 15000 });
    check("R9-03.blankReasonRejected", /دلیل/.test(await page.locator("[data-testid=offer-save-error]").innerText()), "blank reason error");
    await page.fill("[data-testid=offer-inventory-reason]", "شمارش R9");
    await page.click("[data-testid=admin-offer-save]");
    await page.waitForSelector(".Toastify__toast", { timeout: 15000 }).catch(() => null);
    const afterFifty = (await api(`${HOST}/v1/admin/offers/${seedId}`, { headers: adminHeaders })).body.inventoryLocations[0];
    check("R9-03.validReasonCorrectionSaves", Number(afterFifty.onHand) === 50, JSON.stringify({ expected: 50, actual: afterFifty.onHand }));
    await page.fill("[data-testid=offer-inventory-input]", String(originalInventory.onHand));
    await page.fill("[data-testid=offer-inventory-reason]", "بازگردانی R9");
    await page.click("[data-testid=admin-offer-save]");
    await page.waitForTimeout(1200);
    const restored = (await api(`${HOST}/v1/admin/offers/${seedId}`, { headers: adminHeaders })).body.inventoryLocations[0];
    check("R9-03.seedRestored", Number(restored.onHand) === Number(originalInventory.onHand), JSON.stringify({ expected: originalInventory.onHand, actual: restored.onHand }));

    // Two real positions: abort second write, preserve second draft/reason, retry remainder, restore.
    await page.goto(`${FRONT}/fa/admin/offers/${twoId}`, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    const beforeTwo = (await api(`${HOST}/v1/admin/offers/${twoId}`, { headers: adminHeaders })).body.inventoryLocations;
    await page.click("[data-testid=offer-tab-button-inventory]");
    const twoRows = page.locator("[data-testid=offer-inventory-row]");
    await twoRows.nth(0).locator("[data-testid=offer-inventory-input]").fill("10");
    await twoRows.nth(1).locator("[data-testid=offer-inventory-input]").fill("20");
    await page.fill("[data-testid=offer-inventory-reason]", "آزمون دو موقعیت");
    let inventoryCalls = 0;
    await page.route(`**/v1/admin/offers/${twoId}/inventory`, async (route) => {
      inventoryCalls += 1;
      if (inventoryCalls === 2) return route.abort("failed");
      return route.continue();
    });
    await page.click("[data-testid=admin-offer-save]");
    await page.waitForSelector("[data-testid=offer-message-reload]", { timeout: 20000 });
    check("R9-10.secondWriteFailedMessage", true, "MessageBox");
    await page.locator("[data-testid=offer-message-reload]").getByRole("button").first().click();
    await page.waitForTimeout(600);
    const afterPartial = await api(`${HOST}/v1/admin/offers/${twoId}`, { headers: adminHeaders });
    const freshRows = page.locator("[data-testid=offer-inventory-row]");
    check("R9-10.firstSavedSecondDraftKept", Number(afterPartial.body.inventoryLocations[0].onHand) === 10 && (await freshRows.nth(1).locator("[data-testid=offer-inventory-input]").inputValue()) === "20" && /آزمون دو موقعیت/.test(await page.locator("[data-testid=offer-inventory-reason]").inputValue()), JSON.stringify({ first: afterPartial.body.inventoryLocations[0].onHand, secondDraft: await freshRows.nth(1).locator("[data-testid=offer-inventory-input]").inputValue() }));
    await page.unroute(`**/v1/admin/offers/${twoId}/inventory`);
    await page.click("[data-testid=admin-offer-save]");
    await page.waitForTimeout(1200);
    const afterRetry = await api(`${HOST}/v1/admin/offers/${twoId}`, { headers: adminHeaders });
    check("R9-10.retryRemainderOnly", Number(afterRetry.body.inventoryLocations[0].onHand) === 10 && Number(afterRetry.body.inventoryLocations[1].onHand) === 20, JSON.stringify(afterRetry.body.inventoryLocations.map((row) => row.onHand)));
    await page.fill("[data-testid=offer-inventory-reason]", "بازگردانی R9");
    await freshRows.nth(0).locator("[data-testid=offer-inventory-input]").fill(String(beforeTwo[0].onHand));
    await freshRows.nth(1).locator("[data-testid=offer-inventory-input]").fill(String(beforeTwo[1].onHand));
    await page.click("[data-testid=admin-offer-save]");
    await page.waitForTimeout(1200);
    const restoredTwo = (await api(`${HOST}/v1/admin/offers/${twoId}`, { headers: adminHeaders })).body.inventoryLocations;
    check("R9-10.twoPositionsRestored", Number(restoredTwo[0].onHand) === Number(beforeTwo[0].onHand) && Number(restoredTwo[1].onHand) === Number(beforeTwo[1].onHand), JSON.stringify(restoredTwo.map((row) => row.onHand)));

    // Dirty cancel uses MessageBox and clears the discardable draft.
    await page.goto(`${FRONT}/admin/offers/${seedId}`, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    await page.fill("[data-testid=offer-seller-sku]", `TB296-R9-DIRTY-${Date.now()}`);
    await page.click("[data-testid=offer-cancel]");
    await page.waitForSelector("[data-testid=offer-message-confirm]", { timeout: 10000 });
    check("R9-08.dirtyExitMessageBox", true, "MessageBox");
    await page.getByRole("button", { name: "بازگشت به فرم" }).click();
    check("R9-08.dirtyCancelKeepsForm", (await page.locator("[data-testid=offer-workspace]").count()) === 1, "still on form");

    // Actual third-locale route.
    await page.goto(`${FRONT}/ru/admin/offers/${seedId}`, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    await page.waitForTimeout(1200);
    const ruRoot = page.locator("[data-testid=offer-workspace]");
    check("R9-07.actualThirdLocaleRoute", (await ruRoot.getAttribute("dir")) === "ltr" && (await ruRoot.getAttribute("lang")) === "ru-RU" && /Идентификация/.test(await page.locator("[data-testid=offer-tab-button-identity]").innerText()), JSON.stringify({ dir: await ruRoot.getAttribute("dir"), lang: await ruRoot.getAttribute("lang"), tab: await page.locator("[data-testid=offer-tab-button-identity]").innerText() }));
    await page.screenshot({ path: path.join(OUT, "admin-offer-ru-1440.png"), fullPage: true });

    // Unit metadata visible in conditions, not placeholder.
    const conditionText = await page.locator("[data-testid=offer-tab-conditions]").innerText().catch(() => "");
    check("R9-06.noProductPlaceholder", !/از محصول/.test(conditionText), conditionText.slice(0, 120));

    // Lists at 360/768/1440 and registry locale.
    const listContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
    await listContext.addInitScript(({ adminActor }) => localStorage.setItem("tooba.adminActorUserId", adminActor), { adminActor });
    const listPage = await listContext.newPage();
    await listPage.goto(`${FRONT}/ru/admin/offers`, { waitUntil: "load" });
    await listPage.waitForSelector("[data-app-grid-shell]", { timeout: 30000 });
    await listPage.waitForTimeout(1200);
    check("R9-07.listThirdLocale", /Предложения продавцов/.test(await listPage.locator("[data-testid=admin-offers]").innerText()), (await listPage.locator("[data-testid=admin-offers]").innerText()).slice(0, 80));
    for (const width of [360, 768, 1440]) {
      await listPage.setViewportSize({ width, height: 900 });
      await listPage.waitForTimeout(250);
      const overflow = await listPage.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      check(`R9-12.listOverflow.${width}`, overflow <= 0, String(overflow));
      await listPage.screenshot({ path: path.join(OUT, `admin-list-${width}.png`), fullPage: true });
    }
    await listContext.close();

    // Seller duplicate via shared MessageBox (seller A has active listing but no create permission).
    const sellerAContext = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: "fa-IR" });
    await sellerAContext.addInitScript(({ seller }) => {
      localStorage.setItem("tooba.sellerActorUserId", seller.actorUserId);
      localStorage.setItem("tooba.sellerPartyId", seller.sellerPartyId);
    }, { seller: sellerA });
    const sellerAPage = await sellerAContext.newPage();
    await sellerAPage.goto(`${FRONT}/vendor-panel/products/new`, { waitUntil: "load" });
    await sellerAPage.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    await sellerAPage.click("[data-testid=offer-product-picker-toggle]");
    await sellerAPage.fill("[data-testid=offer-product-dialog-search]", "سامسونگ هدفون");
    await sellerAPage.keyboard.press("Enter");
    await sellerAPage.waitForSelector("[data-testid=offer-product-dialog-option]", { timeout: 20000 });
    await sellerAPage.locator("[data-testid=offer-product-dialog-option]").filter({ hasText: "سامسونگ هدفون" }).first().click();
    await sellerAPage.waitForTimeout(900);
    await sellerAPage.click("[data-testid=offer-variant-picker-toggle]");
    await sellerAPage.waitForSelector("[data-testid=offer-variant-option]", { timeout: 20000 });
    await sellerAPage.locator("[data-testid=offer-variant-option]").filter({ hasText: "آبی" }).first().click();
    await sellerAPage.waitForSelector("[data-testid=offer-message-duplicate]", { timeout: 20000 });
    check("R9-02.duplicateMessageBoxLink", (await sellerAPage.locator("[data-testid=offer-message-duplicate] a").getAttribute("href"))?.startsWith("/vendor-panel/products/") ?? false, "existing link");
    await sellerAContext.close();

    // Seller create one Draft with pre-create price/conditions.
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
    await sellerBPage.waitForTimeout(300);
    check("R9-05.sellerPreCreateTabs", !(await sellerBPage.locator("[data-testid=offer-tab-button-pricing]").isDisabled()), "price enabled");
    await sellerBPage.click("[data-testid=offer-tab-button-pricing]");
    await sellerBPage.fill("[data-testid=offer-price-input]", "654321");
    const sellerSku = `TB296-R9-SELLER-${Date.now()}`;
    await sellerBPage.click("[data-testid=offer-tab-button-identity]");
    await sellerBPage.fill("[data-testid=offer-seller-sku]", sellerSku);
    await sellerBPage.click("[data-testid=seller-create-submit]");
    await sellerBPage.waitForURL(/\/vendor-panel\/products\/[0-9a-f-]+$/, { timeout: 30000 });
    const sellerOfferId = sellerBPage.url().split("/").pop();
    result.ids.sellerOfferId = sellerOfferId;
    const sellerRead = await api(`${HOST}/v1/seller/offers/${sellerOfferId}`, { headers: sellerBHeaders });
    check("R9-05.sellerCreateApplied", sellerRead.body?.amount === 654321 && sellerRead.body?.sellerSku === sellerSku, JSON.stringify({ amount: sellerRead.body?.amount, sku: sellerRead.body?.sellerSku }));
    await sellerBContext.close();

    const sellerBOffers = await api(`${HOST}/v1/seller/offers/query`, { method: "POST", headers: sellerBHeaders, body: JSON.stringify({ page: 1, pageSize: 5, filters: [], sorts: [] }) });
    const sellerBOffer = sellerBOffers.body?.items?.[0];
    if (sellerBOffer) {
      const crossRead = await api(`${HOST}/v1/seller/offers/${sellerBOffer.offerId}`, { headers: sellerAHeaders });
      const crossWrite = await api(`${HOST}/v1/seller/offers/${sellerBOffer.offerId}/price`, { method: "PUT", headers: sellerAHeaders, body: JSON.stringify({ amount: 111, currency: "IRR" }) });
      const adminWrite = await api(`${HOST}/v1/admin/offers/${sellerBOffer.offerId}/fulfillment`, { method: "PUT", headers: sellerAHeaders, body: JSON.stringify({ responsibility: "Seller", shippingTypeId: null }) });
      check("R9-12.crossOwnerNegative", [crossRead.status, crossWrite.status].every((status) => status === 403 || status === 404) && [401, 403].includes(adminWrite.status), JSON.stringify({ read: crossRead.status, write: crossWrite.status, admin: adminWrite.status }));
    }
    const orders = await api(`${HOST}/v1/admin/orders/query`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ page: 1, pageSize: 10, filters: [], sorts: [] }) });
    const productsRegression = await api(`${HOST}/v1/admin/catalog/products?page=1&pageSize=10&locale=fa-IR`, { headers: adminHeaders });
    check("R9-12.ordersProductsRegression", (orders.body?.items ?? []).length === 10 && (productsRegression.body?.items ?? []).length > 0, JSON.stringify({ orders: (orders.body?.items ?? []).length, products: (productsRegression.body?.items ?? []).length }));
    const duplicateScoped = await api(`${HOST}/v1/admin/offers/active-listing?sellerPartyId=${sellerA.sellerPartyId}&catalogVariantId=01a05388-6ce8-7000-9cd2-66df593dd1ff&channel=Marketplace`, { headers: adminHeaders });
    check("R10-01.duplicateScopedChannel", duplicateScoped.status === 200 && Boolean(duplicateScoped.body?.offerId), JSON.stringify(duplicateScoped.body));
    check("R10-02.noAutoCreate", createStartUrl.includes("/admin/offers/new"), createStartUrl);
    const locationList = await api(`${HOST}/v1/admin/offers/inventory-locations`, { headers: adminHeaders });
    const firstLocation = Array.isArray(locationList.body) ? locationList.body[0] : null;
    let provisionA = null;
    let provisionB = null;
    if (firstLocation && r9OfferId) {
      provisionA = await api(`${HOST}/v1/admin/offers/${r9OfferId}/inventory/positions`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ locationId: firstLocation.locationId }) });
      provisionB = await api(`${HOST}/v1/admin/offers/${r9OfferId}/inventory/positions`, { method: "POST", headers: adminHeaders, body: JSON.stringify({ locationId: firstLocation.locationId }) });
    }
    check("R10-03.idempotentProvision", Boolean(firstLocation) && provisionA?.status === 200 && provisionB?.status === 200 && provisionA.body?.stockItemId === provisionB.body?.stockItemId, JSON.stringify({ a: provisionA?.body, b: provisionB?.body }));
    const historyPaged = await api(`${HOST}/v1/admin/offers/${r9OfferId}/history?page=1&pageSize=1`, { headers: adminHeaders });
    check("R10-06.historyPagination", historyPaged.status === 200 && historyPaged.body?.items?.length <= 1 && Number(historyPaged.body?.totalCount) >= 1, JSON.stringify(historyPaged.body));
    const sellerFulfillment = await api(`${HOST}/v1/admin/offers/${r9OfferId}/fulfillment`, { method: "PUT", headers: sellerAHeaders, body: JSON.stringify({ responsibility: "Seller", shippingTypeId: null }) });
    check("R10-08.sellerFulfillmentDenied", sellerFulfillment.status === 401 || sellerFulfillment.status === 403, String(sellerFulfillment.status));
    check("R10-09.noFeatureQueryAuthority", !fs.existsSync(path.resolve(process.cwd(), "src/frontend/lib/offer-workspace/offer-locale.ts")) && !fs.readFileSync(path.resolve(process.cwd(), "src/frontend/lib/i18n/registry-locale.ts"), "utf8").includes("new URLSearchParams"), "query authority absent");
    const formMeasure = result.metrics.formLoadMs ?? null;
    check("R10-13.performanceComparable", formMeasure == null || formMeasure <= 2000, String(formMeasure));    check("R9-12.noUnexpectedConsoleErrors", consoleErrors.filter((text) => !text.includes("ERR_FAILED")).length === 0, consoleErrors.filter((text) => !text.includes("ERR_FAILED")).slice(0, 3).join(" | "));
    result.metrics.pagePerformanceMs = await page.evaluate(() => Math.round(performance.now())).catch(() => null);
    result.finishedAt = new Date().toISOString();
    result.ok = result.errors.length === 0;
    fs.writeFileSync(path.join(OUT, "verify-r10.json"), JSON.stringify(result, null, 2));
    console.log(JSON.stringify({ ok: result.ok, checks: Object.keys(result.checks).length, errors: result.errors, ids: result.ids }, null, 2));
  } finally {
    await browser.close();
    await cleanup();
  }
}

main().catch((error) => { console.error(error); process.exit(2); });
