const fs = require("fs");
const path = require("path");
const { test, expect } = require("@playwright/test");
const observations = {};
test.afterAll(() => { fs.writeFileSync(path.join(__dirname, "assertions.json"), JSON.stringify(observations, null, 2)); });

const STORY = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa23a1";
const ITEM = "dddddddd-dddd-dddd-dddd-dddddddd23d1";
const MEDIA = "44444444-4444-4444-4444-444444444444";
const A = "55555555-5555-5555-5555-555555555551";
const B = "66666666-6666-6666-6666-666666666661";
const SHOP = "77777777-7777-7777-7777-777777777771";
const ACTOR = "actor-product-r23";
const LONG_A = "محصول فوق‌العاده طولانی با نام چندکلمه‌ای برای آزمایش موبایل و خوانایی";
const B_TITLE = "محصول ب";

function products() {
  const rows = [];
  for (let i = 1; i <= 20; i += 1) rows.push({ productId: `00000000-0000-4000-8000-${String(i).padStart(12, "0")}`, title: `محصول دیگر ${i}` });
  rows.unshift({ productId: B, title: B_TITLE });
  rows.push({ productId: A, title: LONG_A });
  return rows;
}

function makeState({ seller = false, productFailures = 0 } = {}) {
  const now = new Date().toISOString();
  return {
    seller,
    productFailures,
    version: 7,
    requests: [],
    story: {
      storyId: STORY,
      tenantId: "store-alpha",
      origin: seller ? "Seller" : "Admin",
      reviewStatus: "Approved",
      sellerPartyId: seller ? SHOP : null,
      rejectionReason: null,
      submittedAt: now,
      reviewedAt: now,
      submittedByActorUserId: null,
      reviewedByActorUserId: null,
      locale: "fa",
      market: null,
      title: "استوری انتخاب محصول",
      coverMediaAssetId: null,
      coverMediaUrl: "https://cdn.example.test/cover.jpg",
      displayOrder: 0,
      startAt: null,
      endAt: null,
      status: "Draft",
      ctaType: "none",
      ctaTarget: null,
      versionToken: 7,
      createdAt: now,
      updatedAt: now,
      createdByActorUserId: ACTOR,
      shopDisplayName: seller ? "فروشگاه تست" : null,
      contentReady: true,
      effectivelyPublic: false,
      actionCodes: ["update", "add_item"],
      items: [{
        storyItemId: ITEM,
        displayOrder: 0,
        mediaType: "image",
        mediaAssetId: MEDIA,
        mediaUrl: "https://cdn.example.test/item.jpg",
        caption: "زیرنویس",
        durationMs: 5000,
        ctaType: "product",
        ctaTarget: A,
        ctaProductTitle: LONG_A,
        createdAt: now,
        updatedAt: now
      }]
    }
  };
}

function caps() {
  return {
    userId: ACTOR,
    ownerScopeKind: "Platform",
    roleCodes: ["platform-admin"],
    permissions: ["story.view", "story.create", "story.edit", "story.submit", "story.approve", "story.reject", "story.publish", "accesscontrol.view"]
      .map((permissionId) => ({ permissionId, module: "Story", scopeKind: "GlobalWithinOwner", inheritedViaRoleCodes: ["platform-admin"], deniedByCeiling: false }))
  };
}

async function install(page, state) {
  if (state.seller) {
    await page.addInitScript(({ actor, shop }) => {
      localStorage.setItem("tooba.sellerActorUserId", actor);
      localStorage.setItem("tooba.sellerPartyId", shop);
    }, { actor: ACTOR, shop: SHOP });
  } else {
    await page.addInitScript(({ actor }) => {
      localStorage.setItem("tooba.adminActorUserId", actor);
    }, { actor: ACTOR });
  }
  await page.route("**/v1/**", async (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    const method = request.method();
    state.requests.push({ method, path: path + url.search, headers: request.headers(), body: request.postData() });
    const json = (value, status = 200) => route.fulfill({ status, contentType: "application/json", body: JSON.stringify(value) });
    const role = state.seller ? "seller" : "admin";
    if (path === `/v1/${role}/dev-context` || path === `/v1/${role}/dev-contexts`) {
      return json(state.seller ? [{ sellerPartyId: SHOP, actorUserId: ACTOR, sellerLabel: "فروشگاه تست" }] : { actorUserId: ACTOR, actorLabel: "admin", tenantId: "store-alpha" });
    }
    if (path.includes("/access-control/me/capabilities")) return json(caps());
    if (path.endsWith("/stories/locale-options")) return json([{ code: "fa", name: "فارسی", isDefault: true }, { code: "en", name: "English", isDefault: false }]);
    if (path.endsWith("/stories/product-options")) {
      if (state.productFailures > 0) {
        state.productFailures -= 1;
        return json({ error: "temporary" }, 503);
      }
      const search = (url.searchParams.get("search") || "").trim();
      const pageNo = Number(url.searchParams.get("page") || "1");
      const pageSize = Number(url.searchParams.get("pageSize") || "20");
      const all = products().filter((row) => !search || row.title.includes(search));
      const start = (pageNo - 1) * pageSize;
      return json({ items: all.slice(start, start + pageSize), totalCount: all.length });
    }
    if (path.endsWith("/stories/query")) return json({ items: [state.story], page: 1, pageSize: 50, totalCount: 1 });
    if (path === `/v1/${role}/stories/${STORY}` && method === "GET") return json(state.story);
    if (path === `/v1/${role}/stories/${STORY}/items/${ITEM}` && method === "PUT") {
      const body = JSON.parse(request.postData() || "{}");
      const item = state.story.items[0];
      item.mediaType = body.mediaType;
      item.mediaAssetId = body.mediaAssetId;
      item.mediaUrl = body.mediaUrl;
      item.caption = body.caption;
      item.durationMs = body.durationMs;
      item.ctaType = body.ctaType;
      item.ctaTarget = body.ctaTarget;
      item.ctaProductTitle = body.ctaTarget === B ? B_TITLE : LONG_A;
      state.version = Number(body.expectedVersion) + 1;
      state.story.versionToken = state.version;
      return json(state.story);
    }
    if (path.endsWith("/sellers/query")) return json({ items: [], page: 1, pageSize: 50, totalCount: 0 });
    return json({});
  });
}

