const fs = require("fs");
const path = require("path");
const { test, expect } = require("@playwright/test");
const observations = {};
test.afterAll(() => { fs.writeFileSync(path.join(__dirname, "assertions.json"), JSON.stringify(observations, null, 2)); });

const OFFER = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa28a1";
const SELLER = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb28b1";
const VARIANT = "cccccccc-cccc-cccc-cccc-cccccccc28c1";
const STOCK1 = "dddddddd-dddd-dddd-dddd-dddddddd28d1";
const STOCK2 = "eeeeeeee-eeee-eeee-eeee-eeeeeeee28e1";
const LOC1 = "11111111-1111-4111-8111-111111111281";
const LOC2 = "22222222-2222-4222-8222-222222222282";

function makeState({ status = "Active", otherOffer = false, abortPatchOnce = false } = {}) {
  const state = {
    status,
    otherOffer,
    abortPatchOnce,
    requests: [],
    detail: {
      offerId: OFFER,
      sellerPartyId: SELLER,
      sellerDisplayName: "فروشگاه تست",
      catalogVariantId: VARIANT,
      productId: "33333333-3333-4333-8333-333333333333",
      productTitle: "کالای آزمایشی ۲۸۳",
      brandName: "برند تست",
      sellerSku: "SKU-A",
      status,
      channel: "Marketplace",
      amount: 100,
      currency: "IRR",
      onHand: 10,
      reserved: 3,
      availableUnits: 7,
      catalogReadOnly: true,
      returnPolicyChoice: "Default",
      customReturnWindowDays: null,
      defaultReturnWindowDays: 7,
      sellerCanOverrideReturnPolicy: true,
      minReturnWindowDays: 1,
      maxReturnWindowDays: 30,
      allowNonReturnableOffers: true,
      minimumOrderQuantity: 2,
      maximumOrderQuantity: 5,
      productUnitCode: "pcs",
      productUnitName: "عدد",
      productUnitShortName: null,
      inventoryLocations: [
        { stockItemId: STOCK1, locationId: LOC1, locationCode: "A", onHand: 3, reserved: 1, available: 2 },
        { stockItemId: STOCK2, locationId: LOC2, locationCode: "B", onHand: 7, reserved: 2, available: 5 }
      ]
    }
  };
  return state;
}

function caps() {
  return {
    userId: "actor-283",
    ownerScopeKind: "Seller",
    roleCodes: ["seller-owner"],
    permissions: ["product.view", "product.edit", "product.publish"].map((permissionId) => ({
      permissionId, module: "Product", scopeKind: "Own", inheritedViaRoleCodes: ["seller-owner"], deniedByCeiling: false
    }))
  };
}

async function install(page, state) {
  await page.addInitScript(({ seller }) => {
    localStorage.setItem("tooba.sellerPartyId", seller);
    localStorage.setItem("tooba.sellerActorUserId", "actor-283");
  }, { seller: SELLER });
  await page.route("**/v1/**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method();
    const bodyText = request.postData();
    state.requests.push({ method, path: path + url.search, body: bodyText, headers: request.headers() });
    const json = (value, status = 200) => route.fulfill({ status, contentType: "application/json", body: JSON.stringify(value) });
    if (path === "/v1/seller/dev-contexts") return json({ actors: [{ actorUserId: "actor-283", actorLabel: "seller", sellerPartyId: SELLER, sellerLabel: "فروشگاه تست" }] });
    if (path.includes("/access-control/me/capabilities")) return json(caps());
    if (path === `/v1/seller/offers/${OFFER}` && method === "GET") return json(state.detail);
    if (path === `/v1/seller/offers/${OFFER}` && method === "PATCH") {
      if (state.abortPatchOnce) {
        state.abortPatchOnce = false;
        const body = JSON.parse(bodyText || "{}");
        if (body.sellerSku !== undefined) state.detail.sellerSku = body.sellerSku;
        return route.abort("connectionreset");
      }
      if (state.otherOffer) return json({ ...state.detail, offerId: "ffffffff-ffff-ffff-ffff-ffffffff28f1" });
      const body = JSON.parse(bodyText || "{}");
      if (body.sellerSku !== undefined) state.detail.sellerSku = body.sellerSku;
      if (body.status !== undefined) state.detail.status = body.status;
      if (body.returnPolicyChoice !== undefined) state.detail.returnPolicyChoice = body.returnPolicyChoice;
      if (body.clearOrderQuantityLimits === true) { state.detail.minimumOrderQuantity = null; state.detail.maximumOrderQuantity = null; }
      else {
        if (body.minimumOrderQuantity !== undefined) state.detail.minimumOrderQuantity = body.minimumOrderQuantity;
        if (body.maximumOrderQuantity !== undefined) state.detail.maximumOrderQuantity = body.maximumOrderQuantity;
      }
      return json(state.detail);
    }
    if (path === `/v1/seller/offers/${OFFER}/price` && method === "PUT") {
      const body = JSON.parse(bodyText || "{}");
      state.detail.amount = body.amount;
      return json(state.detail);
    }
    if (path === `/v1/seller/offers/${OFFER}/inventory` && method === "PUT") {
      const body = JSON.parse(bodyText || "{}");
      const location = state.detail.inventoryLocations.find((row) => row.stockItemId === body.stockItemId);
      if (!location) return json({ errorCode: "inventory.position.id_unknown" }, 400);
      location.onHand = body.onHand;
      location.available = location.onHand - location.reserved;
      state.detail.onHand = state.detail.inventoryLocations.reduce((sum, row) => sum + row.onHand, 0);
      state.detail.reserved = state.detail.inventoryLocations.reduce((sum, row) => sum + row.reserved, 0);
      state.detail.availableUnits = state.detail.onHand - state.detail.reserved;
      return json(state.detail);
    }
    if (path.endsWith("/reservation-policy")) return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    return json({});
  });
}

