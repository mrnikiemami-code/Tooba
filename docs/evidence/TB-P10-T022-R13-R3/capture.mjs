/**
 * TB-P10-T022-R13-R3 — distinct variant static + autoplay + Admin→Storefront proof.
 */
import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R13-R3");
const SHOTS = join(OUT, "screenshots");

const VARIANTS = [
  { key: "product.sunny", layout: "sunny", label: "سانی", delay: 5200, testId: "pick-variant-product-sunny" },
  { key: "product.money", layout: "money", label: "مانی", delay: 3800, testId: "pick-variant-product-money" },
  { key: "product.cinematic", layout: "cinematic", label: "سینمایی", delay: 6200, testId: "pick-variant-product-cinematic" },
  { key: "product.cinematic-plus", layout: "cinematic-plus", label: "سینمایی پلاس", delay: 7000, testId: "pick-variant-product-cinematic-plus" },
  { key: "product.explorer", layout: "explorer", label: "کاشف", delay: 4700, testId: "pick-variant-product-explorer" },
];

const report = {
  ok: true,
  steps: [],
  files: {},
  errors: [],
  blockers: [],
  auth: {},
  pageBinding: {},
  autoplay: [],
  variants: {},
};

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
  try {
    if (locator) {
      const count = await locator.count();
      if (count > 0) await locator.first().screenshot({ path: dest, timeout: 15000 });
      else await page.screenshot({ path: dest, fullPage: false });
    } else {
      await page.screenshot({ path: dest, fullPage: false });
    }
  } catch {
    await page.screenshot({ path: dest, fullPage: false });
  }
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 800, { bytes: size });
}

function selectButton(locator) {
  return locator.locator("[data-select-choice='1']");
}

async function chooseVariant(page, testId) {
  const card = page.locator(`[data-testid=${testId}]`);
  await card.scrollIntoViewIfNeeded();
  const btn = selectButton(card);
  if (await btn.count()) {
    await btn.click({ timeout: 10000 });
    return;
  }
  await card.click({ timeout: 10000 });
}

async function fillTitle(page, value) {
  const title = page.locator("label").filter({ hasText: /^عنوان$/ }).locator("input").first();
  if (await title.count()) await title.fill(value);
}

async function fillSeo(page) {
  await page.locator("[data-testid=tab-seo-info]").click().catch(() => {});
  await page.waitForTimeout(200);
  const seoTitle = page.locator("label").filter({ hasText: /عنوان سئو/ }).locator("input").first();
  const seoDesc = page.locator("label").filter({ hasText: /توضیح سئو/ }).locator("textarea").first();
  if (await seoTitle.count()) await seoTitle.fill("R13-R3 Distinct Showcase SEO");
  if (await seoDesc.count()) await seoDesc.fill("Distinct Product Showcase variant visual and autoplay proof.");
  await page.locator("[data-testid=tab-page-info]").click().catch(() => {});
}

async function fetchAdminPage(page, pageId) {
  return page.evaluate(async (id) => {
    const actor = window.localStorage.getItem("tooba.adminActorUserId") || "";
    const r = await fetch(`/v1/admin/pages/${id}`, {
      credentials: "include",
      headers: actor ? { "X-Tooba-Dev-Actor-User-Id": actor } : {},
    });
    if (!r.ok) return { ok: false, status: r.status };
    return { ok: true, data: await r.json() };
  }, pageId);
}

async function ensureVariantPicker(page) {
  if (await page.locator("[data-testid=composition-variant-picker]").count()) return;
  await page.locator("[data-wizard-step=variant]").click().catch(() => {});
  await page.waitForTimeout(300);
  if (!(await page.locator("[data-testid=composition-variant-picker]").count())) {
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
  }
  await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 20000 });
}

async function advanceToSave(page) {
  for (let i = 0; i < 7; i++) {
    if (await page.locator("[data-testid=section-wizard-save]").isVisible().catch(() => false)) return;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(300);
  }
}

async function selectVariantSave(page, v) {
  await ensureVariantPicker(page);
  await chooseVariant(page, v.testId);
  const selected = await page.locator(`[data-testid=${v.testId}]`).getAttribute("aria-selected");
  rec(`admin-selected-${v.layout}`, selected === "true", { selected });
  await advanceToSave(page);
  const source = page.locator("[data-testid=product-source-strategy]");
  if (await source.count()) await source.selectOption("Newest").catch(() => {});
  await advanceToSave(page);
  await page.locator("[data-testid=section-wizard-save]").click({ timeout: 15000 });
  await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 60000 });
  await page.locator("[data-testid=save-page]").click().catch(() => {});
  await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
  await page.waitForTimeout(800);
  // Confirm config persisted via Admin API before public proof.
  const pageId = page.url().match(/landing-pages\/([0-9a-f-]{20,})/i)?.[1];
  if (pageId) {
    for (let i = 0; i < 8; i++) {
      const sections = await page.evaluate(async (id) => {
        const actor = window.localStorage.getItem("tooba.adminActorUserId") || "";
        const r = await fetch(`/v1/admin/pages/${id}/sections`, {
          headers: actor ? { "X-Tooba-Dev-Actor-User-Id": actor } : {},
        });
        return r.ok ? await r.json() : null;
      }, pageId);
      const cfg = sections?.[0]?.config ? JSON.parse(sections[0].config) : null;
      if (cfg?.variantKey === v.key) break;
      await page.waitForTimeout(400);
    }
  }
}