async function openEditor(page, state, locale = "fa") {
  await page.goto(`/${locale}/${state.seller ? "vendor-panel" : "admin"}/stories/${STORY}`);
  await expect(page.getByTestId("story-detail-workspace")).toBeVisible({ timeout: 15000 });
  await page.getByTestId("story-slide-edit").click();
  await expect(page.getByTestId("story-product-picker")).toBeVisible();
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(/محصول|Product/);
}

function mutationRequests(state) {
  return state.requests.filter((row) => row.path.includes("/items/") && row.method !== "GET");
}

test("REQ-01 admin selects B, one PUT preserves media, reload shows B", async ({ page }) => {
  const state = makeState();
  await install(page, state);
  await openEditor(page, state);
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(LONG_A);
  const option = page.getByTestId("story-product-option").filter({ hasText: B_TITLE }).first();
  await option.click();
  await expect(option).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(B_TITLE);
  await page.getByTestId("story-slide-save").click();
  await expect.poll(() => mutationRequests(state).length).toBe(1);
  const put = mutationRequests(state)[0];
  const body = JSON.parse(put.body);
  expect(put.method).toBe("PUT");
  expect(put.path).toContain(`/stories/${STORY}/items/${ITEM}`);
  expect(body.ctaTarget).toBe(B);
  expect(body.expectedVersion).toBe(7);
  expect(body.occurrenceKey).toBeTruthy();
  expect(body.mediaAssetId).toBe(MEDIA);
  expect(body.mediaUrl).toBe("https://cdn.example.test/item.jpg");
  expect(state.requests.filter((row) => /\/media|\/items$/.test(row.path) || row.method === "DELETE").length).toBe(0);
  await page.reload();
  await expect(page.getByTestId("story-detail-workspace")).toBeVisible({ timeout: 15000 });
  await page.getByTestId("story-slide-edit").click();
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(B_TITLE);
  observations.REQ_01 = { putMethod: put.method, putPath: put.path, ctaTarget: body.ctaTarget, expectedVersion: body.expectedVersion, occurrence: Boolean(body.occurrenceKey), mediaAssetId: body.mediaAssetId, mediaUrl: body.mediaUrl, reloadSelected: B_TITLE };
});

test("REQ-02 cancel and Escape send no mutation and reopen A", async ({ page }) => {
  const state = makeState();
  await install(page, state);
  await openEditor(page, state);
  await page.getByTestId("story-product-option").filter({ hasText: B_TITLE }).first().click();
  await page.keyboard.press("Escape");
  const discard = page.getByTestId("story-slide-unsaved-discard");
  if (await discard.isVisible().catch(() => false)) await discard.click();
  await expect(page.getByTestId("story-slide-editor")).toBeHidden({ timeout: 5000 });
  await expect(page.getByTestId("story-slide-unsaved-dialog")).toBeHidden({ timeout: 5000 });
  await expect.poll(() => mutationRequests(state).length).toBe(0);
  await page.getByTestId("story-slide-edit").click();
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(LONG_A);
  observations.REQ_02 = { mutations: mutationRequests(state).length, reopened: LONG_A };
});

test("REQ-03 503 shows error and retry keeps A without mutation", async ({ page }) => {
  const state = makeState({ productFailures: 1 });
  await install(page, state);
  await openEditor(page, state);
  await expect(page.getByTestId("story-product-error")).toBeVisible();
  await expect(page.getByTestId("story-product-empty")).toHaveCount(0);
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(LONG_A);
  await page.getByTestId("story-product-retry").click();
  await expect(page.getByTestId("story-product-option").filter({ hasText: B_TITLE }).first()).toBeVisible();
  await expect.poll(() => mutationRequests(state).length).toBe(0);
  observations.REQ_03 = { errorVisible: true, emptyAbsent: true, retryResults: true, selection: LONG_A, mutations: mutationRequests(state).length };
});

