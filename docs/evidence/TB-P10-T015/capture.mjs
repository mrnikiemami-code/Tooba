import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T015/screenshots");
const report = { ok: true, steps: [], files: {}, errors: [], console: [], appearance: {} };

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

async function putAppearance(paletteKey, themeMode, productCardSkin, backgroundStyle) {
  return admin("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin, backgroundStyle }),
  });
}

async function openHome(page, expectedStyle) {
  for (let i = 0; i < 4; i += 1) {
    await page.goto(`${FE}/fa`, { waitUntil: "load" });
    const style = await page.locator("html").getAttribute("data-storefront-background-style");
    if (style === expectedStyle) {
      return style;
    }
    await page.waitForTimeout(800);
  }
  return page.locator("html").getAttribute("data-storefront-background-style");
}

(async () => {
  const invalid = await putAppearance("tooba-blue", "LightOnly", "classic", "NeonWash");
  rec("invalid-background-400", invalid.status === 400 && invalid.body?.errorCode === "appearance.background.invalid");

  const restored = await putAppearance("tooba-blue", "LightOnly", "classic", "Neutral");
  rec("A-default-neutral", restored.status === 200 && restored.body?.backgroundStyle === "Neutral" && restored.body?.paletteKey === "tooba-blue");
  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });

  const pub = await api("/v1/storefront/appearance");
  rec("public-neutral", pub.status === 200 && pub.body?.backgroundStyle === "Neutral");
  rec("public-tint-tokens", pub.status === 200 && pub.body?.tint?.pageBackgroundRgb && pub.body?.tint?.pageBackgroundRgb !== pub.body?.tokens?.primaryRgb);

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

  await page.goto(`${FE}/admin/settings`, { waitUntil: "load" });
  await waitAdmin(page, "admin-settings-page");
  await page.locator("[data-testid=admin-settings-tab-appearance]").click();
  await page.locator("[data-testid=admin-settings-appearance-form]").waitFor({ timeout: 20000 });
  rec("A-open-appearance", await page.locator("[data-testid=admin-settings-appearance-backgrounds]").count() > 0);
  rec("B-default-neutral-ui", await page.locator("[data-testid=admin-settings-appearance-background-Neutral]").getAttribute("data-selected") === "true");
  await page.locator("[data-testid=admin-settings-appearance-backgrounds]").scrollIntoViewIfNeeded();
  await shot(page, "appearance-background-style.png");

  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  rec("D-home-neutral-marker", await page.locator("[data-storefront-canvas]").count() > 0);
  rec("D-home-neutral-attr", (await openHome(page, "Neutral")) === "Neutral");
  await shot(page, "home-neutral.png");

  const tintBlue = await putAppearance("tooba-blue", "LightOnly", "classic", "PaletteTint");
  rec("C-save-blue-tint", tintBlue.status === 200 && tintBlue.body?.backgroundStyle === "PaletteTint");

  rec("D-home-tinted-blue", (await openHome(page, "PaletteTint")) === "PaletteTint");
  await shot(page, "home-tinted-blue.png");

  await page.goto(`${FE}/landing-campaign`, { waitUntil: "load" });
  rec("E-landing-tinted", await page.locator("[data-testid=storefront-landing-page]").count() > 0);
  rec("E-landing-inherits", await page.locator("html").getAttribute("data-storefront-background-style") === "PaletteTint");
  await shot(page, "landing-tinted.png");

  const tintForest = await putAppearance("forest-green", "LightOnly", "classic", "PaletteTint");
  rec("F-forest-tint", tintForest.status === 200 && tintForest.body?.paletteKey === "forest-green");
  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  rec("G-home-forest", await page.locator("html").getAttribute("data-storefront-palette") === "forest-green");
  await shot(page, "home-tinted-forest.png");

  const darkTint = await putAppearance("forest-green", "DarkOnly", "classic", "PaletteTint");
  rec("H-dark-tint", darkTint.status === 200 && darkTint.body?.themeMode === "DarkOnly");
  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  rec("H-dark-class", await page.locator("html").evaluate((el) => el.classList.contains("dark")));
  await shot(page, "home-dark-tinted.png");

  const end = await putAppearance("tooba-blue", "LightOnly", "classic", "Neutral");
  rec("I-restore-neutral", end.status === 200 && end.body?.backgroundStyle === "Neutral" && end.body?.paletteKey === "tooba-blue" && end.body?.themeMode === "LightOnly" && end.body?.productCardSkin === "classic");
  const homeClear = await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  rec("J-restore-home", homeClear.status === 200 && (homeClear.body?.usesCanonicalHome === true || homeClear.body?.UsesCanonicalHome === true || !homeClear.body?.homePageId));

  report.appearance = {
    paletteKey: end.body?.paletteKey,
    themeMode: end.body?.themeMode,
    productCardSkin: end.body?.productCardSkin,
    backgroundStyle: end.body?.backgroundStyle,
  };

  const loops = report.console.filter((row) => /Maximum update depth|hydration|Hydration/i.test(row.text));
  rec("no-console-loop", loops.length === 0, { loops });

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T015/runtime-report.json"), JSON.stringify(report, null, 2));
  if (!report.ok) process.exit(1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T015/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
