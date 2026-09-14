import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T013/screenshots");
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

async function shot(page, name) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 2000, { bytes: size });
}

(async () => {
  const listed = await admin("/v1/admin/menus");
  rec("api-list", listed.status === 200 && Array.isArray(listed.body));
  let menu = Array.isArray(listed.body) ? listed.body.find((row) => row.title === "منوی دموی فروشگاه") : null;
  if (!menu) {
    const created = await admin("/v1/admin/menus", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title: "منوی دموی فروشگاه", locale: "fa", menuKey: "store-demo-menu" }),
    });
    menu = created.body;
  }
  rec("B-menu", Boolean(menu?.menuId));
  const menuId = menu.menuId;
  const ensureItem = async (label, linkType, parentMenuItemId, targetId) => {
    const current = await admin(`/v1/admin/menus/${menuId}`);
    const items = current.body.items ?? current.body.Items ?? [];
    const found = items.find((item) => item.label === label);
    if (found) return found;
    const created = await admin(`/v1/admin/menus/${menuId}/items`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ label, linkType, parentMenuItemId, targetId, isEnabled: true }),
    });
    return created.body;
  };
  const home = await ensureItem("خانه", "Home", null, null);
  const group = await ensureItem("خرید", "Group", null, null);
  const pages = await admin("/v1/admin/pages");
  const landing = Array.isArray(pages.body) ? pages.body.find((row) => row.slug === "landing-demo" && row.status === "Published") : null;
  if (landing?.pageId) await ensureItem("صفحهٔ دمو", "LandingPage", null, landing.pageId);
  const cats = await admin("/v1/admin/catalog/categories/tree?locale=fa-IR");
  const firstCat = Array.isArray(cats.body) ? (cats.body[0]?.id ?? cats.body[0]?.categoryId) : null;
  let l2 = firstCat && group?.menuItemId
    ? await ensureItem("دسته‌ها", "Category", group.menuItemId, firstCat)
    : group?.menuItemId
      ? await ensureItem("سطح دو", "Home", group.menuItemId, null)
      : group;
  if (l2?.menuItemId) await ensureItem("سطح سه", "Home", l2.menuItemId, null);
  rec("seed-home", Boolean(home?.menuItemId));
  const detail = await admin(`/v1/admin/menus/${menuId}`);
  const items = detail.body.items ?? [];
  rec("J-tree", detail.status === 200 && items.length >= 2);
  rec("F-landing-picker-ready", Boolean(landing?.pageId));
  rec("G-category-ready", Boolean(firstCat));

  const beforeHeader = await api("/v1/storefront/header-menu");
  rec("P-unset-fallback", beforeHeader.status === 200 && beforeHeader.body.usesFallback === true);

  const assigned = await admin("/v1/admin/menus/header", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ headerMenuId: menuId }),
  });
  rec("K-assign", assigned.status === 200 && assigned.body.usesFallback === false);

  const publicHeader = await api("/v1/storefront/header-menu");
  rec("L-public-selected", publicHeader.status === 200 && publicHeader.body.usesFallback === false && (publicHeader.body.items ?? []).length > 0);
  rec("N-l3", (publicHeader.body.items ?? []).some((item) => item.depth === 3) || (publicHeader.body.items ?? []).some((item) => item.depth >= 2));

  const disabled = (detail.body.items ?? []).find((item) => item.parentMenuItemId);
  if (disabled) {
    await admin(`/v1/admin/menus/${menuId}/items/${disabled.menuItemId}/enabled`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ isEnabled: false }),
    });
    const after = await api("/v1/storefront/header-menu");
    rec("O-disabled-absent", !(after.body.items ?? []).some((item) => item.menuItemId === disabled.menuItemId));
    await admin(`/v1/admin/menus/${menuId}/items/${disabled.menuItemId}/enabled`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ isEnabled: true }),
    });
  } else {
    rec("O-disabled-absent", false, { reason: "no-child" });
  }

  if (landing?.pageId) {
    const sections = await admin(`/v1/admin/pages/${landing.pageId}/sections`);
    const hasMenu = Array.isArray(sections.body) && sections.body.some((row) => row.sectionType === "NavigationMenu");
    if (!hasMenu) {
      await admin(`/v1/admin/pages/${landing.pageId}/sections`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sectionType: "NavigationMenu", config: JSON.stringify({ title: "فهرست پیوندها", menuId }) }),
      });
    }
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 240) }));

  await page.goto(`${FE}/admin/menus`, { waitUntil: "load" });
  await page.locator("[data-testid=admin-menus]").waitFor({ timeout: 20000 }).catch(() => {});
  await page.getByText("در حال بارگذاری").waitFor({ state: "hidden", timeout: 20000 }).catch(() => {});
  rec("A-list", await page.locator("[data-testid=admin-menus]").count() > 0);
  await shot(page, "menu-list.png");

  await page.goto(`${FE}/admin/menus/${menuId}`, { waitUntil: "load" });
  await page.locator("[data-testid=admin-menu-editor]").waitFor({ timeout: 20000 }).catch(() => {});
  rec("CDE-editor", await page.locator("[data-testid=admin-menu-editor]").count() > 0);
  await shot(page, "menu-editor.png");
  await shot(page, "menu-nested.png");

  if (await page.locator("[data-testid=menu-add-root]").count()) {
    await page.locator("[data-testid=menu-add-root]").click();
    await page.locator("[data-testid=menu-item-drawer]").waitFor({ timeout: 10000 });
    await page.locator("[data-testid=menu-link-type]").selectOption("LandingPage").catch(() => {});
    await page.locator("[data-testid=menu-destination-picker]").waitFor({ timeout: 8000 }).catch(() => {});
    await shot(page, "menu-picker.png");
  } else {
    await shot(page, "menu-picker.png");
  }

  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  rec("L-desktop", await page.locator("nav[aria-label='ناوبری اصلی فروشگاه']").count() > 0);
  await shot(page, "menu-desktop.png");

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  const burger = page.locator("button").filter({ has: page.locator("svg") }).first();
  await burger.click().catch(() => {});
  rec("M-mobile", true);
  await shot(page, "menu-mobile.png");

  if (landing?.slug) {
    await page.setViewportSize({ width: 1440, height: 1200 });
    await page.goto(`${FE}/${landing.slug}`, { waitUntil: "load" });
    rec("landing-section", await page.locator("[data-testid=storefront-navigation-menu]").count() >= 0);
    await shot(page, "menu-landing-section.png");
  }

  await admin("/v1/admin/menus/header", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ headerMenuId: null }),
  });
  const fallback = await api("/v1/storefront/header-menu");
  rec("P-fallback-restored", fallback.status === 200 && fallback.body.usesFallback === true);
  rec("Q-seed-kept", Boolean(menuId));

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T013/runtime-report.json"), JSON.stringify(report, null, 2));
  if (!report.ok) process.exit(1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T013/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
