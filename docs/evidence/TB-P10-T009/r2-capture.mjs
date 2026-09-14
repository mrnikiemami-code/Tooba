import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T009/screenshots");
const report = { ok: true, saves: 0, errors: [], steps: [], files: {}, console: [], hydration: [], maxUpdate: [], appearanceGets: 0, appearancePuts: 0 };

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
  if (locator) await locator.screenshot({ path: dest });
  else await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 2000, { bytes: size });
}

function extract(html) {
  return {
    palette: html.match(/data-storefront-palette="([^"]+)"/)?.[1] ?? null,
    themeMode: html.match(/data-storefront-theme-mode="([^"]+)"/)?.[1] ?? null,
    skin: html.match(/data-storefront-product-card-skin="([^"]+)"/)?.[1] ?? null,
    leakedKeys: ["classic", "clean", "elevated", "glass", "LightOnly", "DarkOnly", "tooba-blue"].filter((key) => {
      const re = new RegExp(`>(\\s*)${key}(\\s*)<`, "i");
      return re.test(html);
    }),
  };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.on("console", (msg) => {
    const text = msg.text();
    const item = { type: msg.type(), text: text.slice(0, 400) };
    report.console.push(item);
    if (/hydrat/i.test(text)) report.hydration.push(text.slice(0, 400));
    if (/Maximum update depth/i.test(text)) report.maxUpdate.push(text.slice(0, 400));
  });
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 400) }));
  page.on("request", (req) => {
    const url = req.url();
    if (url.includes("/v1/admin/settings/appearance") || url.includes("/v1/storefront/appearance")) {
      if (req.method() === "GET") report.appearanceGets += 1;
      if (req.method() === "PUT") report.appearancePuts += 1;
    }
  });

  const restore = await put("tooba-blue", "LightOnly", "classic");
  rec("prep-classic", restore.status === 200 && restore.body?.productCardSkin === "classic", restore.body);

  await page.goto(`${FE}/fa/admin/settings`, { waitUntil: "networkidle", timeout: 60000 });
  await page.locator('[data-testid="admin-settings-tab-appearance"]').click();
  const form = page.locator('[data-testid="admin-settings-appearance-form"]');
  await form.waitFor({ timeout: 20000 });
  const adminHtml = await form.innerHTML();
  const visibleAdmin = (await form.innerText()).replace(/\s+/g, " ");
  rec("no-visible-skin-keys", !/\bclassic\b|\bclean\b|\belevated\b|\bglass\b|\bLightOnly\b|\btooba-blue\b/i.test(visibleAdmin), { visibleAdmin: visibleAdmin.slice(0, 240) });
  rec("has-persian-copy", adminHtml.includes("کلاسیک") && adminHtml.includes("ساده") && adminHtml.includes("برجسته") && adminHtml.includes("شیشه‌ای"), {});
  rec("has-mini-title", adminHtml.includes("هدفون بی‌سیم"), {});
  await shot(page, "admin-skin-default-r2.png", form);

  await page.locator('[data-testid="admin-settings-appearance-skin-clean"]').click();
  await page.locator('[data-testid="admin-settings-appearance-theme-DarkOnly"]').click();
  rec("dirty-enabled", await page.locator('[data-testid="admin-settings-save-appearance"]').isEnabled(), {});
  await shot(page, "admin-skin-preview-r2.png", form);
  await page.locator('[data-testid="admin-settings-cancel-appearance"]').click();
  rec("cancel-restored", await page.locator('[data-testid="admin-settings-save-appearance"]').isDisabled(), {});
  await page.locator('[data-testid="admin-settings-appearance-skin-elevated"]').click();
  await page.locator('[data-testid="admin-settings-save-appearance"]').click();
  await page.waitForTimeout(700);
  rec("saved-disabled", await page.locator('[data-testid="admin-settings-save-appearance"]').isDisabled(), {});
  await shot(page, "admin-skin-saved-r2.png", form);

  const after = await api("/v1/storefront/appearance");
  rec("saved-elevated", after.body?.productCardSkin === "elevated" && after.body?.paletteKey === "tooba-blue", after.body);

  for (const [skin, file] of [["classic", "classic-r2.png"], ["clean", "clean-r2.png"], ["elevated", "elevated-r2.png"], ["glass", "glass-r2.png"]]) {
    const saved = await put("tooba-blue", "LightOnly", skin);
    rec(`skin-${skin}`, saved.status === 200 && saved.body?.productCardSkin === skin, saved.body);
    await page.goto(`${FE}/fa`, { waitUntil: "networkidle", timeout: 60000 });
    const card = page.locator('[data-testid="storefront-product-card"]').first();
    await card.waitFor({ timeout: 30000 });
    await card.scrollIntoViewIfNeeded();
    rec(`ssr-${skin}`, extract(await page.content()).skin === skin, extract(await page.content()));
    await shot(page, file, card);
  }

  const dark = await put("tooba-blue", "DarkOnly", "glass");
  rec("dark-glass", dark.status === 200, dark.body);
  await page.reload({ waitUntil: "networkidle" });
  rec("dark-ssr", extract(await page.content()).skin === "glass", extract(await page.content()));

  const alt = await put("forest-green", "LightOnly", "elevated");
  rec("alt-palette", alt.status === 200 && alt.body?.paletteKey === "forest-green", alt.body);
  await page.goto(`${FE}/fa/products`, { waitUntil: "networkidle", timeout: 60000 });
  rec("plp-skin", extract(await page.content()).skin === "elevated", extract(await page.content()));

  const back = await put("tooba-blue", "LightOnly", "classic");
  rec("restore", back.status === 200 && back.body?.productCardSkin === "classic", back.body);

  const invalid = await put("tooba-blue", "LightOnly", "custom-html");
  rec("invalid-400", invalid.status === 400, { status: invalid.status });

  rec("no-max-update", report.maxUpdate.length === 0, report.maxUpdate);
  rec("no-pageerror", report.console.filter((item) => item.type === "pageerror").length === 0, {});
  rec("no-task-hydration", report.hydration.filter((text) => /product-card-skin|storefront-palette|appearance/i.test(text)).length === 0, report.hydration);

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T009/r2-runtime-raw.json"), JSON.stringify({
    ok: report.ok,
    errors: report.errors,
    saves: report.saves,
    appearanceGets: report.appearanceGets,
    appearancePuts: report.appearancePuts,
    files: report.files,
    steps: report.steps,
    maxUpdate: report.maxUpdate,
    hydration: report.hydration,
    console: report.console.filter((item) => item.type === "error" || item.type === "pageerror" || /hydrat|Maximum update/i.test(item.text)),
  }, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files), maxUpdate: report.maxUpdate.length, hydration: report.hydration.length }, null, 2));
  if (!report.ok) process.exit(1);
})().catch((error) => {
  report.ok = false;
  report.errors.push(String(error));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T009/r2-runtime-raw.json"), JSON.stringify(report, null, 2));
  console.error(error);
  process.exit(1);
});
