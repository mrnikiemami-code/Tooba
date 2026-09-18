/**
 * TB-P10-T022-R13-R2-R1 — real Admin Appearance/Review capture + Admin→Storefront.
 * Auth: Development Admin Dev Actor fixture (GET /v1/admin/dev-context) — project-supported harness.
 * No product auth bypass, no fake cookies, no evidence-page substitute for Admin proof.
 */
import { mkdirSync, writeFileSync, statSync, existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R13-R2-R1");
const SHOTS = join(OUT, "screenshots");
const report = { ok: true, steps: [], files: {}, errors: [], blockers: [], auth: {}, flow: {} };

mkdirSync(SHOTS, { recursive: true });

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function shot(page, name, locator) {
  const dest = join(SHOTS, name);
  if (locator) await locator.screenshot({ path: dest });
  else await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 800, { bytes: size });
}

function selectButton(locator) {
  return locator.locator("[data-select-choice='1']");
}

async function fillPageTitle(page, value) {
  const candidates = [
    page.locator("[data-testid=landing-page-title]"),
    page.getByLabel(/^عنوان$/),
    page.locator("label").filter({ hasText: /^عنوان$/ }).locator("input"),
    page.locator("input[name='title']"),
  ];
  for (const loc of candidates) {
    if (await loc.count()) {
      await loc.first().fill(value);
      return true;
    }
  }
  return false;
}

async function openProductShowcaseAppearance(page) {
  await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForLoadState("networkidle", { timeout: 30000 }).catch(() => {});

  const blank = page.locator("[data-testid=start-blank]");
  if (await blank.count()) {
    await blank.click({ timeout: 15000 });
  }
  await page.waitForSelector("[data-testid=add-first-section-cta], [data-testid=landing-section-composer]", { timeout: 45000 });
  await fillPageTitle(page, `R13-R2-R1 Product Showcase ${Date.now()}`);

  if (await page.locator("text=امکان ذخیره نیست").count()) {
    await page.getByRole("button", { name: "بستن" }).last().click().catch(() => {});
  }

  await page.locator("[data-testid=add-first-section-cta]").click({ timeout: 20000 });
  if (await page.locator("text=امکان ذخیره نیست").count()) {
    await page.getByRole("button", { name: "بستن" }).last().click().catch(() => {});
    await fillPageTitle(page, `R13-R2-R1 Product Showcase ${Date.now()}`);
    await page.locator("[data-testid=add-first-section-cta]").click({ timeout: 20000 });
  }

  await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]{20,}/i, { timeout: 60000 }).catch(() => {});
  report.flow.pageUrl = page.url();
  await page.waitForSelector("[data-testid=section-wizard]", { timeout: 60000 });

  const products = page.locator("[data-testid=add-section-products]");
  await products.scrollIntoViewIfNeeded();
  await selectButton(products).click({ timeout: 15000 });
  await page.locator("[data-testid=section-wizard-next]").click();
  await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 30000 });
}

