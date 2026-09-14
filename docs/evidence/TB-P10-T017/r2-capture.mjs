import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T017/screenshots/r2");
const report = {
  ok: true,
  steps: [],
  files: {},
  errors: [],
  console: [],
  firstStatus: {},
  appearance: {},
  wells: {},
};

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

async function measureWells(page) {
  return page.evaluate(() => {
    const wells = [...document.querySelectorAll("[data-storefront-media-well]")].slice(0, 4);
    return wells.map((well) => {
      const img = well.querySelector("img");
      const wb = well.getBoundingClientRect();
      const ib = img?.getBoundingClientRect();
      return {
        wellH: Math.round(wb.height),
        wellW: Math.round(wb.width),
        aspect: getComputedStyle(well).aspectRatio,
        imgH: img ? Math.round(ib.height) : 0,
        naturalWidth: img?.naturalWidth ?? 0,
        src: String(img?.currentSrc || img?.src || "").slice(0, 140),
      };
    });
  });
}

async function open(page, path, expectedStyle) {
  const res = await page.goto(`${FE}${path}`, { waitUntil: "load" });
  rec(`nav:${path}`, !res || res.status() < 500, { status: res?.status() });
  const style = await page.locator("html").getAttribute("data-storefront-background-style");
  if (expectedStyle) rec(`style:${path}`, style === expectedStyle, { style });
  await page.waitForTimeout(400);
  return style;
}

async function shot(page, name) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 2000, { bytes: size });
}

async function shotCard(page, name) {
  const card = page.locator("[data-testid=storefront-product-card]").first();
  rec(`card-visible:${name}`, await card.count() > 0);
  const dest = join(OUT, name);
  await card.screenshot({ path: dest });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 2000, { bytes: size });
}

(async () => {
  await coldStatus("/fa/products");
  await coldStatus("/fa/landing-campaign");
  await coldStatus("/fa");

  await putAppearance("tooba-blue", "LightOnly", "classic", "PaletteTint");

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  const page = await context.newPage();
  page.on("console", (msg) => {
    if (msg.type() === "error") report.console.push({ type: "console-error", text: msg.text().slice(0, 400) });
  });

  rec("A-plp", (await open(page, "/fa/products", "PaletteTint")) === "PaletteTint");
  report.wells.plp = await measureWells(page);
  rec("B-plp-media", (report.wells.plp[0]?.wellH ?? 0) >= 200, report.wells.plp[0]);
  await shot(page, "plp-tinted-r2.png");

  rec("C-landing", (await open(page, "/fa/landing-campaign", "PaletteTint")) === "PaletteTint");
  report.wells.landing = await measureWells(page);
  rec("D-landing-media", (report.wells.landing[0]?.wellH ?? 0) >= 80, report.wells.landing[0]);
  await shot(page, "landing-tinted-r2.png");

  rec("E-home", (await open(page, "/fa", "PaletteTint")) === "PaletteTint");
  report.wells.home = await measureWells(page);
  rec("E-home-media", (report.wells.home[0]?.wellH ?? 0) >= 80, report.wells.home[0]);

  for (const skin of ["classic", "clean", "elevated", "glass"]) {
    const put = await putAppearance("tooba-blue", "LightOnly", skin, "PaletteTint");
    rec(`F-skin-${skin}`, put.status === 200 && put.body?.productCardSkin === skin, { skin: put.body?.productCardSkin });
    await open(page, "/fa/products", "PaletteTint");
    const wells = await measureWells(page);
    report.wells[skin] = wells;
    rec(`F-well-${skin}`, (wells[0]?.wellH ?? 0) >= 200 && (wells[0]?.naturalWidth ?? 0) > 0, wells[0]);
    await shotCard(page, `product-card-${skin}-r2.png`);
  }

  rec("G-tint", (await page.locator("html").getAttribute("data-storefront-background-style")) === "PaletteTint");

  const end = await putAppearance("tooba-blue", "LightOnly", "classic", "Neutral");
  rec("H-restore", end.status === 200 && end.body?.backgroundStyle === "Neutral" && end.body?.paletteKey === "tooba-blue" && end.body?.themeMode === "LightOnly" && end.body?.productCardSkin === "classic");
  report.appearance = {
    paletteKey: end.body?.paletteKey,
    themeMode: end.body?.themeMode,
    productCardSkin: end.body?.productCardSkin,
    backgroundStyle: end.body?.backgroundStyle,
  };

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T017/r2-runtime-report.json"), JSON.stringify(report, null, 2));
  if (!report.ok) process.exit(1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T017/r2-runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
