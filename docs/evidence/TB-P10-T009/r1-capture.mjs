import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T009/screenshots");
const PDP = "/fa/products/demo-prod-av-audio-headphones-3";
const report = { ok: true, saves: 0, errors: [], steps: [], files: {}, console: [], hydration: [] };

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, data });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function api(path, init = {}) {
  const res = await fetch(`${HOST}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      Host: "alpha.localhost",
      ...(init.headers ?? {}),
    },
  });
  const text = await res.text();
  let body;
  try { body = JSON.parse(text); } catch { body = text.slice(0, 300); }
  return { status: res.status, body };
}

async function put(paletteKey, themeMode, productCardSkin) {
  report.saves += 1;
  return api("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { "Content-Type": "application/json", "X-Tooba-Dev-Actor-User-Id": ACTOR },
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin }),
  });
}

async function shot(page, name, locator) {
  const dest = join(OUT, name);
  if (locator) {
    await locator.screenshot({ path: dest });
  } else {
    await page.screenshot({ path: dest, fullPage: false });
  }
  const size = statSync(dest).size;
  report.files[name] = { path: dest, bytes: size };
  rec(`file:${name}`, size > 2000, { bytes: size });
}

function extract(html) {
  return {
    palette: html.match(/data-storefront-palette="([^"]+)"/)?.[1] ?? null,
    themeMode: html.match(/data-storefront-theme-mode="([^"]+)"/)?.[1] ?? null,
    scheme: html.match(/data-storefront-color-scheme="([^"]+)"/)?.[1] ?? null,
    skin: html.match(/data-storefront-product-card-skin="([^"]+)"/)?.[1] ?? null,
    cardSkin: html.match(/data-product-card-skin="([^"]+)"/)?.[1] ?? null,
    darkClass: /<html[^>]*class="[^"]*\bdark\b/.test(html),
  };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 1100 },
    locale: "fa-IR",
  });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.on("console", (msg) => {
    const text = msg.text();
    report.console.push({ type: msg.type(), text: text.slice(0, 240) });
    if (/hydrat/i.test(text)) report.hydration.push(text.slice(0, 240));
  });
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 240) }));

  const a = await put("tooba-blue", "LightOnly", "classic");
  rec("A-classic", a.status === 200 && a.body?.productCardSkin === "classic", a.body);

  await page.goto(`${FE}/fa`, { waitUntil: "networkidle", timeout: 60000 });
  const homeCard = page.locator('[data-testid="storefront-product-card"]').first();
  await homeCard.waitFor({ timeout: 30000 });
  await homeCard.scrollIntoViewIfNeeded();
  await shot(page, "classic.png", homeCard);

  await page.goto(`${FE}/fa/admin/settings`, { waitUntil: "networkidle", timeout: 60000 });
  await page.locator('[data-testid="admin-settings-tab-appearance"]').click();
  await page.locator('[data-testid="admin-settings-appearance-form"]').waitFor({ timeout: 20000 });
  const form = page.locator('[data-testid="admin-settings-appearance-form"]');
  await shot(page, "admin-skin-default.png", form);

  await page.locator('[data-testid="admin-settings-appearance-skin-clean"]').click();
  await page.waitForTimeout(400);
  rec("admin-dirty", await page.locator('[data-testid="admin-settings-save-appearance"]').isEnabled(), {});
  await shot(page, "admin-skin-preview.png", form);

  await page.locator('[data-testid="admin-settings-save-appearance"]').click();
  await page.waitForTimeout(800);
  rec("admin-saved-ui", await page.locator('[data-testid="admin-settings-save-appearance"]').isDisabled(), {});
  await shot(page, "admin-skin-saved.png", form);
  report.saves += 1;

  const afterAdmin = await api("/v1/storefront/appearance");
  rec("admin-saved-api", afterAdmin.body?.productCardSkin === "clean", afterAdmin.body);

  await page.goto(`${FE}/fa`, { waitUntil: "networkidle", timeout: 60000 });
  await homeCard.waitFor({ timeout: 30000 });
  await homeCard.scrollIntoViewIfNeeded();
  await shot(page, "clean.png", page.locator('[data-testid="storefront-product-card"]').first());

  const elevated = await put("tooba-blue", "LightOnly", "elevated");
  rec("C-elevated", elevated.status === 200 && elevated.body?.productCardSkin === "elevated", elevated.body);
  await page.reload({ waitUntil: "networkidle" });
  await page.locator('[data-testid="storefront-product-card"]').first().waitFor({ timeout: 30000 });
  await page.locator('[data-testid="storefront-product-card"]').first().scrollIntoViewIfNeeded();
  await shot(page, "elevated.png", page.locator('[data-testid="storefront-product-card"]').first());

  const glass = await put("tooba-blue", "LightOnly", "glass");
  rec("D-glass", glass.status === 200 && glass.status === 200, glass.body);
  await page.reload({ waitUntil: "networkidle" });
  await page.locator('[data-testid="storefront-product-card"]').first().waitFor({ timeout: 30000 });
  await page.locator('[data-testid="storefront-product-card"]').first().scrollIntoViewIfNeeded();
  await shot(page, "glass.png", page.locator('[data-testid="storefront-product-card"]').first());

  const cons = await put("tooba-blue", "LightOnly", "clean");
  rec("G-clean-for-surfaces", cons.status === 200, cons.body);
  await page.reload({ waitUntil: "networkidle" });
  await page.locator('[data-testid="storefront-product-card"]').first().waitFor({ timeout: 30000 });
  await page.locator('[data-testid="storefront-product-card"]').first().scrollIntoViewIfNeeded();
  await shot(page, "skin-home.png", page.locator('[data-testid="storefront-product-card"]').first());
  rec("home-marker", extract(await page.content()).skin === "clean", extract(await page.content()));

  await page.goto(`${FE}/fa/products`, { waitUntil: "networkidle", timeout: 60000 });
  const plpCard = page.locator('[data-testid="storefront-product-card"]').first();
  await plpCard.waitFor({ timeout: 30000 });
  await plpCard.scrollIntoViewIfNeeded();
  await shot(page, "skin-plp.png", plpCard);
  rec("plp-marker", extract(await page.content()).skin === "clean", extract(await page.content()));

  await page.goto(`${FE}${PDP}`, { waitUntil: "networkidle", timeout: 60000 });
  const related = page.locator('[data-testid="storefront-product-card"]').first();
  const relatedCount = await page.locator('[data-testid="storefront-product-card"]').count();
  rec("pdp-related-available", relatedCount > 0, { relatedCount });
  if (relatedCount > 0) {
    await related.scrollIntoViewIfNeeded();
    await shot(page, "skin-pdp-related.png", related);
  }

  await page.goto(`${FE}/fa/cart`, { waitUntil: "networkidle", timeout: 60000 });
  const recs = page.locator('[data-testid="cart-recommendations"] [data-testid="storefront-product-card"]').first();
  const recCount = await page.locator('[data-testid="cart-recommendations"] [data-testid="storefront-product-card"]').count();
  rec("cart-recs-available", recCount > 0, { recCount });
  if (recCount > 0) {
    await recs.scrollIntoViewIfNeeded();
    await shot(page, "skin-cart-recommendation.png", recs);
  }

  const dark = await put("tooba-blue", "DarkOnly", "glass");
  rec("E-dark-glass", dark.status === 200 && dark.body?.themeMode === "DarkOnly", dark.body);
  await page.goto(`${FE}/fa`, { waitUntil: "networkidle", timeout: 60000 });
  await page.locator('[data-testid="storefront-product-card"]').first().waitFor({ timeout: 30000 });
  await page.locator('[data-testid="storefront-product-card"]').first().scrollIntoViewIfNeeded();
  await shot(page, "skin-dark.png", page.locator('[data-testid="storefront-product-card"]').first());
  rec("dark-marker", extract(await page.content()).darkClass === true, extract(await page.content()));

  const alt = await put("forest-green", "LightOnly", "elevated");
  rec("F-alt-palette", alt.status === 200 && alt.body?.paletteKey === "forest-green", alt.body);
  await page.reload({ waitUntil: "networkidle" });
  await page.locator('[data-testid="storefront-product-card"]').first().waitFor({ timeout: 30000 });
  await page.locator('[data-testid="storefront-product-card"]').first().scrollIntoViewIfNeeded();
  await shot(page, "skin-alt-palette.png", page.locator('[data-testid="storefront-product-card"]').first());

  const both = await put("forest-green", "DarkOnly", "glass");
  rec("F-dark-alt", both.status === 200, both.body);
  await page.reload({ waitUntil: "networkidle" });
  await page.locator('[data-testid="storefront-product-card"]').first().waitFor({ timeout: 30000 });
  await page.locator('[data-testid="storefront-product-card"]').first().scrollIntoViewIfNeeded();
  await shot(page, "skin-dark-alt-palette.png", page.locator('[data-testid="storefront-product-card"]').first());

  const restore = await put("tooba-blue", "LightOnly", "classic");
  rec("H-restore", restore.status === 200 && restore.body?.productCardSkin === "classic" && restore.body?.paletteKey === "tooba-blue", restore.body);
  await page.reload({ waitUntil: "networkidle" });
  await page.locator('[data-testid="storefront-product-card"]').first().waitFor({ timeout: 30000 });
  await page.locator('[data-testid="storefront-product-card"]').first().scrollIntoViewIfNeeded();
  await shot(page, "classic-restored.png", page.locator('[data-testid="storefront-product-card"]').first());
  rec("restore-marker", extract(await page.content()).skin === "classic" && !extract(await page.content()).darkClass, extract(await page.content()));

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T009/r1-runtime-raw.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, saves: report.saves, files: Object.keys(report.files) }, null, 2));
  if (!report.ok) process.exit(1);
})().catch((error) => {
  report.ok = false;
  report.errors.push(String(error));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T009/r1-runtime-raw.json"), JSON.stringify(report, null, 2));
  console.error(error);
  process.exit(1);
});