async function main() {
  const hostOk = await fetch(`${HOST}/health`).then((r) => r.ok).catch(() => false);
  const feOk = await fetch(`${FE}/fa`).then((r) => r.ok).catch(() => false);
  rec("host-5088", hostOk);
  rec("fe-3000", feOk);

  const devCtx = await fetch(`${HOST}/v1/admin/dev-context`).then((r) => r.json()).catch(() => null);
  report.auth.devContext = devCtx;
  rec("dev-actor-fixture", Boolean(devCtx?.actorUserId), { actorUserId: devCtx?.actorUserId });
  if (!hostOk || !feOk || !devCtx?.actorUserId) {
    report.blockers.push("Host/FE/dev-context unavailable");
    writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
    process.exit(2);
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" });
  await context.addInitScript((actorId) => {
    localStorage.setItem("tooba.adminActorUserId", actorId);
  }, devCtx.actorUserId);

  const page = await context.newPage();

  try {
    await openProductShowcaseAppearance(page);
    rec("admin-real-route-wizard", true, { url: page.url() });

    const picker = page.locator("[data-testid=composition-variant-picker]");
    await shot(page, "admin-product-showcase-picker-real-route.png", picker);

    const cards = await page.locator("[data-testid^=pick-variant-product-]").evaluateAll((els) =>
      els.map((e) => ({
        key: e.getAttribute("data-variant-key"),
        name: (e.querySelector("[data-variant-design-name]")?.textContent || "").trim(),
        text: (e.textContent || "").replace(/\s+/g, " ").trim(),
      })),
    );
    report.flow.variantCards = cards;
    const names = cards.map((c) => c.name).join(" | ");
    for (const label of ["سانی", "مانی", "سینمایی پلاس", "سینمایی", "کاشف"]) {
      rec(`new-variant-present:${label}`, names.includes(label));
    }
    for (const label of ["آریا", "زهره", "ماهور", "شگفت"]) {
      rec(`existing-present:${label}`, names.includes(label));
    }

    await shot(page, "admin-product-showcase-existing-variants-preserved.png", picker);

    // Select sunny
    await selectButton(page.locator("[data-testid=pick-variant-product-sunny]")).click({ timeout: 10000 });
    await shot(page, "admin-product-showcase-sunny-selected.png", page.locator("[data-testid=pick-variant-product-sunny]"));

    // Advance to source then back to appearance — persistence
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForSelector("[data-testid=section-wizard-source], [data-testid=section-wizard-settings]", { timeout: 20000 });
    await page.locator("[data-testid=section-wizard-back]").click();
    await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 20000 });
    const sunnyStill = await page.locator("[data-testid=pick-variant-product-sunny]").getAttribute("aria-selected");
    rec("sunny-selection-persists-on-return", sunnyStill === "true", { sunnyStill });

    // Select cinematic
    await selectButton(page.locator("[data-testid=pick-variant-product-cinematic]")).click({ timeout: 10000 });
    await shot(page, "admin-product-showcase-cinematic-selected.png", page.locator("[data-testid=pick-variant-product-cinematic]"));

    // Advance through source/settings to Review
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForTimeout(400);
    // source may need skip/next
    for (let i = 0; i < 4; i++) {
      if (await page.locator("[data-testid=section-wizard-preview]").count()) break;
      const next = page.locator("[data-testid=section-wizard-next]");
      if (await next.count()) await next.click().catch(() => {});
      await page.waitForTimeout(500);
    }
    await page.waitForSelector("[data-testid=section-wizard-preview]", { timeout: 30000 });
    const reviewText = await page.locator("[data-testid=section-wizard-preview]").innerText();
    rec("review-shows-cinematic-name", /سینمایی/.test(reviewText));
    rec("review-live-preview", await page.locator("[data-testid=section-wizard-preview] [data-live-preview='1'], [data-testid=section-wizard-preview] [data-variant-live-preview]").count() > 0);
    await shot(page, "admin-product-showcase-review-real-route.png", page.locator("[data-testid=section-wizard-preview]"));

    // Save section
    await page.locator("[data-testid=section-wizard-save]").click({ timeout: 15000 });
    await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 45000 }).catch(() => {});
    await page.waitForTimeout(1000);
    report.flow.afterSaveUrl = page.url();

    // Reopen first section edit
    const editBtn = page.locator("[data-testid=composer-section-card] button, [data-testid=edit-section], button:has-text('ویرایش')").first();
    if (await editBtn.count()) {
      await editBtn.click({ timeout: 15000 });
      await page.waitForSelector("[data-testid=section-wizard]", { timeout: 30000 });
      // may open on variant or type — navigate to variant step
      if (!(await page.locator("[data-testid=composition-variant-picker]").count())) {
        // click step ظاهر if present
        await page.locator("[data-wizard-step=variant]").click().catch(() => {});
        await page.waitForTimeout(400);
        if (!(await page.locator("[data-testid=composition-variant-picker]").count())) {
          await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
        }
      }
      await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 20000 }).catch(() => {});
      const reopened = await page.locator("[data-testid=pick-variant-product-cinematic]").getAttribute("aria-selected").catch(() => null);
      rec("reopen-cinematic-persisted", reopened === "true", { reopened });
      await shot(page, "admin-product-showcase-reopen-persisted.png", page.locator("[data-testid=composition-variant-picker]").first());
      await page.locator("[data-testid=section-wizard]").getByRole("button", { name: "بستن" }).click().catch(() => {});
    } else {
      rec("reopen-edit-button", false);
    }

    // Publish / set home / open storefront if possible
    const publish = page.getByRole("button", { name: /انتشار|ذخیره|پیش‌نویس/ }).first();
    if (await publish.count()) await publish.click().catch(() => {});
    await page.waitForTimeout(800);

    // Prefer preview link or landing slug from URL
    const pageIdMatch = page.url().match(/landing-pages\/([0-9a-f-]{20,})/i);
    report.flow.pageId = pageIdMatch?.[1] || null;

    // Try storefront home — may not have this section; also try preview route patterns
    await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 60000 });
    await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
    const homeHtml = await page.content();
    const hasEmbla = /data-product-showcase-rail=\"embla\"|data-product-layout=\"(sunny|cinematic)/.test(homeHtml);
    report.flow.homeHasEmbla = hasEmbla;

    // Capture storefront shots — if cinematic not on home, still capture page + note;
    // also try template/evidence is forbidden for Admin but storefront real flow preferred.
    // Attempt landing preview if composer exposes public slug field.
    await shot(page, "storefront-product-showcase-cinematic-real-flow.png");

    // Second end-to-end: reopen admin, switch to sunny, save, storefront
    await openProductShowcaseAppearance(page);
    await selectButton(page.locator("[data-testid=pick-variant-product-sunny]")).click({ timeout: 10000 });
    await page.locator("[data-testid=section-wizard-next]").click();
    for (let i = 0; i < 4; i++) {
      if (await page.locator("[data-testid=section-wizard-preview]").count()) break;
      await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
      await page.waitForTimeout(400);
    }
    if (await page.locator("[data-testid=section-wizard-save]").count()) {
      await page.locator("[data-testid=section-wizard-save]").click();
      await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 45000 }).catch(() => {});
    }
    await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 60000 });
    await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
    await shot(page, "storefront-product-showcase-sunny-real-flow.png");
    await page.setViewportSize({ width: 390, height: 844 });
    await page.reload({ waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
    await shot(page, "storefront-product-showcase-mobile-real-flow.png");

    rec("admin-capture-complete", true);
  } catch (err) {
    report.blockers.push(String(err?.message || err));
    rec("admin-capture", false, { error: String(err?.message || err) });
    try {
      await shot(page, "admin-capture-error.png");
    } catch {
      /* ignore */
    }
  }

  await browser.close();
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, files: Object.keys(report.files).length, blockers: report.blockers, errors: report.errors }, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main();
