import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T017/screenshots/r1");
const report = { ok: true, steps: [], files: {}, errors: [], console: [], authMe: [], firstStatus: {}, appearance: {}, media: {} };

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

async function coldStatus(path) {
  const res = await fetch(`${FE}${path}`, { redirect: "manual" });
  report.firstStatus[path] = res.status;
  rec(`cold:${path}`, res.status === 200, { status: res.status });
  return res.status;
}

async function waitMedia(page, label) {
  const stats = await page.evaluate(async () => {
    const wells = [...document.querySelectorAll("[data-storefront-media-well] img, [data-testid=storefront-pdp] img")].slice(0, 12);
    for (const img of wells) {
      img.scrollIntoView({ block: "center" });
    }
    await Promise.all(wells.map((img) => {
      if (img.complete && img.naturalWidth > 0) return undefined;
      return new Promise((resolve) => {
        img.addEventListener("load", resolve, { once: true });
        img.addEventListener("error", resolve, { once: true });
        setTimeout(resolve, 4000);
      });
    }));
    window.scrollTo(0, 0);
    return wells.map((img) => ({
      src: String(img.currentSrc || img.src).slice(0, 120),
      naturalWidth: img.naturalWidth,
      naturalHeight: img.naturalHeight,
      complete: img.complete,
    }));
  }).catch(() => []);
  report.media[label] = stats;
}

async function shot(page, name) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 2000, { bytes: size });
}

async function open(page, path, expectedStyle) {
  for (let i = 0; i < 3; i += 1) {
    const res = await page.goto(`${FE}${path}`, { waitUntil: "load" });
    rec(`nav:${path}#${i}`, !res || res.status() < 500, { status: res?.status() });
    const style = await page.locator("html").getAttribute("data-storefront-background-style");
    if (!expectedStyle || style === expectedStyle) {
      await waitMedia(page, path);
      return style;
    }
  }
  await waitMedia(page, path);
  return page.locator("html").getAttribute("data-storefront-background-style");
}

(async () => {
  const homeFeed = await api("/v1/storefront/home?locale=fa");
  const slug = homeFeed.body?.specialOffers?.[0]?.slug
    || homeFeed.body?.newArrivals?.[0]?.slug
    || homeFeed.body?.featuredProducts?.[0]?.slug
    || "demo-prod-fashion-men-men-pants-1";
  rec("slug", Boolean(slug), { slug });

  await coldStatus("/fa");
  await coldStatus("/fa/products");
  await coldStatus(`/fa/products/${slug}`);
  await coldStatus("/fa/landing-campaign");
  await coldStatus("/fa/cart");

  await putAppearance("tooba-blue", "LightOnly", "classic", "PaletteTint");
  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  const page = await context.newPage();
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 280) }));
  page.on("console", (msg) => {
    if (msg.type() === "error") report.console.push({ type: "console-error", text: msg.text().slice(0, 280) });
  });
  page.on("response", (res) => {
    if (/\/api\/auth\/me(?:\?|$)/.test(res.url())) {
      report.authMe.push({ status: res.status(), url: res.url() });
    }
  });

  rec("F-home", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
  rec("F-plp", (await open(page, "/fa/products", "PaletteTint")) === "PaletteTint");
  await shot(page, "plp-tinted.png");
  rec("F-pdp", (await open(page, `/fa/products/${slug}`, "PaletteTint")) === "PaletteTint");
  rec("F-pdp-marker", await page.locator("[data-testid=storefront-pdp]").count() > 0);
  await shot(page, "pdp-tinted.png");
  rec("F-landing", (await open(page, "/fa/landing-campaign", "PaletteTint")) === "PaletteTint");
  await shot(page, "landing-tinted.png");
  rec("F-cart", (await open(page, "/fa/cart", "PaletteTint")) === "PaletteTint");

  const dark = await putAppearance("tooba-blue", "DarkOnly", "classic", "PaletteTint");
  rec("G-dark", dark.status === 200 && dark.body?.themeMode === "DarkOnly");
  rec("G-pdp", (await open(page, `/fa/products/${slug}`, "PaletteTint")) === "PaletteTint");
  rec("G-dark-class", await page.locator("html").evaluate((el) => el.classList.contains("dark")));
  await shot(page, "dark-pdp-tinted.png");
  rec("G-landing", (await open(page, "/fa/landing-campaign", "PaletteTint")) === "PaletteTint");
  await shot(page, "dark-landing-tinted.png");

  const end = await putAppearance("tooba-blue", "LightOnly", "classic", "Neutral");
  rec("H-restore", end.status === 200 && end.body?.backgroundStyle === "Neutral" && end.body?.paletteKey === "tooba-blue" && end.body?.themeMode === "LightOnly" && end.body?.productCardSkin === "classic");
  report.appearance = {
    paletteKey: end.body?.paletteKey,
    themeMode: end.body?.themeMode,
    productCardSkin: end.body?.productCardSkin,
    backgroundStyle: end.body?.backgroundStyle,
  };

  const loops = report.console.filter((row) => /Maximum update depth|useTheme must be used/i.test(row.text));
  rec("no-theme-throw", loops.length === 0, { loops });
  rec("auth-me-not-storm", report.authMe.length <= 20, { count: report.authMe.length, statuses: report.authMe.map((x) => x.status) });

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T017/r1-runtime-report.json"), JSON.stringify(report, null, 2));
  if (!report.ok) process.exit(1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T017/r1-runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
