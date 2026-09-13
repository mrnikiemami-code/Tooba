import { execFileSync } from "node:child_process";
import { writeFileSync } from "node:fs";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const MOBILE = "09111111111";
const OTP = "123456";
const EVIDENCE = "docs/evidence/TB-P10-T004";
const log = {};
const net = { authMe: 0, merge: 0, logout: 0, commit: 0, consoleErrors: [], status401: [] };

function rec(name, ok, detail) {
  log[name] = { ok: !!ok, detail };
  console.log(`${ok ? "PASS" : "FAIL"} ${name}: ${detail}`);
}

function pick(obj, ...names) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const n of names) if (obj[n] !== undefined) return obj[n];
  return undefined;
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q],
    { encoding: "utf8" },
  ).trim();
}

async function req(method, path, { body, extra } = {}) {
  const headers = { Host: "alpha.localhost", Accept: "application/json", ...(extra ?? {}) };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const res = await fetch(HOST + path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: res.status, json, text };
}

async function otpLogin() {
  const request = await req("POST", "/v1/auth/otp-login/request", { body: { identifier: MOBILE } });
  const challengeId = pick(request.json, "challengeId", "ChallengeId");
  const complete = await req("POST", "/v1/auth/otp-login/complete", {
    body: { identifier: MOBILE, challengeId, secret: OTP },
  });
  return {
    token: pick(complete.json, "accessToken", "AccessToken"),
    userId: pick(complete.json, "userId", "UserId"),
  };
}

async function waitVisible(page, selector, timeout = 20000) {
  await page.waitForSelector(selector, { state: "visible", timeout });
}

async function loginUi(page) {
  await page.goto(`${FE}/fa/login`, { waitUntil: "domcontentloaded" });
  const mobileInput = page.locator("[data-testid=login-mobile-input]");
  await mobileInput.waitFor({ state: "visible", timeout: 20000 });
  await page.waitForFunction(() => {
    const el = document.querySelector("[data-testid=login-mobile-input]");
    return !!el && !el.disabled;
  });
  await mobileInput.click();
  await mobileInput.fill("");
  await mobileInput.pressSequentially(MOBILE, { delay: 20 });
  const send = page.locator("[data-testid=login-send-otp]");
  await page.waitForFunction(() => {
    const btn = document.querySelector("[data-testid=login-send-otp]");
    return !!btn && !btn.disabled;
  });
  await send.click();
  await waitVisible(page, "[data-testid=login-otp-input]");
  const otpInput = page.locator("[data-testid=login-otp-input]");
  await otpInput.click();
  await otpInput.fill("");
  await otpInput.pressSequentially(OTP, { delay: 20 });
  const verify = page.locator("[data-testid=login-verify-otp]");
  await page.waitForFunction(() => {
    const btn = document.querySelector("[data-testid=login-verify-otp]");
    return !!btn && !btn.disabled;
  });
  await verify.click();
  await page.waitForURL((url) => !url.pathname.includes("/login"), { timeout: 20000 });
}

