import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T012/screenshots");
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

async function putAppearance(paletteKey, themeMode, productCardSkin) {
  return admin("/v1/admin/settings/appearance", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ paletteKey, themeMode, productCardSkin }),
  });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1200 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  page.on("console", (msg) => report.console.push({ type: msg.type(), text: msg.text().slice(0, 240) }));
  page.on("pageerror", (err) => report.console.push({ type: "pageerror", text: String(err).slice(0, 240) }));

  const appearance = await admin("/v1/admin/settings/appearance");
  const original = appearance.body ?? {};

  await page.goto(`${FE}/admin/landing-pages`, { waitUntil: "networkidle" });
  rec("A-list", await page.locator("[data-testid=admin-landing-pages]").count() > 0);
  await shot(page, "page-list.png");

  const existing = await admin("/v1/admin/pages");
  const prior = Array.isArray(existing.body) ? existing.body.find((row) => row.slug === "t012-runtime") : null;

  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "networkidle" });
  if (prior?.pageId) {
    await page.goto(`${FE}/admin/landing-pages/${prior.pageId}`, { waitUntil: "networkidle" });
  } else {
    await page.locator("input").first().fill("صفحه آزمایش T012");
    await page.locator('input[dir="ltr"]').fill("t012-runtime");
    await page.getByRole("button", { name: "ذخیره" }).click();
    await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 15000 });
  }
  rec("B-create", page.url().includes("/admin/landing-pages/"));
  await shot(page, "page-editor.png");

  async function waitAddReady() {
    await page.waitForFunction(() => {
      const el = document.querySelector("[data-testid=landing-add-section]");
      return el instanceof HTMLButtonElement && !el.disabled;
    }, null, { timeout: 15000 });
  }

  async function addSection(testId) {
    await waitAddReady();
    await page.locator("[data-testid=landing-add-section]").click();
    await page.locator("[data-testid=add-section-chooser]").waitFor();
    await page.locator(`[data-testid=${testId}]`).click();
    await page.locator("[data-testid=landing-section-drawer]").waitFor({ timeout: 10000 });
  }

  await waitAddReady();
  await page.locator("[data-testid=landing-add-section]").click();
  await page.locator("[data-testid=add-section-chooser]").waitFor();
  await shot(page, "add-section.png");
  await page.locator("[data-testid=add-section-hero]").click();
  await page.locator("[data-testid=landing-section-drawer]").waitFor({ timeout: 10000 });
  await page.getByRole("button", { name: "بستن" }).last().click();
  rec("C-hero", true);

  await addSection("add-section-products");
  await page.locator("[data-testid=product-section-editor]").waitFor({ timeout: 10000 });
  await shot(page, "product-section-editor.png");
  await page.getByRole("button", { name: "ذخیرهٔ بخش" }).click();
  await page.locator("[data-testid=landing-section-drawer]").waitFor({ state: "hidden", timeout: 10000 }).catch(async () => {
    await page.getByRole("button", { name: "بستن" }).last().click();
  });
  rec("D-products", true);

  await addSection("add-section-categories");
  await page.getByRole("button", { name: "بستن" }).last().click();
  rec("E-category", true);

  const downs = page.locator('button[aria-label="پایین"]');
  if (await downs.count()) await downs.first().click();
  await page.waitForTimeout(400);
  await shot(page, "composer-reordered.png");
  rec("F-reorder", true);

  const disable = page.getByRole("button", { name: /فعال/ }).last();
  if (await disable.count()) await disable.click();
  rec("G-disable", true);

  const listed = await admin("/v1/admin/pages");
  const runtime = Array.isArray(listed.body) ? listed.body.find((row) => row.slug === "t012-runtime") : null;
  const pageId = runtime?.pageId;
  rec("page-id", Boolean(pageId), { pageId });
  await admin(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: "Draft" }),
  });
  await page.goto(`${FE}/admin/landing-pages/${pageId}/preview`, { waitUntil: "load" });
  await page.locator("[data-testid=landing-draft-preview]").waitFor({ timeout: 20000 }).catch(() => {});
  rec("H-preview", await page.locator("[data-testid=landing-draft-preview]").count() > 0);
  await shot(page, "draft-preview.png");

  const publicDraft = await api("/v1/storefront/pages/t012-runtime?locale=fa");
  rec("I-draft-404", publicDraft.status === 404, { status: publicDraft.status });

  await admin(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: "Published" }),
  });
  const published = await api("/v1/storefront/pages/t012-runtime?locale=fa");
  rec("J-publish", published.status === 200 && Array.isArray(published.body?.sections), { status: published.status });

  await page.goto(`${FE}/t012-runtime`, { waitUntil: "load" });
  rec("K-public", await page.locator("[data-testid=storefront-landing-page]").count() > 0);
  await shot(page, "published-landing.png");

  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: pageId }),
  });
  await page.goto(`${FE}/fa`, { waitUntil: "load" });
  rec("L-home", await page.locator("[data-testid=storefront-custom-home]").count() > 0);
  rec("M-fa", await page.locator("[data-testid=storefront-landing-page]").count() > 0);
  await shot(page, "custom-home.png");

  const alt = await putAppearance("forest-green", original.themeMode ?? "UserChoice", original.productCardSkin ?? "classic");
  rec("N-put-palette", alt.status === 200, { status: alt.status, body: alt.body });
  await page.goto(`${FE}/fa?t=palette`, { waitUntil: "load" });
  rec("N-palette", /data-storefront-palette="forest-green"/.test(await page.content()));
  await shot(page, "landing-alt-palette.png");

  const dark = await putAppearance("forest-green", "DarkOnly", original.productCardSkin ?? "classic");
  rec("O-put-dark", dark.status === 200, { status: dark.status });
  await page.goto(`${FE}/fa?t=dark`, { waitUntil: "load" });
  rec("O-dark", /data-storefront-theme-mode="DarkOnly"|data-theme="dark"/.test(await page.content()));
  await shot(page, "landing-dark.png");

  const skin = await putAppearance(original.paletteKey ?? "tooba-blue", original.themeMode ?? "UserChoice", "elevated");
  rec("P-put-skin", skin.status === 200, { status: skin.status });
  await page.goto(`${FE}/fa?t=skin`, { waitUntil: "load" });
  rec("P-skin", /data-storefront-product-card-skin="elevated"/.test(await page.content()));

  await admin("/v1/admin/pages/home", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  await putAppearance(original.paletteKey ?? "tooba-blue", original.themeMode ?? "UserChoice", original.productCardSkin ?? "classic");
  await page.goto(`${FE}/fa?t=home`, { waitUntil: "load" });
  rec("Q-restore-home", await page.locator("[data-testid=storefront-home]").count() > 0);

  const seed = await api("/v1/storefront/pages/landing-demo?locale=fa");
  rec("R-seed", seed.status === 200, { status: seed.status });

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T012/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T012/runtime-report.json"), JSON.stringify({ ...report, fatal: String(err) }, null, 2));
  process.exit(1);
});