test("REQ-04 mobile long A outside first page, no overflow and Save reachable", async ({ page }) => {
  const state = makeState();
  await install(page, state);
  await page.setViewportSize({ width: 360, height: 640 });
  await openEditor(page, state);
  await page.getByTestId("story-product-search").fill("zz");
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(LONG_A);
  await page.getByTestId("story-product-search").fill("");
  await expect(page.getByTestId("story-product-load-more")).toBeVisible();
  await page.getByTestId("story-product-load-more").click();
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(LONG_A);
  const saveBox = await page.getByTestId("story-slide-save").boundingBox();
  const cardBox = await page.getByTestId("story-product-selected").boundingBox();
  expect(saveBox).not.toBeNull();
  expect(cardBox).not.toBeNull();
  const geometry = await page.evaluate(({ saveBox, cardBox }) => ({
    overflow: document.documentElement.scrollWidth - window.innerWidth,
    saveHit: document.querySelector('[data-testid="story-slide-save"]').contains(
      document.elementFromPoint(saveBox.x + saveBox.width / 2, saveBox.y + saveBox.height / 2)),
    cardHit: document.querySelector('[data-testid="story-product-selected"]').contains(
      document.elementFromPoint(cardBox.x + cardBox.width / 2, cardBox.y + cardBox.height / 2)),
    nameWrapped: (() => {
      const name = document.querySelector('[data-testid="story-product-selected-name"]');
      return name.scrollWidth <= name.clientWidth;
    })()
  }), { saveBox, cardBox });
  expect(geometry.overflow).toBeLessThanOrEqual(0);
  expect(geometry.saveHit).toBe(true);
  expect(geometry.cardHit).toBe(true);
  expect(geometry.nameWrapped).toBe(true);
  await page.getByTestId("story-slide-save").scrollIntoViewIfNeeded();
  await page.getByTestId("story-slide-save").click();
  await expect.poll(() => mutationRequests(state).length).toBe(1);
  expect(JSON.parse(mutationRequests(state)[0].body).ctaTarget).toBe(A);
  observations.REQ_04 = { viewport: "360x640", overflow: geometry.overflow, saveHit: geometry.saveHit, cardHit: geometry.cardHit, nameWrapped: geometry.nameWrapped, saveClick: true, ctaTarget: JSON.parse(mutationRequests(state)[0].body).ctaTarget };
  await page.setViewportSize({ width: 360, height: 800 });
  const mobile800Overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(mobile800Overflow).toBeLessThanOrEqual(0);
  observations.REQ_04.mobile800Overflow = mobile800Overflow;
});

test("REQ-05 seller uses its fixed shop and fa/en direction/labels", async ({ page }) => {
  const seller = makeState({ seller: true });
  await install(page, seller);
  await openEditor(page, seller);
  await page.getByTestId("story-product-option").filter({ hasText: B_TITLE }).first().click();
  await expect(page.getByTestId("story-product-selected-name")).toHaveText(B_TITLE);
  await page.keyboard.press("Escape");
  const discard = page.getByTestId("story-slide-unsaved-discard");
  if (await discard.isVisible().catch(() => false)) await discard.click();
  await expect.poll(() => mutationRequests(seller).length).toBe(0);
  const sellerProductRequests = seller.requests.filter((row) => row.path.includes("product-options"));
  expect(sellerProductRequests.length).toBeGreaterThan(0);
  expect(sellerProductRequests.every((row) => row.headers["x-tooba-seller-party-id"] === SHOP)).toBe(true);
  expect(sellerProductRequests.every((row) => !row.path.includes(SHOP) || row.path.includes(`sellerPartyId=${SHOP}`))).toBe(true);

  const english = makeState();
  await install(page, english);
  await page.goto(`/en/admin/stories/${STORY}`);
  await expect(page.getByTestId("story-detail-workspace")).toBeVisible({ timeout: 15000 });
  await page.getByTestId("story-slide-edit").click();
  await expect(page.getByTestId("story-product-picker")).toHaveAttribute("dir", "ltr");
  await expect(page.getByTestId("story-product-selected")).toContainText("Selected product");
  await page.setViewportSize({ width: 1440, height: 900 });
  const desktopOverflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(desktopOverflow).toBeLessThanOrEqual(0);
  observations.REQ_05 = { sellerShop: SHOP, sellerHeaderOnlyFixedShop: true, englishDir: await page.getByTestId("story-product-picker").getAttribute("dir"), englishLabel: await page.getByTestId("story-product-selected").innerText(), desktopOverflow };
});