async function addTwoLines(page, offerIds) {
  return page.evaluate(async (ids) => {
    const current = await fetch("/v1/storefront/cart/current", { credentials: "include", cache: "no-store" });
    let cart = current.ok ? await current.json() : null;
    let status = String(cart?.status || cart?.Status || "");
    if (!cart || !/^active$/i.test(status)) {
      const created = await fetch("/v1/storefront/cart", { method: "POST", credentials: "include", cache: "no-store" });
      cart = await created.json();
      status = String(cart?.status || cart?.Status || "Active");
    }
    let cartId = cart.cartId || cart.CartId;
    let version = cart.version ?? cart.Version ?? 0;
    let guestSecret = cart.guestSecret || cart.GuestSecret || sessionStorage.getItem("tooba.storefront.guestSecret");
    const existingLines = cart.lines || cart.Lines || [];
    const existingCount = cart.itemCount ?? cart.ItemCount ?? existingLines.length;
    if (/^active$/i.test(status) && (existingLines.length >= 2 || Number(existingCount) >= 2)) {
      return { status: 200, cartId, lines: existingLines.length, itemCount: existingCount, reused: true };
    }
    const errors = [];
    for (const offerId of ids) {
      const headers = { "content-type": "application/json" };
      if (guestSecret) headers["X-Tooba-Guest-Secret"] = guestSecret;
      let line = await fetch(`/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
        method: "POST",
        credentials: "include",
        cache: "no-store",
        headers,
        body: JSON.stringify({ offerId, quantity: 1 }),
      });
      let next = await line.json().catch(() => ({}));
      if (!line.ok && (next.errorCode === "cart.guest.invalid" || next.ErrorCode === "cart.guest.invalid")) {
        sessionStorage.removeItem("tooba.storefront.cartId");
        sessionStorage.removeItem("tooba.storefront.guestSecret");
        const created = await fetch("/v1/storefront/cart", { method: "POST", credentials: "include", cache: "no-store" });
        cart = await created.json();
        cartId = cart.cartId || cart.CartId;
        version = cart.version ?? cart.Version ?? 0;
        guestSecret = cart.guestSecret || cart.GuestSecret || guestSecret;
        const retryHeaders = { "content-type": "application/json" };
        if (guestSecret) retryHeaders["X-Tooba-Guest-Secret"] = guestSecret;
        line = await fetch(`/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
          method: "POST",
          credentials: "include",
          cache: "no-store",
          headers: retryHeaders,
          body: JSON.stringify({ offerId, quantity: 1 }),
        });
        next = await line.json().catch(() => ({}));
      }
      if (!line.ok) {
        errors.push({ offerId, status: line.status, next });
        continue;
      }
      cartId = next.cartId || next.CartId || cartId;
      version = next.version ?? next.Version ?? version;
      guestSecret = next.guestSecret || next.GuestSecret || guestSecret;
      cart = next;
    }
    return {
      status: errors.length ? errors[0].status : 200,
      cartId,
      lines: (cart.lines || cart.Lines || []).length,
      itemCount: cart.itemCount ?? cart.ItemCount,
      guestSecret,
      errors,
    };
  }, offerIds);
}

async function badgeText(page) {
  const el = page.locator("[data-testid=header-cart-badge]").first();
  if (!(await el.count())) return "0";
  return ((await el.innerText()) || "0").trim();
}

function faDigits(n) {
  return String(n).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)]);
}

async function waitBadge(page, expected, timeout = 15000) {
  const en = String(expected);
  const fa = faDigits(expected);
  await page.waitForFunction(
    ({ en: e, fa: f }) => {
      const t = (document.querySelector("[data-testid=header-cart-badge]")?.textContent || "").trim();
      return t === e || t === f;
    },
    { en, fa },
    { timeout },
  );
}

function badgeMatches(text, expected) {
  const t = (text || "").trim();
  return t === String(expected) || t === faDigits(expected);
}

async function typeField(locator, value) {
  await locator.click();
  await locator.fill("");
  await locator.pressSequentially(value, { delay: 20 });
}

async function completeShipping(page) {
  const toggle = page.locator("[data-testid=shipping-saved-toggle]");
  if (await toggle.count()) {
    await toggle.click().catch(() => {});
    try {
      await waitVisible(page, "[data-testid=shipping-saved-list]", 8000);
      await page.locator("[data-testid=shipping-saved-list] button").first().click();
      await page.waitForTimeout(500);
    } catch {
      /* optional saved list */
    }
  }
  await typeField(page.locator("[data-testid=shipping-first-name]"), "محمد");
  await typeField(page.locator("[data-testid=shipping-last-name]"), "امامی");
  const mobile = page.locator("[data-testid=shipping-recipient] input[dir=ltr]").first();
  if (await mobile.count()) {
    await typeField(mobile, MOBILE);
  }
  const province = page.locator("[data-testid=shipping-recipient] select").first();
  if (await province.count()) {
    const current = await province.inputValue().catch(() => "");
    if (!current) {
      const options = await province.locator("option").allTextContents();
      const firstReal = (await province.locator("option").nth(1).getAttribute("value")) || "";
      if (firstReal) await province.selectOption(firstReal);
      void options;
    }
  }
  await page.waitForTimeout(400);
  const city = page.locator("[data-testid=shipping-recipient] select").nth(1);
  if (await city.count()) {
    const current = await city.inputValue().catch(() => "");
    if (!current) {
      const firstReal = (await city.locator("option").nth(1).getAttribute("value")) || "";
      if (firstReal) await city.selectOption(firstReal);
    }
  }
  const address = page.locator("[data-testid=shipping-recipient] textarea").first();
  if (await address.count()) {
    const current = (await address.inputValue().catch(() => "")) || "";
    if (!current.trim()) await typeField(address, "تهران خیابان آزمایش ۱۲");
  }
  const postal = page.locator("[data-testid=shipping-recipient] input[maxlength='10']").first();
  if (await postal.count()) {
    const current = (await postal.inputValue().catch(() => "")) || "";
    if (!current.trim()) await typeField(postal, "1234567890");
  }
  const methodBtn = page.locator("[data-testid^=shipping-method-]").first();
  await methodBtn.waitFor({ state: "visible", timeout: 15000 });
  await methodBtn.click();
  const dateBtn = page.locator("[data-testid^=shipping-date-]").first();
  await dateBtn.waitFor({ state: "visible", timeout: 15000 });
  await dateBtn.click();
  const timeBtn = page.locator("[data-testid=shipping-time] button").first();
  if (await timeBtn.count()) await timeBtn.click();
  await page.waitForFunction(() => {
    const btn = document.querySelector("[data-testid=shipping-continue]");
    return !!btn && !btn.disabled;
  }, null, { timeout: 15000 });
}

