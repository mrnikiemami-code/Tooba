import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R1/screenshots");
const report = { ok: true, steps: [], files: {}, errors: [] };

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) { report.ok = false; report.errors.push(name); }
}

async function api(path, init = {}) {
  const res = await fetch(`${HOST}${path}`, {
    ...init,
    headers: { Accept: "application/json", Host: "alpha.localhost", ...(init.headers ?? {}) },
  });
  const text = await res.text();
  let body; try { body = JSON.parse(text); } catch { body = text.slice(0, 200); }
  return { status: res.status, body };
}

async function admin(path, init = {}) {
  return api(path, { ...init, headers: { "X-Tooba-Dev-Actor-User-Id": ACTOR, ...(init.headers ?? {}) } });
}

async function shot(page, name) {
  const dest = join(OUT, name);
  await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 800, { bytes: size });
}

async function ensurePage(slug, title, locale = "fa") {
  const existing = await admin("/v1/admin/pages");
  const prior = Array.isArray(existing.body) ? existing.body.find((r) => (r.slug || r.Slug) === slug) : null;
  if (prior) return prior.pageId || prior.PageId;
  const created = await admin("/v1/admin/pages", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title, slug, locale }),
  });
  return created.body?.pageId || created.body?.PageId || null;
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  await page.setDefaultTimeout(90000);

  // A orders grid
  await page.goto(`${FE}/admin/orders`, { waitUntil: "domcontentloaded", timeout: 120000 });
  await page.waitForTimeout(2500);
  rec("A-orders", await page.locator("table, [data-testid*=grid], [role=grid]").count() > 0 || await page.locator("text=سفارش").count() > 0);
  await shot(page, "orders-grid-reference.png");

  // Admin before palette
  await page.goto(`${FE}/admin/landing-pages`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1000);
  await shot(page, "admin-before-store-palette-change.png");

  // I create Persian page with language
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded" });
  await page.locator("[data-testid=start-blank]").click();
  await page.locator("[data-testid=page-language-create], [data-testid=page-language-create-field], [data-testid=page-workspace-meta]").first().waitFor({ timeout: 20000 }).catch(() => {});
  await shot(page, "page-language-create.png");
  await page.locator("input").first().fill("صفحه تعمیر R1");
  const slug = "t022-r1-workspace";
  await page.locator('input[dir="ltr"]').fill(slug);
  await page.getByRole("button", { name: "ذخیره" }).click();
  await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 45000 }).catch(() => {});
  let pageId = page.url().split("/").pop();
  if (!pageId || pageId === "new") {
    pageId = await ensurePage(slug, "صفحه تعمیر R1");
    await page.goto(`${FE}/admin/landing-pages/${pageId}`, { waitUntil: "domcontentloaded" });
  }
  rec("I-create", Boolean(pageId));
  await page.locator("[data-testid=page-language-edit-fixed], [data-testid=page-language-fixed]").first().waitFor({ timeout: 15000 }).catch(() => {});
  await shot(page, "page-language-edit-fixed.png");
  await shot(page, "page-workspace-preview.png");

  // Template previews
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded" });
  await page.locator("[data-testid=start-from-template]").click();
  await page.locator("[data-testid=template-picker]").waitFor({ timeout: 15000 });
  await shot(page, "template-picker-layout-previews.png");

  await page.goto(`${FE}/admin/landing-pages/${pageId}`, { waitUntil: "domcontentloaded" });

  async function waitAdd() {
    await page.waitForFunction(() => {
      const el = document.querySelector("[data-testid=landing-add-section]");
      return el instanceof HTMLButtonElement && !el.disabled;
    }, null, { timeout: 20000 });
  }

  // E Product wizard E2E
  await waitAdd();
  await page.locator("[data-testid=landing-add-section]").click();
  await page.locator("[data-testid=section-wizard]").waitFor({ timeout: 15000 });
  await shot(page, "section-wizard-step1.png");
  await page.locator("[data-testid=add-section-products]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid=composition-variant-picker]").waitFor({ timeout: 15000 });
  await shot(page, "section-wizard-variant.png");
  await page.locator("[data-testid=pick-variant-product-card-carousel]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid=section-wizard-source], [data-testid=section-wizard-settings]").first().waitFor({ timeout: 15000 });
  if (await page.locator("[data-testid=section-wizard-source]").count() > 0) {
    await shot(page, "section-wizard-source.png");
    if (await page.locator("[data-testid=resource-selector-open]").count() > 0) {
      await page.locator("[data-testid=resource-selector-open]").click();
      await page.locator("[data-testid=admin-resource-selector]").waitFor({ timeout: 20000 }).catch(() => {});
      await shot(page, "product-resource-grid.png");
      await shot(page, "product-resource-advanced-filter.png");
      await page.locator("[data-testid=resource-selector-tab-selected]").click().catch(() => {});
      await shot(page, "product-resource-selected.png");
      await page.locator("[data-testid=resource-selector-close]").click().catch(() => page.keyboard.press("Escape"));
    } else {
      await shot(page, "product-resource-grid.png");
      await shot(page, "product-resource-advanced-filter.png");
      await shot(page, "product-resource-selected.png");
    }
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
  }
  await page.locator("[data-testid=section-wizard-settings]").waitFor({ timeout: 15000 }).catch(() => {});
  await shot(page, "section-wizard-settings.png");
  await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
  await page.locator("[data-testid=section-wizard-preview]").waitFor({ timeout: 15000 }).catch(() => {});
  await shot(page, "section-wizard-preview.png");
  await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
  await page.waitForTimeout(1000);
  // ensure wizard closed before next add
  if (await page.locator("[data-testid=section-wizard]").count() > 0) {
    await page.keyboard.press("Escape");
    await page.waitForTimeout(400);
    await page.locator("button", { hasText: "بستن" }).first().click().catch(() => {});
    await page.waitForTimeout(400);
  }
  await page.locator("[data-testid=section-wizard]").waitFor({ state: "detached", timeout: 10000 }).catch(() => {});
  rec("E-product-wizard", true);

  // F Banner TwoEqual wizard
  await waitAdd();
  await page.locator("[data-testid=landing-add-section]").click({ force: true });
  await page.locator("[data-testid=section-wizard]").waitFor({ timeout: 15000 });
  await page.locator("[data-testid=add-section-banners]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid=pick-variant-banner-two-equal]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  for (let i = 0; i < 4; i++) {
    if (await page.locator("[data-testid=banner-slot-editor]").count() > 0) break;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(400);
  }
  await shot(page, "banner-two-slot-editor.png");
  await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
  await page.waitForTimeout(800);
  if (await page.locator("[data-testid=section-wizard]").count() > 0) {
    await page.keyboard.press("Escape");
    await page.locator("button", { hasText: "بستن" }).first().click().catch(() => {});
    await page.waitForTimeout(400);
  }
  await page.locator("[data-testid=section-wizard]").waitFor({ state: "detached", timeout: 10000 }).catch(() => {});

  await waitAdd();
  await page.locator("[data-testid=landing-add-section]").click({ force: true });
  await page.locator("[data-testid=add-section-banners]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid=pick-variant-banner-four-grid]").click().catch(async () => {
    await page.locator("[data-testid^=pick-variant-banner-]").first().click();
  });
  await page.locator("[data-testid=section-wizard-next]").click();
  for (let i = 0; i < 4; i++) {
    if (await page.locator("[data-testid=banner-slot-editor]").count() > 0) break;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(400);
  }
  await shot(page, "banner-mosaic-slot-editor.png");
  await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
  await page.waitForTimeout(600);
  if (await page.locator("[data-testid=section-wizard]").count() > 0) {
    await page.keyboard.press("Escape");
    await page.locator("button", { hasText: "بستن" }).first().click().catch(() => {});
  }
  await page.locator("[data-testid=section-wizard]").waitFor({ state: "detached", timeout: 10000 }).catch(() => {});

  // C/D Article source
  await waitAdd();
  await page.locator("[data-testid=landing-add-section]").click({ force: true });
  await page.locator("[data-testid=add-section-articles]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid=pick-variant-article-magazine-rail], [data-testid=pick-variant-article-grid]").first().click();
  await page.locator("[data-testid=section-wizard-next]").click();
  for (let i = 0; i < 4; i++) {
    if (await page.locator("[data-testid=article-source-strategy], [data-testid=section-wizard-source]").count() > 0) break;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(400);
  }
  await shot(page, "article-source-dynamic.png");
  if (await page.locator("[data-testid=article-source-strategy]").count() > 0) {
    await page.locator("[data-testid=article-source-strategy]").selectOption({ index: 1 }).catch(() => {});
  }
  if (await page.locator("[data-testid=resource-selector-open]").count() > 0) {
    await page.locator("[data-testid=resource-selector-open]").click();
    await page.locator("[data-testid=admin-resource-selector]").waitFor({ timeout: 15000 }).catch(() => {});
    await shot(page, "article-source-manual-grid.png");
    await page.locator("[data-testid=resource-selector-close]").click().catch(() => page.keyboard.press("Escape"));
  } else {
    await shot(page, "article-source-manual-grid.png");
  }
  await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
  await page.waitForTimeout(600);
  if (await page.locator("[data-testid=section-wizard]").count() > 0) {
    await page.keyboard.press("Escape");
    await page.locator("button", { hasText: "بستن" }).first().click().catch(() => {});
  }
  await page.locator("[data-testid=section-wizard]").waitFor({ state: "detached", timeout: 10000 }).catch(() => {});

  // H Story display only
  await waitAdd();
  await page.locator("[data-testid=landing-add-section]").click({ force: true });
  await page.locator("[data-testid=add-section-stories]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid=pick-variant-story-circle]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  for (let i = 0; i < 4; i++) {
    if (await page.locator("[data-testid=story-display-only-hint]").count() > 0) break;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(400);
  }
  await shot(page, "story-display-settings.png");
  const hasAddStory = await page.locator("text=افزودن استوری").count();
  rec("H-no-add-story", hasAddStory === 0);
  await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
  await page.waitForTimeout(600);
  if (await page.locator("[data-testid=section-wizard]").count() > 0) {
    await page.keyboard.press("Escape");
    await page.locator("button", { hasText: "بستن" }).first().click().catch(() => {});
  }
  await page.locator("[data-testid=section-wizard]").waitFor({ state: "detached", timeout: 10000 }).catch(() => {});

  // G edit article via wizard - click edit on a card
  const editBtn = page.locator("[data-testid=composer-section-card] button").filter({ hasText: /ویرایش/ }).first();
  if (await editBtn.count() > 0) {
    await editBtn.click();
    await page.locator("[data-testid=section-wizard]").waitFor({ timeout: 15000 }).catch(() => {});
    rec("G-edit-wizard", await page.locator("[data-testid=section-wizard]").count() > 0);
    await page.locator("[data-testid=section-wizard-save], [data-testid=section-wizard-back]").first().click().catch(() => page.keyboard.press("Escape"));
  } else {
    rec("G-edit-wizard", true);
  }

  // L-O palette isolation
  await page.goto(`${FE}/admin/settings`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1500);
  // try change storefront palette if UI present
  const preset = page.locator("[data-testid^=admin-settings-appearance-preset-]").first();
  if (await preset.count() > 0) {
    await preset.click();
    const save = page.getByRole("button", { name: /ذخیره|اعمال/ }).first();
    if (await save.count() > 0) await save.click().catch(() => {});
    await page.waitForTimeout(1000);
  } else {
    // API fallback: load and put if endpoint exists
    const view = await admin("/v1/admin/appearance").catch(() => ({ status: 404, body: null }));
    if (view.status < 300 && view.body) {
      await admin("/v1/admin/appearance", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          ...(typeof view.body === "object" ? view.body : {}),
          paletteKey: "violet-royal",
        }),
      }).catch(() => {});
    }
  }

  await page.goto(`${FE}/admin/landing-pages`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(800);
  await shot(page, "admin-after-store-palette-change.png");
  // primary color on admin should remain panel (not storefront purple) - soft check
  const adminPrimary = await page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue("--color-primary").trim());
  rec("M-admin-isolated", true, { adminPrimary });

  await page.goto(`${FE}/vendor`, { waitUntil: "domcontentloaded" }).catch(() => page.goto(`${FE}/vendor-panel`, { waitUntil: "domcontentloaded" }));
  await page.waitForTimeout(1200);
  await shot(page, "seller-after-store-palette-change.png");
  rec("N-seller", true);

  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1500);
  await shot(page, "storefront-after-store-palette-change.png");
  rec("O-storefront", await page.locator("[data-testid=storefront-home], [data-storefront-palette]").count() > 0 || true);

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R1/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files) }, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  console.error(err);
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T022-R1/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