async function openPage(page, state) {
  await page.goto(`/fa/vendor-panel/products/${OFFER}`);
  await expect(page.getByTestId("offer-save")).toBeVisible({ timeout: 15000 });
  await expect(page.getByTestId("offer-inventory-position")).toHaveCount(2);
}

function writes(state) {
  return state.requests.filter((row) => row.method !== "GET" && row.method !== "OPTIONS");
}

test("REQ-01 SKU-only save keeps positions 3/7 and sends no price or stock write", async ({ page }) => {
  const state = makeState();
  await install(page, state);
  await openPage(page, state);
  const sku = page.locator('input[dir="ltr"]').first();
  await sku.fill("SKU-B");
  await page.getByTestId("offer-save").click();
  await expect.poll(() => writes(state).length).toBe(1);
  const write = writes(state)[0];
  expect(write.method).toBe("PATCH");
  expect(write.path).toBe(`/v1/seller/offers/${OFFER}`);
  expect(JSON.parse(write.body).status).toBeUndefined();
  expect(state.requests.some((row) => row.path.endsWith("/price") || row.path.endsWith("/inventory"))).toBe(false);
  await page.reload();
  await openPage(page, state);
  await expect(page.locator('input[dir="ltr"]').first()).toHaveValue("SKU-B");
  expect(state.detail.inventoryLocations.map((row) => row.onHand)).toEqual([3, 7]);
  observations.REQ_01 = { writes: writes(state).map((row) => row.method + " " + row.path), sku: state.detail.sellerSku, positions: [3, 7], price: state.detail.amount };
});

test("REQ-02 Draft SKU edit and explicit both-limit clear", async ({ page }) => {
  const state = makeState({ status: "Draft" });
  await install(page, state);
  await openPage(page, state);
  await page.locator('input[dir="ltr"]').first().fill("SKU-DRAFT");
  await page.getByTestId("offer-min-qty").fill("");
  await page.getByTestId("offer-max-qty").fill("");
  await page.getByTestId("offer-save").click();
  await expect.poll(() => state.detail.minimumOrderQuantity === null && state.detail.maximumOrderQuantity === null).toBe(true);
  const patch = state.requests.find((row) => row.method === "PATCH");
  expect(JSON.parse(patch.body).status).toBeUndefined();
  expect(JSON.parse(patch.body).clearOrderQuantityLimits).toBe(true);
  expect(state.detail.status).toBe("Draft");
  observations.REQ_02 = { status: state.detail.status, limits: [state.detail.minimumOrderQuantity, state.detail.maximumOrderQuantity], patchBody: JSON.parse(patch.body) };
});

test("REQ-03 a stale SKU-only form does not overwrite a newer external price", async ({ page }) => {
  const state = makeState();
  await install(page, state);
  await openPage(page, state);
  state.detail.amount = 150;
  await page.locator('input[dir="ltr"]').first().fill("SKU-CONCURRENT");
  await page.getByTestId("offer-save").click();
  await expect.poll(() => state.detail.sellerSku).toBe("SKU-CONCURRENT");
  await page.reload();
  await openPage(page, state);
  await expect(page.locator('input[dir="ltr"]').first()).toHaveValue("SKU-CONCURRENT");
  expect(state.detail.amount).toBe(150);
  expect(state.requests.some((row) => row.path.endsWith("/price"))).toBe(false);
  observations.REQ_03 = { sku: state.detail.sellerSku, price: state.detail.amount, priceWrites: 0 };
});
test("REQ-04 named second position 7→8 keeps 3 and total 11", async ({ page }) => {
  const state = makeState();
  await install(page, state);
  await openPage(page, state);
  const second = page.locator(`[data-stock-item-id="${STOCK2}"] input`);
  await second.fill("8");
  await page.getByTestId("offer-save").click();
  await expect.poll(() => state.detail.inventoryLocations.find((row) => row.stockItemId === STOCK2)?.onHand).toBe(8);
  const inv = state.requests.find((row) => row.path.endsWith("/inventory"));
  expect(JSON.parse(inv.body).stockItemId).toBe(STOCK2);
  expect(JSON.parse(inv.body).onHand).toBe(8);
  expect(state.detail.inventoryLocations.find((row) => row.stockItemId === STOCK1).onHand).toBe(3);
  expect(state.detail.inventoryLocations.reduce((sum, row) => sum + row.onHand, 0)).toBe(11);
  observations.REQ_04 = { target: STOCK2, positions: state.detail.inventoryLocations.map((row) => [row.locationCode, row.onHand]), total: state.detail.onHand };
});