async function publishIfNeeded(page) {
  const pub = page.getByRole("button", { name: /^انتشار$/ }).first();
  if (await pub.count()) {
    await pub.click();
    await page.waitForTimeout(1000);
  }
  const badge = await page.locator("text=منتشرشده").count();
  rec("published", badge > 0 || (await page.getByRole("button", { name: /^پیش‌نویس$/ }).count()) > 0, { badge });
}

async function openEditSection(page, adminUrl) {
  await page.goto(adminUrl, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForSelector("[data-testid=section-edit]", { timeout: 45000 });
  await page.locator("[data-testid=section-edit]").first().click();
  await page.waitForSelector("[data-testid=section-wizard]", { timeout: 45000 });
}

async function main() {
  const hostOk = await fetch(`${HOST}/health`).then((r) => r.ok).catch(() => false);
  const feOk = await fetch(`${FE}/fa`).then((r) => r.ok).catch(() => false);
  rec("host-5088", hostOk);
  rec("fe-3000", feOk);
  const devCtx = await fetch(`${HOST}/v1/admin/dev-context`).then((r) => r.json()).catch(() => null);
  report.auth.devContext = devCtx;
  rec("dev-actor", Boolean(devCtx?.actorUserId));
  if (!hostOk || !feOk || !devCtx?.actorUserId) {
    report.blockers.push("runtime unavailable");
    writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
    process.exit(2);
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" });
  await context.addInitScript((id) => localStorage.setItem("tooba.adminActorUserId", id), devCtx.actorUserId);
  const page = await context.newPage();

  try {
    const stamp = Date.now();
    const title = `R13-R3 Distinct ${stamp}`;
    await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.waitForLoadState("networkidle", { timeout: 30000 }).catch(() => {});
    if (await page.locator("[data-testid=start-blank]").count()) await page.locator("[data-testid=start-blank]").click();
    await page.waitForSelector("[data-testid=add-first-section-cta]", { timeout: 45000 });
    await fillTitle(page, title);
    await fillSeo(page);
    const typeSelect = page.locator("[data-testid=create-page-type] select");
    if (await typeSelect.count()) await typeSelect.selectOption("Landing");
    await page.locator("[data-testid=add-first-section-cta]").click({ timeout: 20000 });
    await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]{20,}/i, { timeout: 60000 });
    const adminUrl = page.url();
    const pageId = adminUrl.match(/landing-pages\/([0-9a-f-]{20,})/i)?.[1];
    report.pageBinding.adminUrl = adminUrl;
    report.pageBinding.pageId = pageId;
    await page.waitForSelector("[data-testid=section-wizard]", { timeout: 60000 });
    await selectButton(page.locator("[data-testid=add-section-products]")).click();
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 30000 });
    await shot(page, "admin-picker-five-new-distinct-previews.png", page.locator("[data-testid=composition-variant-picker]"));

    // First save sunny path through review for cinematic-plus review shot later
    await chooseVariant(page, "pick-variant-product-sunny");
    await advanceToSave(page);
    const source = page.locator("[data-testid=product-source-strategy]");
    if (await source.count()) await source.selectOption("Newest").catch(() => {});
    await advanceToSave(page);
    await page.locator("[data-testid=section-wizard-save]").click();
    await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 60000 });
    await fillSeo(page);
    await page.locator("[data-testid=save-page]").click();
    await page.waitForTimeout(800);
    await publishIfNeeded(page);

    const meta = await fetchAdminPage(page, pageId);
    if (!meta.ok) throw new Error(`admin fetch ${meta.status}`);
    const slug = meta.data.slug || meta.data.Slug;
    const publicUrl = `${FE}/landing/${slug}`;
    report.pageBinding.slug = slug;
    report.pageBinding.exactPublicPath = `/landing/${slug}`;
    report.pageBinding.exactPublicUrl = publicUrl;
    rec("page-route-binding", Boolean(slug), report.pageBinding);

    // Cinematic-plus review shot
    await openEditSection(page, adminUrl);
    await ensureVariantPicker(page);
    await chooseVariant(page, "pick-variant-product-cinematic-plus");
    for (let i = 0; i < 6; i++) {
      if (await page.locator("[data-testid=section-wizard-preview]").count()) break;
      await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
      await page.waitForTimeout(350);
    }
    await page.waitForSelector("[data-testid=section-wizard-preview]", { timeout: 20000 }).catch(() => {});
    if (await page.locator("[data-testid=section-wizard-preview]").count()) {
      await shot(page, "admin-review-cinematic-plus.png", page.locator("[data-testid=section-wizard-preview]"));
    }
    await page.locator("[data-testid=section-wizard-save]").click().catch(async () => {
      await advanceToSave(page);
      await page.locator("[data-testid=section-wizard-save]").click();
    });
    await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 60000 }).catch(() => {});

    for (const v of VARIANTS) {
      await openEditSection(page, adminUrl);
      await selectVariantSave(page, v);
      let found = false;
      for (let attempt = 0; attempt < 12; attempt++) {
        // Prefer Host/FE SSR fetch first — avoids client hydration races.
        const ssr = await fetch(publicUrl, { headers: { Accept: "text/html" } }).then((r) => r.text()).catch(() => "");
        if (ssr.includes(`data-product-layout="${v.layout}"`)) {
          found = true;
          break;
        }
        await page.waitForTimeout(700);
      }
      if (!found) {
        // Last resort: browser navigation
        for (let attempt = 0; attempt < 5; attempt++) {
          await page.goto(publicUrl, { waitUntil: "domcontentloaded", timeout: 90000 });
          await page.waitForLoadState("networkidle", { timeout: 25000 }).catch(() => {});
          const hit = await page.locator(`[data-product-layout="${v.layout}"]`).count();
          if (hit > 0) {
            found = true;
            break;
          }
          await page.waitForTimeout(700);
        }
      } else {
        await page.goto(publicUrl, { waitUntil: "domcontentloaded", timeout: 90000 });
        await page.waitForLoadState("networkidle", { timeout: 25000 }).catch(() => {});
      }
      rec(`public-layout-${v.layout}`, found);
      if (!found) throw new Error(`public layout missing: ${v.layout}`);
      await page.mouse.move(2, 2);
      // Wait for Embla client hydration markers (SSR also emits data-slide-active).
      await page.waitForFunction(
        (layout) => {
          const root = document.querySelector(`[data-product-layout="${layout}"]`);
          return Boolean(root?.querySelector('[data-slide-active="true"]'));
        },
        v.layout,
        { timeout: 45000 },
      );
      const beforeIdx = await page.locator(`[data-product-layout="${v.layout}"] [data-slide-active="true"]`).first().getAttribute("data-slide-index");
      const autoAttr = await page.locator(`[data-product-layout="${v.layout}"]`).getAttribute("data-product-showcase-autoplay");
      await shot(page, `${v.layout}-static.png`);
      // Keep pointer off the rail so stopOnMouseEnter does not freeze autoplay during proof.
      await page.mouse.move(2, 2);
      const t0 = Date.now();
      // Wait slightly longer than configured delay without interaction
      await page.waitForTimeout(v.delay + 1500);
      const afterIdx = await page.locator(`[data-product-layout="${v.layout}"] [data-slide-active="true"]`).getAttribute("data-slide-index");
      const elapsed = Date.now() - t0;
      await shot(page, `${v.layout}-autoplay-after.png`);
      const advanced = beforeIdx != null && afterIdx != null && beforeIdx !== afterIdx;
      report.autoplay.push({
        variant: v.key,
        layout: v.layout,
        beforeIdx,
        afterIdx,
        elapsed,
        autoAttr,
        advanced,
      });
      rec(`autoplay-${v.layout}`, advanced, { beforeIdx, afterIdx, elapsed, autoAttr });
      report.variants[v.layout] = { beforeIdx, afterIdx, autoAttr };

      if (v.layout === "cinematic" || v.layout === "cinematic-plus" || v.layout === "explorer") {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.reload({ waitUntil: "domcontentloaded" });
        await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
        await page.waitForSelector(`[data-product-layout="${v.layout}"]`, { timeout: 20000 });
        await shot(page, `${v.layout}-mobile.png`);
        await page.setViewportSize({ width: 1440, height: 900 });
      }
    }

    // Existing variants still present in picker
    await openEditSection(page, adminUrl);
    await ensureVariantPicker(page);
    const pickerText = await page.locator("[data-testid=composition-variant-picker]").innerText();
    for (const label of ["آریا", "زهره", "ماهور", "شگفت"]) {
      rec(`existing-preserved:${label}`, pickerText.includes(label));
    }
    await page.keyboard.press("Escape").catch(() => {});
  } catch (err) {
    report.blockers.push(String(err?.message || err));
    rec("capture-main", false, { error: String(err?.message || err) });
    try {
      await shot(page, "capture-error.png");
    } catch {
      /* ignore */
    }
  }

  await browser.close();
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  const autoplayMd = [
    "# Autoplay Proof — TB-P10-T022-R13-R3",
    "",
    "No manual drag/click during observation windows.",
    "",
    "| Variant | Before | After | Elapsed ms | Advanced |",
    "|---|---|---|---|---|",
    ...report.autoplay.map(
      (row) =>
        `| ${row.variant} | ${row.beforeIdx} | ${row.afterIdx} | ${row.elapsed} | ${row.advanced ? "YES" : "NO"} |`,
    ),
    "",
  ].join("\n");
  writeFileSync(join(OUT, "autoplay-proof.md"), autoplayMd);
  console.log(
    JSON.stringify(
      {
        ok: report.ok,
        errors: report.errors,
        blockers: report.blockers,
        files: Object.keys(report.files),
        autoplay: report.autoplay,
        pageBinding: report.pageBinding,
      },
      null,
      2,
    ),
  );
  process.exit(report.ok ? 0 : 1);
}

main();