(async () => {
  const hostLogin = await otpLogin();
  rec("prep-login", !!hostLogin.token, hostLogin.userId || "no-token");
  if (!hostLogin.token) throw new Error("host login failed");
  const auth = { Authorization: `Bearer ${hostLogin.token}` };
  const me = await req("GET", "/v1/auth/me", { extra: auth });
  rec(
    "A-me-identity",
    me.status === 200 && (pick(me.json, "mobile", "Mobile") === MOBILE || !!pick(me.json, "displayName", "DisplayName")),
    JSON.stringify({
      displayName: pick(me.json, "displayName", "DisplayName"),
      mobile: pick(me.json, "mobile", "Mobile"),
      firstName: pick(me.json, "firstName", "FirstName"),
    }),
  );

  const openUnpaid = sql(
    `select distinct k.checkout_id::text from "order".checkouts k join "order".seller_orders o on o.checkout_id=k.checkout_id where k.placed_by_user_id='${hostLogin.userId}' and o.status in ('PendingPayment','Submitted');`,
  )
    .split("\n")
    .map((x) => x.trim())
    .filter(Boolean);
  const cancelled = [];
  for (const checkoutId of openUnpaid) {
    const res = await req("POST", `/v1/storefront/checkout/${checkoutId}/cancel`, { extra: auth, body: {} });
    cancelled.push({ checkoutId, status: res.status });
  }
  rec("prep-cancel-open-unpaid", cancelled.every((x) => x.status < 500), JSON.stringify(cancelled));
  sql(
    `delete from "order".checkout_reservation_commits where customer_id='${hostLogin.userId}' and occurred_at > now() - interval '30 minutes';`,
  );

  const offers = sql(
    `select o.offer_id::text from offer.offers o join inventory.stock_positions s on s.offer_id = o.offer_id where s.on_hand - s.reserved >= 1 limit 2;`,
  )
    .split("\n")
    .map((x) => x.trim())
    .filter(Boolean);
  rec("prep-offers", offers.length >= 2, offers.join(","));

  const browser = await chromium.launch({ headless: true });
  const customer = await browser.newContext({ locale: "fa-IR", viewport: { width: 1440, height: 1100 } });
  const page = await customer.newPage();
  page.on("console", (msg) => {
    if (msg.type() === "error") net.consoleErrors.push(msg.text());
  });
  page.on("request", (request) => {
    const u = request.url();
    if (u.includes("/api/auth/me") || u.includes("/v1/auth/me")) net.authMe += 1;
    if (u.includes("/cart/merge")) net.merge += 1;
    if (u.includes("/auth/logout")) net.logout += 1;
    if (u.includes("/shipping/commit")) net.commit += 1;
  });
  page.on("response", (response) => {
    if (response.status() === 401) net.status401.push(response.url().split("?")[0]);
  });

  await loginUi(page);
  rec("A-login", true, page.url());
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  await waitVisible(page, "[data-testid=header-account-menu]", 30000);
  const label = (await page.locator("[data-testid=header-account-label]").first().innerText()).trim();
  rec("B-header-identity", label === MOBILE || (label.length > 0 && label !== "حساب کاربری"), label);
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r3-header-identity.png`, fullPage: false });
  await page.locator("[data-testid=header-account-button]").first().click();
  await waitVisible(page, "[data-testid=header-account-dropdown]");
  rec(
    "dropdown",
    (await page.locator("[data-testid=header-account-panel]").count()) >= 1 &&
      (await page.locator("[data-testid=header-account-orders]").count()) >= 1 &&
      (await page.locator("[data-testid=header-account-logout]").count()) >= 1,
    "panel/orders/logout",
  );
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r3-account-dropdown.png`, fullPage: false });
  await page.locator("[data-testid=header-account-button]").first().click();

  const added = await addTwoLines(page, offers);
  rec("C-atc-2", added.lines >= 2 || Number(added.itemCount) >= 2, JSON.stringify(added));
  await page.evaluate((cartId) => {
    sessionStorage.setItem("tooba.storefront.cartId", cartId);
    window.dispatchEvent(new Event("tooba-cart-changed"));
  }, added.cartId);
  await page.goto(`${FE}/fa/cart`, { waitUntil: "domcontentloaded" });
  const expectedCount = Number(added.itemCount) >= 2 ? Number(added.itemCount) : 2;
  await waitBadge(page, expectedCount);
  const beforeLogoutBadge = await badgeText(page);
  rec("D-cart-2", badgeMatches(beforeLogoutBadge, expectedCount), `badge=${beforeLogoutBadge}`);
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r3-cart-before-logout.png`, fullPage: true });
  const accountCartId = added.cartId;

  await page.locator("[data-testid=header-account-button]").first().click();
  await page.locator("[data-testid=header-account-logout]").first().click();
  await page.waitForSelector("[data-testid=header-login-link]", { timeout: 15000 });
  rec("E-logout-ui", true, "ورود visible");
  await waitBadge(page, 0);
  const anonBadge = await badgeText(page);
  rec("F-anon-hidden", badgeMatches(anonBadge, 0), `badge=${anonBadge}`);
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r3-anonymous-after-logout.png`, fullPage: false });

  const db = sql(
    `select status||'|'||coalesce(owner_user_id::text,'')||'|'||(select count(*) from cart.cart_lines l where l.cart_id=c.cart_id) from cart.carts c where c.cart_id='${accountCartId}';`,
  );
  rec("G-db-still-active", db.startsWith("Active|") && db.includes(hostLogin.userId) && db.endsWith("|2"), db);
  const anonGet = await req("GET", `/v1/storefront/cart/${accountCartId}`);
  rec("F-anon-api", [400, 401, 403, 404].includes(anonGet.status), String(anonGet.status));

  await loginUi(page);
  await page.goto(`${FE}/fa/cart`, { waitUntil: "domcontentloaded" });
  await waitBadge(page, expectedCount);
  const restored = await badgeText(page);
  rec("H-relogin", true, page.url());
  rec("I-restored-2", badgeMatches(restored, expectedCount), `badge=${restored}`);
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r3-cart-after-relogin.png`, fullPage: true });

  for (const pathName of ["/fa", "/fa/cart", "/fa/shipping"]) {
    await page.goto(`${FE}${pathName}`, { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(500);
    const same = (await page.locator("[data-testid=header-account-label]").first().innerText()).trim();
    rec(`J-identity-${pathName}`, same === label, same);
  }

  await page.goto(`${FE}/fa/shipping`, { waitUntil: "domcontentloaded" });
  await waitVisible(page, "[data-testid=shipping-page]");
  await completeShipping(page);
  const afterAddressBadge = await badgeText(page);
  rec("K-address-cart-2", badgeMatches(afterAddressBadge, expectedCount), `badge=${afterAddressBadge}`);
  const beforeCommitBadge = await badgeText(page);
  rec("L-before-continue", badgeMatches(beforeCommitBadge, expectedCount), `badge=${beforeCommitBadge}`);
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r3-shipping-before-commit.png`, fullPage: true });

  const timeline = { t3: Date.now(), t4: 0, t8: 0, t9: 0 };
  const onCommitReq = (request) => {
    if (request.url().includes("/shipping/commit")) {
      timeline.t4 = Date.now();
      page.off("request", onCommitReq);
    }
  };
  const onCommitRes = (response) => {
    if (response.url().includes("/shipping/commit")) {
      timeline.t8 = Date.now();
      page.off("response", onCommitRes);
    }
  };
  page.on("request", onCommitReq);
  page.on("response", onCommitRes);
  const shippingNet = [];
  page.on("response", (response) => {
    if (response.url().includes("/shipping/")) {
      shippingNet.push({ status: response.status(), url: response.url() });
    }
  });
  await page.locator("[data-testid=shipping-continue]").click();
  try {
    await page.waitForURL((url) => url.pathname.includes("/payment"), { timeout: 25000 });
  } catch (error) {
    const err = await page.locator("[data-testid=shipping-error]").textContent().catch(() => "");
    rec("M-ship-error", false, JSON.stringify({ err, shippingNet, url: page.url() }));
    await page.screenshot({ path: `${EVIDENCE}/r24-r1-r3-shipping-commit-fail.png`, fullPage: true });
    throw error;
  }
  timeline.t9 = Date.now();
  rec("M-order", timeline.t4 > 0 && timeline.t8 >= timeline.t4 && timeline.t9 >= timeline.t8, JSON.stringify(timeline));
  await page.waitForTimeout(800);
  const payBadge = await badgeText(page);
  rec("O-payment", page.url().includes("/payment"), page.url());
  rec("P-badge-after-commit", badgeMatches(payBadge, 0) || /[۰0]/.test(payBadge), `badge=${payBadge}`);
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r3-payment-after-commit.png`, fullPage: true });
  const converted = sql(`select status from cart.carts where cart_id='${accountCartId}';`);
  rec("N-converted", converted === "Converted", converted);
  const cycle = sql(
    `select count(*) from "order".reservation_cycles c join "order".checkouts k on k.checkout_id=c.checkout_id where k.placed_by_user_id='${hostLogin.userId}';`,
  );
  rec("N-cycle", Number(cycle) >= 1, cycle);

  await page.evaluate(() => {
    sessionStorage.removeItem("tooba.storefront.cartId");
    sessionStorage.removeItem("tooba.storefront.guestSecret");
  });
  const added2 = await addTwoLines(page, offers);
  if (added2.cartId) {
    await page.evaluate((payload) => {
      sessionStorage.setItem("tooba.storefront.cartId", payload.cartId);
      if (payload.guestSecret) sessionStorage.setItem("tooba.storefront.guestSecret", payload.guestSecret);
      window.dispatchEvent(new Event("tooba-cart-changed"));
    }, { cartId: added2.cartId, guestSecret: added2.guestSecret || null });
  }
  rec("Q-prep-fault-cart", added2.lines >= 2 || Number(added2.itemCount) >= 2, JSON.stringify(added2));
  if (!(added2.lines >= 2 || Number(added2.itemCount) >= 2)) {
    throw new Error(`Q-prep-fault-cart empty ${JSON.stringify(added2)}`);
  }
  await page.route("**/v1/storefront/shipping/commit", async (route) => {
    await route.fulfill({ status: 409, contentType: "application/json", body: JSON.stringify({ errorCode: "checkout.fault", detail: "injected" }) });
  });
  await page.goto(`${FE}/fa/shipping`, { waitUntil: "domcontentloaded" });
  await waitVisible(page, "[data-testid=shipping-page]");
  await completeShipping(page);
  await page.click("[data-testid=shipping-continue]");
  await page.waitForTimeout(1200);
  rec("Q-no-payment", !page.url().includes("/payment"), page.url());
  const faultBadge = await badgeText(page);
  rec("Q-cart-intact", !badgeMatches(faultBadge, 0), `badge=${faultBadge}`);
  await page.unroute("**/v1/storefront/shipping/commit");

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  await waitVisible(page, "[data-testid=header-account-menu]", 20000);
  await page.locator('button[aria-label="منوی موبایل"]').click();
  await page.waitForTimeout(400);
  await page.locator("[data-testid=header-account-button]").last().click();
  await waitVisible(page, "[data-testid=header-account-logout]", 8000);
  const mobileLogout = await page.locator("[data-testid=header-account-logout]").count();
  rec("R-mobile-menu", mobileLogout >= 1, `logout=${mobileLogout}`);

  const authMeHits = net.authMe;
  const max401PerUrl = net.status401.reduce((map, url) => {
    map[url] = (map[url] || 0) + 1;
    return map;
  }, {});
  const storm = Object.values(max401PerUrl).some((n) => n >= 25);
  rec("net-no-401-storm", !storm, JSON.stringify({ authMeHits, max401PerUrl, console401: net.consoleErrors.filter((x) => x.includes("401")).length }));
  rec("net-merge-not-loop", net.merge <= 6, String(net.merge));
  rec("net-logout-not-dup-storm", net.logout <= 3, String(net.logout));

  await browser.close();
  writeFileSync(`${EVIDENCE}/_r24-r1-r3-runtime-raw.json`, JSON.stringify({ accountCartId, label, net, timeline, log }, null, 2));
  const failed = Object.entries(log).filter(([, v]) => !v.ok);
  if (failed.length) {
    console.error("FAILED", failed);
    process.exit(1);
  }
  console.log("ALL_RUNTIME_PASS", accountCartId);
})().catch((error) => {
  console.error(error);
  writeFileSync(`${EVIDENCE}/_r24-r1-r3-runtime-raw.json`, JSON.stringify({ error: String(error), log, net }, null, 2));
  process.exit(1);
});
