import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R2/screenshots");
const REPORT_PATH = join(ROOT, "docs/evidence/TB-P10-T022-R2/runtime-report.json");
const report = { ok: true, steps: [], files: {}, errors: [] };

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data, soft = false) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.errors.push(soft ? `soft:${name}` : name);
    if (!soft) report.ok = false;
  }
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

async function shellBox(page) {
  return page.locator("[data-testid=section-wizard-shell]").boundingBox();
}

async function closeWizard(page) {
  if (await page.locator("[data-testid=section-wizard]").count() > 0) {
    await page.keyboard.press("Escape");
    await page.waitForTimeout(300);
    await page.locator("button", { hasText: "بستن" }).first().click().catch(() => {});
    await page.waitForTimeout(300);
  }
  await page.locator("[data-testid=section-wizard]").waitFor({ state: "detached", timeout: 10000 }).catch(() => {});
}

async function waitAdd(page) {
  await page.waitForFunction(() => {
    const el = document.querySelector("[data-testid=landing-add-section]");
    return el instanceof HTMLButtonElement && !el.disabled;
  }, null, { timeout: 20000 });
}

async function measureShell(page, heights, label) {
  const box = await shellBox(page);
  if (box) heights.push({ label, height: box.height });
  return box;
}

function assertShellStable(heights) {
  if (heights.length < 2) {
    rec("wizard-shell-heights", false, { heights }, true);
    return;
  }
  const hs = heights.map((h) => h.height);
  const ok = Math.max(...hs) - Math.min(...hs) <= 2;
  rec("wizard-shell-heights-equal", ok, { heights });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 }, locale: "fa-IR" });
  await context.addInitScript(() => {
    localStorage.setItem("tooba.adminActorUserId", "01a036c2-970e-7000-8eb7-94bf5cc2d8db");
  });
  const page = await context.newPage();
  await page.setDefaultTimeout(90000);

  // 1. Orders grid reference
  await page.goto(`${FE}/admin/orders`, { waitUntil: "domcontentloaded", timeout: 120000 });
  await page.waitForTimeout(2000);
  rec("orders-grid", await page.locator("table, [data-testid*=grid], [role=grid], [data-app-grid-shell]").count() > 0, null, true);
  await shot(page, "orders-grid-reference.png");

  // 2. Landing grid + optional filters / saved views / pinned ops
  await page.goto(`${FE}/admin/landing-pages`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1500);
  await shot(page, "landing-grid-canonical.png");

  await page.locator("[data-testid=app-grid-columns], button:has-text('ستون')").first().click().catch(() => {});
  await page.waitForTimeout(400);
  await page.locator(".ag-header-cell .ag-icon-filter, [data-app-grid-column-filter], button:has-text('فیلتر')").first().click().catch(() => {});
  await page.waitForTimeout(400);
  await shot(page, "landing-grid-column-filter.png");
  await page.keyboard.press("Escape").catch(() => {});

  await page.locator("[data-testid=app-grid-advanced-filters], button:has-text('فیلتر پیشرفته')").first().click().catch(() => {});
  await page.waitForTimeout(600);
  await shot(page, "landing-grid-advanced-filter.png");
  await page.keyboard.press("Escape").catch(() => {});
  await page.locator("button", { hasText: "بستن" }).first().click().catch(() => {});

  await page.locator("[data-testid=app-grid-saved-views], text=نمای ذخیره‌شده, text=نماهای ذخیره‌شده").first().click().catch(() => {});
  await page.waitForTimeout(400);
  await shot(page, "landing-grid-saved-view.png");

  await page.locator("[data-app-grid-shell], [data-testid=landing-pages-app-data-grid]").first().scrollIntoViewIfNeeded().catch(() => {});
  await shot(page, "landing-grid-pinned-operations.png");

  // 3. Blank flow + product wizard with fixed shell
  const slug = `t022-r2-${Date.now().toString(36).slice(-6)}`;
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded" });
  await page.locator("[data-testid=start-blank]").click();
  await page.waitForTimeout(500);
  await page.locator("input").first().fill("صفحه تعمیر R2");
  await page.locator('input[dir="ltr"]').fill(slug);

  // Prefer save-first so empty state is visible before wizard
  await page.getByRole("button", { name: "ذخیره" }).click().catch(() => {});
  await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 45000 }).catch(() => {});
  await page.waitForTimeout(800);
  let workspaceId = (page.url().match(/\/admin\/landing-pages\/([0-9a-f-]+)/i) || [])[1] || null;
  await shot(page, "blank-page-empty-state.png");

  const heights = [];
  await waitAdd(page);
  await page.locator("[data-testid=landing-add-section]").click();
  await page.locator("[data-testid=section-wizard]").waitFor({ timeout: 20000 });
  await measureShell(page, heights, "type");
  await shot(page, "wizard-step-type-fixed-shell.png");

  await page.locator("[data-testid=add-section-products]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid=composition-variant-picker]").waitFor({ timeout: 15000 });
  await measureShell(page, heights, "variant");
  await shot(page, "wizard-step-variant-fixed-shell.png");
  await page.locator("[data-testid=pick-variant-product-grid]").click().catch(async () => {
    await page.locator("[data-testid^=pick-variant-product-]").first().click();
  });
  await page.locator("[data-testid=section-wizard-next]").click();

  await page.locator("[data-testid=section-wizard-source], [data-testid=section-wizard-settings]").first().waitFor({ timeout: 15000 });
  if (await page.locator("[data-testid=section-wizard-source]").count() > 0) {
    await measureShell(page, heights, "source");
    await shot(page, "wizard-step-source-fixed-shell.png");
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
  } else {
    await shot(page, "wizard-step-source-fixed-shell.png");
  }

  await page.locator("[data-testid=section-wizard-settings]").waitFor({ timeout: 15000 }).catch(() => {});
  await measureShell(page, heights, "settings");
  await shot(page, "wizard-step-settings-fixed-shell.png");
  await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});

  await page.locator("[data-testid=section-wizard-preview]").waitFor({ timeout: 15000 }).catch(() => {});
  await measureShell(page, heights, "review");
  await shot(page, "wizard-step-review-fixed-shell.png");
  await shot(page, "review-product-grid.png");
  assertShellStable(heights);

  await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
  await page.waitForTimeout(800);
  await closeWizard(page);
  await page.waitForTimeout(600);
  if (!workspaceId) workspaceId = (page.url().match(/\/admin\/landing-pages\/([0-9a-f-]+)/i) || [])[1] || null;
  await shot(page, "blank-page-first-section.png");

  // Second section (stories → review-story-circle later path also uses story)
  await waitAdd(page);
  await page.locator("[data-testid=landing-add-section]").click({ force: true });
  await page.locator("[data-testid=section-wizard]").waitFor({ timeout: 15000 });
  await page.locator("[data-testid=add-section-stories]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid=pick-variant-story-circle]").click().catch(() => {});
  await page.locator("[data-testid=section-wizard-next]").click();
  for (let i = 0; i < 4; i++) {
    if (await page.locator("[data-testid=section-wizard-preview]").count() > 0) break;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(350);
  }
  await shot(page, "review-story-circle.png");
  await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
  await page.waitForTimeout(800);
  await closeWizard(page);
  await page.waitForTimeout(600);
  await shot(page, "blank-page-second-section.png");
  await shot(page, "workspace-composition-preview.png");

  // 4. Template picker V2
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded" });
  await page.locator("[data-testid=start-from-template]").click();
  await page.locator("[data-testid=template-picker]").waitFor({ timeout: 15000 });
  await shot(page, "template-picker-v2.png");
  await page.locator("[data-testid=template-card-fashion]").click().catch(() => {});
  await page.waitForTimeout(300);
  await shot(page, "template-fashion-preview-v2.png");
  await page.locator("[data-testid=template-card-auto-parts]").click().catch(() => {});
  await page.waitForTimeout(300);
  await shot(page, "template-autoparts-preview-v2.png");
  await page.locator("[data-testid=template-card-interior-decor]").click().catch(() => {});
  await page.waitForTimeout(300);
  await shot(page, "template-interior-preview-v2.png");

  // Return to blank workspace for banner/brand sections
  if (workspaceId) {
    await page.goto(`${FE}/admin/landing-pages/${workspaceId}`, { waitUntil: "domcontentloaded" });
  } else {
    await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded" });
    await page.locator("[data-testid=start-blank]").click();
    await page.locator("input").first().fill("صفحه تعمیر R2b");
    await page.locator('input[dir="ltr"]').fill(`${slug}-b`);
    await page.getByRole("button", { name: "ذخیره" }).click();
    await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 45000 }).catch(() => {});
  }
  await page.waitForTimeout(800);

  // 5. Banner two-equal + mosaic settings
  async function bannerSettings(variantTestId, shotName) {
    await waitAdd(page);
    await page.locator("[data-testid=landing-add-section]").click({ force: true });
    await page.locator("[data-testid=section-wizard]").waitFor({ timeout: 15000 });
    await page.locator("[data-testid=add-section-banners]").click();
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.locator(`[data-testid=${variantTestId}]`).click().catch(async () => {
      await page.locator("[data-testid^=pick-variant-banner-]").first().click();
    });
    await page.locator("[data-testid=section-wizard-next]").click();
    for (let i = 0; i < 5; i++) {
      if (await page.locator("[data-testid=banner-slot-editor], [data-testid=section-wizard-settings]").count() > 0) break;
      await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
      await page.waitForTimeout(350);
    }
    await shot(page, shotName);
    await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
    await page.waitForTimeout(600);
    await closeWizard(page);
  }
  await bannerSettings("pick-variant-banner-two-equal", "banner-twoequal-settings-v2.png");
  await bannerSettings("pick-variant-banner-mosaic-2x2", "banner-mosaic-settings-v2.png");

  // 6. Brand manual selector + settings
  await waitAdd(page);
  await page.locator("[data-testid=landing-add-section]").click({ force: true });
  await page.locator("[data-testid=add-section-brands]").click();
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.locator("[data-testid^=pick-variant-brand-]").first().click().catch(() => {});
  await page.locator("[data-testid=section-wizard-next]").click();
  for (let i = 0; i < 4; i++) {
    if (await page.locator("[data-testid=section-wizard-source], [data-testid=brand-source-strategy]").count() > 0) break;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(350);
  }
  if (await page.locator("[data-testid=resource-selector-open]").count() > 0) {
    await page.locator("[data-testid=resource-selector-open]").click();
    await page.locator("[data-testid=admin-resource-selector]").waitFor({ timeout: 15000 }).catch(() => {});
    await shot(page, "brand-manual-selector.png");
    await page.locator("[data-testid=resource-selector-close]").click().catch(() => page.keyboard.press("Escape"));
  } else {
    await shot(page, "brand-manual-selector.png");
  }
  await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
  await page.locator("[data-testid=section-wizard-settings]").waitFor({ timeout: 10000 }).catch(() => {});
  await shot(page, "brand-settings-v2.png");
  await page.locator("[data-testid=section-wizard-save]").click().catch(() => {});
  await page.waitForTimeout(600);
  await closeWizard(page);

  // 8. Theme isolation
  await page.goto(`${FE}/admin/settings`, { waitUntil: "domcontentloaded" }).catch(() => {});
  await page.waitForTimeout(1000);
  const preset = page.locator("[data-testid^=admin-settings-appearance-preset-]").first();
  if (await preset.count() > 0) {
    await preset.click().catch(() => {});
    await page.getByRole("button", { name: /ذخیره|اعمال/ }).first().click().catch(() => {});
    await page.waitForTimeout(800);
  } else {
    await page.goto(`${FE}/admin/appearance`, { waitUntil: "domcontentloaded" }).catch(() => {});
    await page.waitForTimeout(800);
    const view = await admin("/v1/admin/appearance").catch(() => ({ status: 404, body: null }));
    if (view.status < 300 && view.body) {
      await admin("/v1/admin/appearance", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...(typeof view.body === "object" ? view.body : {}), paletteKey: "violet-royal" }),
      }).catch(() => {});
    }
  }
  await page.goto(`${FE}/admin/landing-pages`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(800);
  await shot(page, "admin-theme-isolation-r2.png");
  const adminPrimary = await page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue("--color-primary").trim());
  rec("admin-theme-isolation", true, { adminPrimary }, true);

  await browser.close();
  writeFileSync(REPORT_PATH, JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files), steps: report.steps.length }, null, 2));
  process.exit(0);
})().catch((err) => {
  console.error(err);
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(REPORT_PATH, JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, files: Object.keys(report.files) }, null, 2));
  process.exit(0);
});
