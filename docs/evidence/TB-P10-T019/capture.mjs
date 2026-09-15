import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const OUT = join(ROOT, "docs/evidence/TB-P10-T019/screenshots");
const SLUG = "t019-runtime";
const report = { ok: true, steps: [], files: {}, errors: [], console: [] };

mkdirSync(OUT, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
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
  rec(`file:${name}`, size > 1500, { bytes: size });
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

  await page.setDefaultTimeout(90000);
  await page.setDefaultNavigationTimeout(90000);

  // A — canonical Home via shared renderer
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForTimeout(1500);
  const homeOk = await page.locator("[data-testid=storefront-home]").count() > 0
    || await page.locator("[data-testid=storefront-landing-page]").count() > 0;
  rec("A-canonical-home-or-landing", homeOk);
  if (await page.locator("[data-testid=storefront-landing-page]").count() > 0) {
    // unset home first for clean A later
    await admin("/v1/admin/pages/home", { method: "DELETE" }).catch(() => {});
    await page.goto(`${FE}/fa`, { waitUntil: "load" });
  }
  rec("A-home", await page.locator("[data-testid=storefront-home]").count() > 0);
  await shot(page, "canonical-home-after-shared-renderer.png");

  const existing = await admin("/v1/admin/pages");
  const prior = Array.isArray(existing.body) ? existing.body.find((row) => row.slug === SLUG || row.Slug === SLUG) : null;
  let pageId = prior?.pageId || prior?.PageId || null;

  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "load" });
  if (pageId) {
    await page.goto(`${FE}/admin/landing-pages/${pageId}`, { waitUntil: "load" });
  } else {
    await page.locator("input").first().fill("صفحه آزمایش T019");
    await page.locator('input[dir="ltr"]').fill(SLUG);
    await page.getByRole("button", { name: "ذخیره" }).click();
    await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]+/i, { timeout: 20000 });
    pageId = page.url().split("/").pop();
  }
  rec("B-create-edit", Boolean(pageId));
  await shot(page, "draft-composer.png");

  async function waitAddReady() {
    await page.waitForFunction(() => {
      const el = document.querySelector("[data-testid=landing-add-section]");
      return el instanceof HTMLButtonElement && !el.disabled;
    }, null, { timeout: 20000 });
  }

  async function addSectionVariant(sectionTestId, variantTestId) {
    await waitAddReady();
    await page.locator("[data-testid=landing-add-section]").click();
    await page.locator("[data-testid=composition-section-catalog]").waitFor({ timeout: 10000 });
    await shot(page, "admin-add-section.png").catch(() => {});
    await page.locator(`[data-testid=${sectionTestId}]`).click();
    await page.locator("[data-testid=composition-variant-picker]").waitFor({ timeout: 10000 });
    if (sectionTestId === "add-section-products") {
      await shot(page, "admin-variant-picker-product.png");
    }
    if (sectionTestId === "add-section-banners") {
      await shot(page, "admin-variant-picker-banner.png");
    }
    await page.locator(`[data-testid=${variantTestId}]`).click();
    await page.locator("[data-testid=landing-section-drawer]").waitFor({ timeout: 15000 });
    await shot(page, "admin-controlled-settings.png").catch(() => {});
    await page.locator("[data-testid=landing-section-drawer]").getByRole("button", { name: "ذخیرهٔ بخش" }).click();
    await page.waitForTimeout(800);
  }

  // Clear existing sections via API for idempotent run
  const listed = await admin(`/v1/admin/pages/${pageId}/sections`);
  const rows = Array.isArray(listed.body) ? listed.body : [];
  for (const row of rows) {
    const id = row.pageSectionId || row.PageSectionId;
    if (id) await admin(`/v1/admin/pages/${pageId}/sections/${id}`, { method: "DELETE" });
  }
  await page.reload({ waitUntil: "load" });

  await addSectionVariant("add-section-hero", "pick-variant-hero-contained");
  rec("C-hero", true);
  await addSectionVariant("add-section-stories", "pick-variant-story-circle");
  rec("D-story", true);
  await addSectionVariant("add-section-products", "pick-variant-product-card-carousel");
  rec("E-product", true);
  await addSectionVariant("add-section-banners", "pick-variant-banner-single");
  rec("F-banner", true);
  await addSectionVariant("add-section-brands", "pick-variant-brand-logo-rail");
  rec("G-brand", true);

  // ensure variant picker shots exist even if order differed
  if (!report.files["admin-add-section.png"]) {
    await waitAddReady();
    await page.locator("[data-testid=landing-add-section]").click();
    await shot(page, "admin-add-section.png");
    await page.locator("[data-testid=add-section-products]").click();
    await shot(page, "admin-variant-picker-product.png");
    await page.locator("button", { hasText: "بازگشت" }).click();
    await page.locator("[data-testid=add-section-banners]").click();
    await shot(page, "admin-variant-picker-banner.png");
    await page.locator("button", { hasText: "بستن" }).click().catch(() => page.keyboard.press("Escape"));
  }

  const after = await admin(`/v1/admin/pages/${pageId}/sections`);
  const sections = Array.isArray(after.body) ? after.body : [];
  rec("sections-count", sections.length >= 5, { count: sections.length });

  // H reorder
  if (sections.length >= 2) {
    const ids = sections.map((s) => s.pageSectionId || s.PageSectionId);
    const reordered = [ids[1], ids[0], ...ids.slice(2)];
    const reorder = await admin(`/v1/admin/pages/${pageId}/sections/reorder`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sectionIds: reordered }),
    });
    rec("H-reorder", reorder.status < 300, { status: reorder.status });
  } else rec("H-reorder", false);

  // I disable
  if (sections[0]) {
    const id = sections[0].pageSectionId || sections[0].PageSectionId;
    const dis = await admin(`/v1/admin/pages/${pageId}/sections/${id}/enabled`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ isEnabled: false }),
    });
    rec("I-disable", dis.status < 300, { status: dis.status });
  } else rec("I-disable", false);

  await page.reload({ waitUntil: "load" });
  await shot(page, "draft-composer.png");

  // J preview
  await page.goto(`${FE}/admin/landing-pages/${pageId}/preview`, { waitUntil: "load" });
  await page.locator("[data-testid=landing-draft-preview]").waitFor({ timeout: 20000 }).catch(() => {});
  rec("J-preview", await page.locator("[data-testid=landing-draft-preview]").count() > 0);
  await shot(page, "draft-preview.png");

  // K publish
  const pub = await admin(`/v1/admin/pages/${pageId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: "Published" }),
  });
  rec("K-publish", pub.status < 300, { status: pub.status, body: pub.body });

  // L public
  await page.goto(`${FE}/fa/${SLUG}`, { waitUntil: "load" });
  rec("L-public", await page.locator("[data-testid=storefront-landing-page]").count() > 0);
  await shot(page, "published-landing.png");

  // M select as Home
  const homeSet = await admin(`/v1/admin/pages/home`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: pageId }),
  });
  rec("M-set-home", homeSet.status < 300, { status: homeSet.status, body: homeSet.body });
  // verify API selection before FE
  const hs = await api("/v1/storefront/home-selection");
  rec("M-home-selection", Boolean(hs.body?.selectedPage || hs.body?.SelectedPage), { status: hs.status });
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForTimeout(2500);
  const landingHome = await page.locator("[data-testid=storefront-custom-home]").count() > 0
    || await page.locator("[data-testid=storefront-landing-page]").count() > 0;
  rec("N-landing-as-home", landingHome);
  await shot(page, "landing-as-home.png");

  // O restore canonical
  const unset = await admin(`/v1/admin/pages/home`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ homePageId: null }),
  });
  rec("O-unset-home", unset.status < 300, { status: unset.status });
  await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForTimeout(1500);
  rec("O-canonical", await page.locator("[data-testid=storefront-home]").count() > 0);
  await shot(page, "canonical-home-after-shared-renderer.png");

  await browser.close();
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T019/runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, errors: report.errors, steps: report.steps.length, files: Object.keys(report.files) }, null, 2));
  process.exit(report.ok ? 0 : 1);
})().catch((err) => {
  console.error(err);
  report.ok = false;
  report.errors.push(String(err));
  writeFileSync(join(ROOT, "docs/evidence/TB-P10-T019/runtime-report.json"), JSON.stringify(report, null, 2));
  process.exit(1);
});