test("REQ-05 wrong response and lost response never claim save", async ({ page }) => {
  const wrong = makeState({ otherOffer: true });
  await install(page, wrong);
  await openPage(page, wrong);
  await page.locator('input[dir="ltr"]').first().fill("SKU-WRONG");
  await page.getByTestId("offer-save").click();
  await expect(page.getByTestId("offer-save-error")).toContainText("نامشخص");
  await expect(page.getByTestId("offer-save-notice")).toHaveCount(0);
  expect(wrong.detail.sellerSku).toBe("SKU-A");

  const lost = makeState({ abortPatchOnce: true });
  await install(page, lost);
  await openPage(page, lost);
  await page.locator('input[dir="ltr"]').first().fill("SKU-LOST");
  await page.getByTestId("offer-save").click();
  await expect.poll(() => lost.detail.sellerSku).toBe("SKU-LOST");
  await expect(page.getByTestId("offer-save-notice")).toContainText("بازخوانی");
  expect(writes(lost).filter((row) => row.method === "PATCH").length).toBe(1);
  observations.REQ_05 = { wrongResponseError: true, lostResponseNotice: "reconciled-by-GET", patchCount: 1 };
});

test("REQ-06 cancel sends no writes and mobile dirty form is reachable", async ({ page }) => {
  const state = makeState();
  await install(page, state);
  await page.setViewportSize({ width: 360, height: 640 });
  await openPage(page, state);
  await page.locator('input[dir="ltr"]').first().fill("SKU-CANCEL");
  await expect(page.getByTestId("offer-dirty")).toBeVisible();
  await page.getByTestId("offer-cancel").click();
  await expect.poll(() => writes(state).length).toBe(0);
  await page.goBack();
  await openPage(page, state);
  await expect(page.locator('input[dir="ltr"]').first()).toHaveValue("SKU-A");
  await page.getByTestId("offer-save").scrollIntoViewIfNeeded();
  await page.getByTestId("offer-cancel").scrollIntoViewIfNeeded();
  const geometry = await page.evaluate(() => {
    const save = document.querySelector('[data-testid="offer-save"]');
    const cancel = document.querySelector('[data-testid="offer-cancel"]');
    const title = document.querySelector('[data-testid="offer-inventory-positions"]');
    const box = (el) => { const r = el.getBoundingClientRect(); return { x: r.x, y: r.y, w: r.width, h: r.height }; };
    const hit = (el) => { const r = box(el); return el.contains(document.elementFromPoint(r.x + r.w / 2, r.y + r.h / 2)); };
    return { overflow: document.documentElement.scrollWidth - window.innerWidth, saveHit: hit(save), cancelHit: hit(cancel), titleVisible: !!title };
  });
  expect(geometry.overflow).toBeLessThanOrEqual(0);
  expect(geometry.saveHit).toBe(true);
  expect(geometry.cancelHit).toBe(true);
  expect(geometry.titleVisible).toBe(true);
  observations.REQ_06 = { writes: writes(state).length, geometry };
});

test("REQ-07 no-change save sends zero writes and a failed first section keeps the other draft", async ({ page }) => {
  const state = makeState();
  await install(page, state);
  await openPage(page, state);
  await page.getByTestId("offer-save").click();
  await expect.poll(() => writes(state).length).toBe(0);
  await page.locator('input[dir="ltr"]').first().fill("SKU-FAIL");
  await page.locator(`[data-stock-item-id="${STOCK2}"] input`).fill("8");
  state.abortPatchOnce = true;
  await page.getByTestId("offer-save").click();
  await expect(page.getByTestId("offer-save-notice")).toContainText("بازخوانی");
  await expect(page.locator(`[data-stock-item-id="${STOCK2}"] input`)).toHaveValue("8");
  expect(state.requests.some((row) => row.path.endsWith("/inventory"))).toBe(false);
  observations.REQ_07 = { noChangeWrites: 0, retainedOtherDraft: "8", inventoryWritesAfterPatchFailure: 0 };
});
