import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T017/screenshots");
const PALETTES = ["tooba-blue", "forest-green", "wine-burgundy", "slate-navy", "amber-gold", "teal-lagoon", "violet-royal"];
const report = { ok: true, steps: [], files: {}, errors: [], console: [], appearance: {}, roles: {}, pages: [], status500: [] };

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
    headers: { Accept: "application/json", Host: "alpha.localhost", ...(init.headers ?? {}) },
  });
  const text = await res.text();
  let body;
  try { body = JSON.parse(text); } catch { body = text.slice(0, 240); }
  return { status: res.status, body };
}

async function admin(path, init = {}) {
  return api(path, {
    ...init,
    headers: { "X-Tooba-Dev-Actor-User-Id": ACTOR, ...(init.headers ?? {}) },
  });
}

async function putAppearance(paletteKey, themeMode, productCardSkin, backgroundStyle) {
  return admin("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin, backgroundStyle }),
  });
}

async function waitMedia(page) {
  await page.waitForTimeout(400);
  await page.evaluate(async () => {
    const imgs = [...document.querySelectorAll("img")].slice(0, 16);
    await Promise.all(imgs.map((img) => {
      if (img.complete && img.naturalWidth > 0) return undefined;
      return new Promise((resolve) => {
        img.addEventListener("load", resolve, { once: true });
        img.addEventListener("error", resolve, { once: true });
        setTimeout(resolve, 3500);
      });
    }));
  });
}

async function waitAdmin(page, testId) {
  await page.getByText("در حال آماده‌سازی پنل مدیریت").waitFor({ state: "hidden", timeout: 30000 }).catch(() => {});
  await page.locator(`[data-testid=${testId}]`).waitFor({ timeout: 20000 }).catch(() => {});
  await page.getByText("در حال بارگذاری").waitFor({ state: "hidden", timeout: 20000 }).catch(() => {});
}

async function shot(page, name) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 2000, { bytes: size });
}

async function open(page, path, expectedStyle) {
  for (let i = 0; i < 4; i += 1) {
    const res = await page.goto(`${FE}${path}`, { waitUntil: "load" });
    if (res && res.status() >= 500) report.status500.push({ path, status: res.status() });
    const style = await page.locator("html").getAttribute("data-storefront-background-style");
    if (!expectedStyle || style === expectedStyle) {
      await waitMedia(page);
      return style;
    }
    await page.waitForTimeout(700);
  }
  await waitMedia(page);
  return page.locator("html").getAttribute("data-storefront-background-style");
}

async function sampleRoles(page) {
  return page.evaluate(() => {
    const read = (sel) => {
      const el = document.querySelector(sel);
      if (!el) return null;
      return getComputedStyle(el).backgroundColor;
    };
    return {
      page: read("[data-storefront-canvas]") || getComputedStyle(document.body).backgroundColor,
      section: read("[data-storefront-surface-role='section']"),
      alternate: read("[data-storefront-surface-role='alternate']"),
      accent: read("[data-storefront-surface-role='accent']"),
      card: read("[data-testid=storefront-product-card]") || read(".bg-white") || read("[data-storefront-product-card]"),
    };
  });
}

