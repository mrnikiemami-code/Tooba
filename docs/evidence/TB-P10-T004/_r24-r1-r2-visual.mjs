import { execFileSync } from "node:child_process";
import { writeFileSync } from "node:fs";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const MOBILE = "09111111111";
const OTP = "123456";
const CANONICAL = "محمد امامی";
const LEGACY = "محمد لمامی";
const EVIDENCE = "docs/evidence/TB-P10-T004";
const log = {};

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

function bearer(token) {
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function waitVisible(page, selector, timeout = 20000) {
  await page.waitForSelector(selector, { state: "visible", timeout });
}

(async () => {
  const login = await otpLogin();
  rec("prep-login", !!login.token, login.userId || "no-token");
  if (!login.token) throw new Error("host login failed");
  const auth = bearer(login.token);

  const pending = sql(
    `select c.checkout_id from "order".checkouts c join "order".seller_orders s on s.checkout_id = c.checkout_id where c.placed_by_user_id = '${login.userId}' and s.status in ('PendingPayment','Submitted');`,
  );
  for (const id of pending.split("\n").map((x) => x.trim()).filter(Boolean)) {
    await req("POST", `/v1/storefront/checkout/${id}/cancel`, { extra: auth });
  }

  const listed = await req("GET", "/v1/customer/addresses", { extra: auth });
  const addresses = Array.isArray(listed.json) ? listed.json : pick(listed.json, "items", "Items") ?? [];
  let legacy = addresses.find(
    (a) =>
      pick(a, "recipientName", "RecipientName") === LEGACY &&
      !pick(a, "firstName", "FirstName") &&
      !pick(a, "lastName", "LastName"),
  );
  if (!legacy) {
    const created = await req("POST", "/v1/customer/addresses", {
      extra: auth,
      body: {
        recipientName: LEGACY,
        contactMobile: "09121111444",
        country: "IR",
        provinceName: "تهران",
        cityName: "تهران",
        postalCode: "1111111111",
        postalAddress: "نشانی قدیمی لمامی",
        label: "legacy-r24r1r2",
        isDefault: false,
      },
    });
    legacy = created.json;
  }
  rec("prep-legacy", !!pick(legacy, "addressId", "AddressId"), pick(legacy, "addressId", "AddressId"));

  const offerId = sql(
    `select o.offer_id::text from offer.offers o join inventory.stock_positions s on s.offer_id = o.offer_id where s.on_hand - s.reserved >= 1 limit 1;`,
  );
  rec("prep-offer", !!offerId, offerId);

  const historical = sql(
    `select checkout_id from "order".checkouts where coalesce(recipient_first_name,'') = '' and coalesce(recipient_last_name,'') = '' and recipient_name <> '' order by submitted_at desc limit 1;`,
  );
  rec("prep-historical", !!historical, historical);

  const browser = await chromium.launch({ headless: true });
  const customer = await browser.newContext({ locale: "fa-IR", viewport: { width: 1440, height: 1100 } });
  const admin = await browser.newContext({ locale: "fa-IR", viewport: { width: 1440, height: 1100 } });
  rec("contexts", true, "isolated customer + admin (no shared cookies)");

  const page = await customer.newPage();
  await page.goto(`${FE}/fa/login`, { waitUntil: "networkidle" });
  await waitVisible(page, "[data-testid=login-mobile-input]");
  await page.fill("[data-testid=login-mobile-input]", MOBILE);
  await page.click("[data-testid=login-send-otp]");
  await waitVisible(page, "[data-testid=login-otp-input]");
  await page.fill("[data-testid=login-otp-input]", OTP);
  await page.click("[data-testid=login-verify-otp]");
  await page.waitForURL((url) => !url.pathname.includes("/login"), { timeout: 20000 });
  rec("browser-customer-login", true, page.url());

  const added = await page.evaluate(async (id) => {
    const current = await fetch("/v1/storefront/cart/current", { credentials: "include", cache: "no-store" });
    let cart = current.ok ? await current.json() : null;
    if (!cart) {
      const created = await fetch("/v1/storefront/cart", { method: "POST", credentials: "include", cache: "no-store" });
      cart = await created.json();
    }
    const cartId = cart.cartId || cart.CartId;
    const version = cart.version ?? cart.Version ?? 0;
    const existing = cart.lines || cart.Lines || [];
    if (existing.length >= 1) return { status: 200, cartId, lines: existing.length, reused: true };
    const line = await fetch(`/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
      method: "POST",
      credentials: "include",
      cache: "no-store",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ offerId: id, quantity: 1 }),
    });
    const next = await line.json().catch(() => ({}));
    return { status: line.status, cartId, lines: (next.lines || next.Lines || []).length };
  }, offerId);
  rec("browser-atc", added.status === 200 && added.lines >= 1, JSON.stringify(added));

  await page.goto(`${FE}/fa/shipping`, { waitUntil: "networkidle" });
  await waitVisible(page, "[data-testid=shipping-page]");
  await page.click("[data-testid=shipping-saved-toggle]");
  await waitVisible(page, "[data-testid=shipping-saved-list]");
  const picked = await page.evaluate((legacyName) => {
    const buttons = [...document.querySelectorAll("[data-testid=shipping-saved-list] button")];
    const match = buttons.find((b) => (b.innerText || "").includes(legacyName));
    if (!match) return false;
    match.click();
    return true;
  }, LEGACY);
  rec("shipping-select-legacy", picked, picked ? LEGACY : "legacy row not listed");
  await page.waitForTimeout(500);
  await page.fill("[data-testid=shipping-first-name]", "محمد");
  await page.fill("[data-testid=shipping-last-name]", "امامی");

  const methodBtn = page.locator("[data-testid^=shipping-method-]").first();
  await methodBtn.waitFor({ state: "visible", timeout: 15000 });
  await methodBtn.click();
  const dateBtn = page.locator("[data-testid^=shipping-date-]").first();
  await dateBtn.waitFor({ state: "visible", timeout: 15000 });
  await dateBtn.click();
  const timeBtn = page.locator("[data-testid=shipping-time] button").first();
  if (await timeBtn.count()) await timeBtn.click();
  await page.click("[data-testid=shipping-continue]");
  await page.waitForURL((url) => url.pathname.includes("/payment"), { timeout: 25000 });
  let checkoutId = new URL(page.url()).searchParams.get("checkoutId") || "";
  rec("checkout-id", !!checkoutId, checkoutId);
  await page.goto(`${FE}/fa/payment?checkoutId=${encodeURIComponent(checkoutId)}`, { waitUntil: "networkidle" });
  await page.waitForFunction((name) => document.body.innerText.includes(name), CANONICAL, { timeout: 20000 });
  const payText = await page.locator("body").innerText();
  rec("payment-canonical", payText.includes(CANONICAL) && !payText.includes(LEGACY), page.url());
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r2-payment-recipient.png`, fullPage: true });

  await page.goto(`${FE}/fa/customer-panel/orders/${checkoutId}`, { waitUntil: "networkidle" });
  await page.waitForTimeout(1500);
  const custText = await page.locator("body").innerText();
  rec("customer-canonical", custText.includes(CANONICAL) && !custText.includes(LEGACY), page.url());
  await page.screenshot({ path: `${EVIDENCE}/r24-r1-r2-customer-order-recipient.png`, fullPage: true });

  const adminPage = await admin.newPage();
  await adminPage.addInitScript((actor) => {
    localStorage.setItem("tooba.adminActorUserId", actor);
  }, ADMIN);
  await adminPage.goto(`${FE}/admin/orders/${checkoutId}`, { waitUntil: "networkidle" });
  await adminPage.waitForTimeout(2000);
  const adminText = await adminPage.locator("body").innerText();
  rec(
    "admin-canonical",
    adminText.includes(CANONICAL) && !adminText.includes(LEGACY) && !/denied|دسترسی ندارید|authorization/i.test(adminText),
    pageSnippet(adminText),
  );
  await adminPage.screenshot({ path: `${EVIDENCE}/r24-r1-r2-admin-order-recipient.png`, fullPage: true });

  if (historical) {
    await adminPage.goto(`${FE}/admin/orders/${historical}`, { waitUntil: "networkidle" });
    await adminPage.waitForTimeout(1500);
    const histText = await adminPage.locator("body").innerText();
    rec("historical-legacy", /محمد لمامی|گیرنده/.test(histText) && histText.includes(sql(`select recipient_name from "order".checkouts where checkout_id='${historical}';`)), historical);
    await adminPage.screenshot({ path: `${EVIDENCE}/r24-r1-r2-historical-legacy-recipient.png`, fullPage: true });
  }

  const fa = await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  const en = await page.goto(`${FE}/en`, { waitUntil: "domcontentloaded" });
  rec("fa-rtl", fa.ok(), String(fa.status()));
  rec("en-smoke", en.ok(), String(en.status()));

  await browser.close();
  writeFileSync(`${EVIDENCE}/_r24-r1-r2-visual-raw.json`, JSON.stringify({ checkoutId, historical, log }, null, 2));
  const failed = Object.entries(log).filter(([, v]) => !v.ok);
  if (failed.length) {
    console.error("FAILED", failed);
    process.exit(1);
  }
  console.log("ALL_VISUAL_PASS", checkoutId);
})().catch((error) => {
  console.error(error);
  writeFileSync(`${EVIDENCE}/_r24-r1-r2-visual-raw.json`, JSON.stringify({ error: String(error), log }, null, 2));
  process.exit(1);
});

function pageSnippet(text) {
  return text.replace(/\s+/g, " ").slice(0, 220);
}
