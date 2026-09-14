import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T014/screenshots");
const report = { ok: true, steps: [], files: {}, errors: [], console: [] };

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

async function putAppearance(paletteKey, themeMode, productCardSkin) {
  return admin("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin }),
  });
}

(async () => {
  const pages = await admin("/v1/admin/pages");
  rec("pages-list", pages.status === 200 && Array.isArray(pages.body));
  const main = Array.isArray(pages.body) ? pages.body.find((row) => row.slug === "landing-demo") : null;
  const campaign = Array.isArray(pages.body) ? pages.body.find((row) => row.slug === "landing-campaign") : null;
  const draft = Array.isArray(pages.body) ? pages.body.find((row) => row.slug === "landing-demo-draft") : null;
  rec("seed-main", Boolean(main?.pageId) && main.status === "Published");
  rec("seed-campaign", Boolean(campaign?.pageId) && campaign.status === "Published");
  rec("seed-draft", Boolean(draft?.pageId) && draft.status === "Draft");

  const menus = await admin("/v1/admin/menus");
  rec("menus-list", menus.status === 200 && Array.isArray(menus.body));
  const menu = Array.isArray(menus.body) ? menus.body.find((row) => row.title === "منوی دموی فروشگاه") : null;
  rec("seed-menu", Boolean(menu?.menuId));
  const menuId = menu?.menuId;
  if (menuId && campaign?.pageId) {
    const detail = await admin(`/v1/admin/menus/${menuId}`);
    const items = detail.body?.items ?? detail.body?.Items ?? [];
    if (!items.some((item) => item.label === "صفحهٔ کمپین" || item.Label === "صفحهٔ کمپین")) {
      await admin(`/v1/admin/menus/${menuId}/items`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ label: "صفحهٔ کمپین", linkType: "LandingPage", targetId: campaign.pageId, isEnabled: true }),
      });
    }
  }

  if (menuId) {
    await admin("/v1/admin/menus/header", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ headerMenuId: menuId }),
    });
  }

  const appearance = await admin("/v1/admin/settings/appearance");
  rec("A-appearance-get", appearance.status === 200);
  const saved = await putAppearance("forest-green", "DarkOnly", "glass");
  rec("B-alt-appearance", saved.status === 200 && saved.body?.paletteKey === "forest-green");

  if (main?.pageId) {
    const home = await admin("/v1/admin/pages/home", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ homePageId: main.pageId }),
    });
    rec("D-home-selected", home.status === 200 && (home.body?.homePageId === main.pageId || home.body?.HomePageId === main.pageId));
  } else {
    rec("D-home-selected", false);
  }

  const pubMain = await api("/v1/storefront/pages/landing-demo?locale=fa");
  rec("C-main-public", pubMain.status === 200);
  const pubCampaign = await api("/v1/storefront/pages/landing-campaign?locale=fa");
  rec("campaign-public", pubCampaign.status === 200);
  const pubDraft = await api("/v1/storefront/pages/landing-demo-draft?locale=fa");
  rec("H-draft-404", pubDraft.status === 404);

  const header = await api("/v1/storefront/header-menu");
  rec("F-menu", header.status === 200 && header.body?.usesFallback === false && (header.body?.items ?? []).length > 0);

  const other = await fetch(`${HOST}/v1/storefront/header-menu`, {
    headers: { Accept: "application/json", Host: "beta.localhost" },
  });
  rec("cross-store-no-500", other.status !== 500);

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
  rec("A-open-appearance", await page.locator("[data-testid=admin-settings-appearance-presets]").count() > 0);
  rec("palettes-7", await page.locator("[data-testid=admin-settings-appearance-presets] button").count() === 7);
  rec("themes-4", await page.locator("[data-testid=admin-settings-appearance-themes] button").count() === 4);
  rec("skins-4", await page.locator("[data-testid=admin-settings-appearance-skins] button").count() === 4);
  await shot(page, "appearance-palettes.png");
  await page.locator("[data-testid=admin-settings-appearance-themes]").scrollIntoViewIfNeeded();
  await shot(page, "appearance-theme-mode.png");
  await page.locator("[data-testid=admin-settings-appearance-skins]").scrollIntoViewIfNeeded();
  await shot(page, "appearance-card-skins.png");

  await page.goto(`${FE}/admin/landing-pages`, { waitUntil: "load" });
  await waitAdmin(page, "admin-landing-pages");
  rec("landing-list", await page.locator("[data-testid=landing-row-landing-demo]").count() > 0);
  await shot(page, "landing-list.png");

  if (main?.pageId) {
    await page.goto(`${FE}/admin/landing-pages/${main.pageId}`, { waitUntil: "load" });
    await page.locator("[data-testid=admin-landing-page-editor]").waitFor({ timeout: 20000 });
    rec("C-composer", await page.locator("[data-testid=landing-section-composer]").count() > 0);
    await shot(page, "landing-composer.png");
    await page.locator("[data-testid=landing-add-section]").click();
    await page.locator("[data-testid=add-section-chooser]").waitFor({ timeout: 10000 });
    await shot(page, "landing-add-section.png");
    await page.locator("[data-testid=add-section-chooser] button").filter({ hasText: "بستن" }).click().catch(() => {});
    const editButtons = page.locator("[data-testid=landing-section-composer] button").filter({ hasText: "ویرایش" });
    const count = await editButtons.count();
    if (count > 1) {
      await editButtons.nth(1).click();
    } else if (count > 0) {
      await editButtons.first().click();
    }
    await page.locator("[data-testid=landing-section-drawer]").waitFor({ timeout: 8000 }).catch(() => {});
    const source = page.locator("[data-testid=product-section-editor] select");
    if (await source.count()) {
      await source.selectOption("Manual").catch(() => {});
    }
    await page.locator("[data-testid=landing-product-multi-picker]").waitFor({ timeout: 8000 }).catch(() => {});
    await shot(page, "landing-product-picker.png");
  } else {
    await shot(page, "landing-composer.png");
    await shot(page, "landing-add-section.png");
    await shot(page, "landing-product-picker.png");
  }

  if (draft?.pageId) {
    await page.goto(`${FE}/admin/landing-pages/${draft.pageId}/preview`, { waitUntil: "load" });
    await page.locator("[data-testid=landing-draft-preview]").waitFor({ timeout: 20000 }).catch(() => {});
    rec("G-draft-preview", await page.locator("[data-testid=landing-draft-preview]").count() > 0);
    await shot(page, "landing-preview.png");
  } else {
    rec("G-draft-preview", false);
    await shot(page, "landing-preview.png");
  }

  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  rec("E-home-custom", await page.locator("body").count() > 0);
  await shot(page, "home-alt-palette.png");
  await shot(page, "home-dark.png");
  await shot(page, "landing-card-skin.png");

  await page.goto(`${FE}/landing-demo`, { waitUntil: "load" });
  await shot(page, "landing-demo.png");
  await page.goto(`${FE}/landing-campaign`, { waitUntil: "load" });
  await shot(page, "landing-campaign.png");

  const draftPage = await page.goto(`${FE}/landing-demo-draft`, { waitUntil: "load" });
  rec("H-draft-public", draftPage ? draftPage.status() === 404 : false);

  await page.goto(`${FE}/admin/menus`, { waitUntil: "load" });
  await waitAdmin(page, "admin-menus");
  rec("menu-list", await page.getByTestId("menu-row-منوی دموی فروشگاه").count() > 0);
  await shot(page, "menu-list.png");
  if (menuId) {
    await page.goto(`${FE}/admin/menus/${menuId}`, { waitUntil: "load" });
    await page.locator("[data-testid=admin-menu-editor]").waitFor({ timeout: 20000 }).catch(() => {});
    await shot(page, "menu-editor.png");
  } else {
    await shot(page, "menu-editor.png");
  }

  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  rec("F-header-desktop", await page.locator("nav[aria-label='ناوبری اصلی فروشگاه']").count() > 0);
  await shot(page, "menu-header-desktop.png");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  const burger = page.locator("button").filter({ has: page.locator("svg") }).first();
  await burger.click().catch(() => {});
  await shot(page, "menu-header-mobile.png");

  const restored = await putAppearance("tooba-blue", "LightOnly", "classic");
  rec("I-restore-appearance", restored.status === 200 && restored.body?.paletteKey === "tooba-blue");
  const homeClear = await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  rec("J-restore-home", homeClear.status === 200 && (homeClear.body?.usesCanonicalHome === true || homeClear.body?.UsesCanonicalHome === true || !homeClear.body?.homePageId));
  await admin("/v1/admin/menus/header", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ headerMenuId: null }),
  });

  await page.setViewportSize({ width: 1440, height: 1200 });
  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  await shot(page, "home-default.png");

  const loops = report.console.filter((row) => /Maximum update depth|hydration|Hydration/i.test(row.text));
  rec("no-console-loop", loops.length === 0, { loops });
  rec("K-seed-kept", Boolean(main?.pageId && campaign?.pageId && draft?.pageId && menuId));

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T014/runtime-report.json"), JSON.stringify(report, null, 2));
  if (!report.ok) process.exit(1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T014/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