(async () => {
  const pages = await admin("/v1/admin/pages");
  const list = Array.isArray(pages.body) ? pages.body : (pages.body?.items || pages.body?.Items || []);
  const campaign = list.find((row) => (row.slug || row.Slug) === "landing-campaign");
  const campaignId = campaign?.pageId || campaign?.PageId || null;
  rec("landing-campaign-id", Boolean(campaignId), { campaignId, count: list.length });

  const homeFeed = await api("/v1/storefront/home?locale=fa");
  const slug = homeFeed.body?.specialOffers?.[0]?.slug
    || homeFeed.body?.newArrivals?.[0]?.slug
    || homeFeed.body?.featuredProducts?.[0]?.slug
    || homeFeed.body?.productRail?.[0]?.slug
    || null;
  rec("pdp-slug", Boolean(slug), { slug });

  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 240) }));
  page.on("console", (msg) => {
    if (msg.type() === "error") report.console.push({ type: "console-error", text: msg.text().slice(0, 240) });
  });
  page.on("response", (res) => {
    if (res.status() >= 500 && /127\.0\.0\.1:3000|127\.0\.0\.1:5088/.test(res.url())) {
      report.status500.push({ url: res.url().slice(0, 180), status: res.status() });
    }
  });

  const neu = await putAppearance("tooba-blue", "LightOnly", "classic", "Neutral");
  rec("neutral-set", neu.status === 200 && neu.body?.backgroundStyle === "Neutral");
  rec("neutral-home", (await open(page, "/fa", "Neutral")) === "Neutral");
  await shot(page, "neutral-home.png");
  rec("neutral-pdp", slug ? (await open(page, `/fa/products/${slug}`, "Neutral")) === "Neutral" : false);
  await shot(page, "neutral-pdp.png");

  const tint = await putAppearance("tooba-blue", "LightOnly", "classic", "PaletteTint");
  rec("tint-set", tint.status === 200 && tint.body?.backgroundStyle === "PaletteTint");
  rec("tinted-home", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
  report.roles = await sampleRoles(page);
  rec("roles-sampled", Boolean(report.roles.page && report.roles.section), report.roles);
  rec("roles-distinct", report.roles.page !== report.roles.alternate && report.roles.section !== report.roles.accent, report.roles);
  await shot(page, "tinted-home.png");
  await shot(page, "01-home-tinted.png");

  rec("02-plp", (await open(page, "/fa/products", "PaletteTint")) === "PaletteTint");
  await shot(page, "02-plp-tinted.png");

  rec("03-pdp", slug ? (await open(page, `/fa/products/${slug}`, "PaletteTint")) === "PaletteTint" : false);
  await shot(page, "03-pdp-tinted.png");
  await shot(page, "tinted-pdp.png");

  rec("04-content", (await open(page, "/fa/blogs", "PaletteTint")) === "PaletteTint");
  await shot(page, "04-content-tinted.png");

  rec("05-cart", (await open(page, "/fa/cart", "PaletteTint")) === "PaletteTint");
  await shot(page, "05-cart-tinted.png");

  rec("06-checkout", (await open(page, "/fa/shipping", "PaletteTint")) === "PaletteTint");
  await shot(page, "06-checkout-tinted.png");

  rec("07-account", (await open(page, "/fa/customer-panel", "PaletteTint")) === "PaletteTint");
  rec("07-wishlist", (await open(page, "/fa/customer-panel/wishlist", "PaletteTint")) === "PaletteTint");
  await shot(page, "07-account-tinted.png");

  rec("08-landing", (await open(page, "/fa/landing-campaign", "PaletteTint")) === "PaletteTint");
  await shot(page, "08-landing-tinted.png");

  if (campaignId) {
    const setHome = await admin("/v1/admin/pages/home", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ homePageId: campaignId }),
    });
    rec("custom-home-set", setHome.status === 200, { status: setHome.status });
    rec("09-custom-home", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
    await shot(page, "09-custom-home-tinted.png");
    const clear = await admin("/v1/admin/pages/home", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ homePageId: null }),
    });
    rec("custom-home-clear", clear.status === 200);
  } else {
    rec("custom-home-set", false);
  }

  for (const key of PALETTES) {
    const put = await putAppearance(key, "LightOnly", "classic", "PaletteTint");
    rec(`palette-${key}`, put.status === 200 && put.body?.paletteKey === key);
    rec(`palette-${key}-home`, (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
    await shot(page, `palette-${key}.png`);
  }

  const darkBlue = await putAppearance("tooba-blue", "DarkOnly", "classic", "PaletteTint");
  rec("dark-blue", darkBlue.status === 200 && darkBlue.body?.themeMode === "DarkOnly");
  rec("dark-home", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
  rec("dark-class-home", await page.locator("html").evaluate((el) => el.classList.contains("dark")));
  await shot(page, "dark-home-tinted.png");
  if (slug) await open(page, `/fa/products/${slug}`, "PaletteTint");
  rec("dark-class-pdp", await page.locator("html").evaluate((el) => el.classList.contains("dark")));
  await shot(page, "dark-pdp-tinted.png");
  rec("dark-landing", (await open(page, "/fa/landing-campaign", "PaletteTint")) === "PaletteTint");
  await shot(page, "dark-landing-tinted.png");

  const darkForest = await putAppearance("forest-green", "DarkOnly", "classic", "PaletteTint");
  rec("dark-forest", darkForest.status === 200 && darkForest.body?.paletteKey === "forest-green");
  rec("dark-forest-home", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");

  await putAppearance("tooba-blue", "LightOnly", "classic", "PaletteTint");
  await page.goto(`${FE}/admin/settings`, { waitUntil: "load" });
  await waitAdmin(page, "admin-settings-page");
  await page.locator("[data-testid=admin-settings-tab-appearance]").click();
  await page.locator("[data-testid=admin-settings-appearance-form]").waitFor({ timeout: 20000 });
  rec("admin-form", await page.locator("[data-testid=admin-settings-appearance-preview]").count() > 0);
  rec("admin-persian-roles",
    (await page.getByText("زمینه صفحه").count()) > 0
    && (await page.getByText("بخش جایگزین").count()) > 0
    && (await page.getByText("بخش برجسته").count()) > 0);
  await page.locator("[data-testid=admin-settings-appearance-preview]").scrollIntoViewIfNeeded().catch(() => {});
  await shot(page, "admin-appearance-final.png");

  const end = await putAppearance("tooba-blue", "LightOnly", "classic", "Neutral");
  rec("restore", end.status === 200 && end.body?.backgroundStyle === "Neutral" && end.body?.paletteKey === "tooba-blue" && end.body?.themeMode === "LightOnly" && end.body?.productCardSkin === "classic");
  const homeClear = await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  rec("restore-home", homeClear.status === 200 && (homeClear.body?.usesCanonicalHome === true || homeClear.body?.UsesCanonicalHome === true || !homeClear.body?.homePageId));

  report.appearance = {
    paletteKey: end.body?.paletteKey,
    themeMode: end.body?.themeMode,
    productCardSkin: end.body?.productCardSkin,
    backgroundStyle: end.body?.backgroundStyle,
  };

  const loops = report.console.filter((row) => /Maximum update depth/i.test(row.text));
  rec("no-console-loop", loops.length === 0, { loops });

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T017/runtime-report.json"), JSON.stringify(report, null, 2));
  if (!report.ok) process.exit(1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T017/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
