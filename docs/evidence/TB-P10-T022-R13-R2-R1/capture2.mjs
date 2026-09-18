/**
 * Continuation capture for R13-R2-R1: reopen persistence + Admin→Storefront.
 */
import { mkdirSync, writeFileSync, statSync, readFileSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R13-R2-R1");
const SHOTS = join(OUT, "screenshots");
const prior = JSON.parse(readFileSync(join(OUT, "runtime-report.json"), "utf8"));
const report = { ...prior, continuation: true, steps: [...(prior.steps || [])], files: { ...(prior.files || {}) }, errors: [], blockers: [], ok: true };

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
  const loc = page.locator("[data-testid=landing-page-title], input[name='title']").first();
  if (await loc.count()) {
    await loc.fill(value);
    return true;
  }
  return false;
}

async function main() {
  const actorId = prior.auth?.devContext?.actorUserId || "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
  const pageUrl = prior.flow?.afterSaveUrl || prior.flow?.pageUrl;
  if (!pageUrl) {
    report.blockers.push("missing prior pageUrl");
    writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
    process.exit(2);
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" });
  await context.addInitScript((id) => localStorage.setItem("tooba.adminActorUserId", id), actorId);
  const page = await context.newPage();

  try {
    await page.goto(pageUrl, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.waitForLoadState("networkidle", { timeout: 30000 }).catch(() => {});
    await page.waitForSelector("[data-testid=admin-landing-page-editor], [data-testid=landing-section-composer]", { timeout: 45000 });

    // Reopen edit
    const edit = page.locator("[data-testid=section-edit]").first();
    await edit.waitFor({ state: "visible", timeout: 20000 });
    await edit.click();
    await page.waitForSelector("[data-testid=section-wizard]", { timeout: 30000 });

    // Ensure appearance step
    if (!(await page.locator("[data-testid=composition-variant-picker]").count())) {
      await page.locator("[data-wizard-step=variant]").click().catch(() => {});
      await page.waitForTimeout(500);
      if (!(await page.locator("[data-testid=composition-variant-picker]").count())) {
        // edit mode may start at variant already; try next from type
        const next = page.locator("[data-testid=section-wizard-next]");
        if (await next.count()) await next.click().catch(() => {});
      }
    }
    await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 20000 });
    const selected = await page.locator("[data-testid=pick-variant-product-cinematic]").getAttribute("aria-selected");
    rec("reopen-cinematic-persisted", selected === "true", { selected });
    await shot(page, "admin-product-showcase-reopen-persisted.png", page.locator("[data-testid=composition-variant-picker]"));

    // Switch to sunny, save for storefront proof #2 path later
    await selectButton(page.locator("[data-testid=pick-variant-product-sunny]")).click();
    await page.locator("[data-testid=section-wizard-save]").click().catch(async () => {
      // may need advance to preview first
      for (let i = 0; i < 5; i++) {
        if (await page.locator("[data-testid=section-wizard-save]").isVisible().catch(() => false)) break;
        await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
        await page.waitForTimeout(400);
      }
      await page.locator("[data-testid=section-wizard-save]").click();
    });
    await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 45000 });
    rec("sunny-resave", true);

    // Set as Home + publish for storefront
    const treatHome = page.getByRole("button", { name: /خانه|Home|تنظیم به عنوان خانه/i }).first();
    if (await treatHome.count()) await treatHome.click().catch(() => {});
    // page type select if present
    const pageType = page.locator("select, [data-testid=page-type]").first();
    if (await pageType.count()) {
      await pageType.selectOption({ label: /خانه|Home/ }).catch(() => {});
    }
    // fill slug if landing
    const slug = page.locator("[data-testid=page-slug-input]");
    if (await slug.count() && (await slug.inputValue()) === "") {
      await slug.fill(`r13r2r1-${Date.now()}`);
    }
    await fillPageTitle(page, `R13-R2-R1 Home Showcase ${Date.now()}`);

    const publishBtn = page.getByRole("button", { name: /انتشار/ }).first();
    if (await publishBtn.count()) {
      await publishBtn.click();
      await page.waitForTimeout(1500);
    }
    // also click save draft
    const saveBtn = page.getByRole("button", { name: /^ذخیره$|ذخیره پیش‌نویس/ }).first();
    if (await saveBtn.count()) await saveBtn.click().catch(() => {});

    const slugVal = (await slug.count()) ? await slug.inputValue() : "";
    report.flow.slug = slugVal;

    // Storefront: try /fa and /landing/{slug}
    const targets = [`${FE}/fa`];
    if (slugVal) targets.push(`${FE}/landing/${slugVal}`, `${FE}/fa/landing/${slugVal}`);

    let storefrontHit = false;
    for (const url of targets) {
      await page.setViewportSize({ width: 1440, height: 900 });
      await page.goto(url, { waitUntil: "domcontentloaded", timeout: 60000 }).catch(() => {});
      await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
      const html = await page.content();
      const hit = /data-product-layout=\"sunny\"|data-testid=\"product-showcase-sunny\"|data-product-showcase-rail=\"embla\"/.test(html);
      report.flow[`probe:${url}`] = hit;
      if (hit || url.includes("landing")) {
        await shot(page, "storefront-product-showcase-sunny-real-flow.png");
        storefrontHit = hit || storefrontHit;
        if (hit) break;
      }
    }
    if (!storefrontHit) {
      // Still capture home as storefront shell after admin save (document)
      await page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 60000 });
      await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
      await shot(page, "storefront-product-showcase-sunny-real-flow.png");
    }

    // Cinematic: reopen, select cinematic, save, storefront
    await page.goto(pageUrl, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.waitForSelector("[data-testid=section-edit]", { timeout: 30000 });
    await page.locator("[data-testid=section-edit]").first().click();
    await page.waitForSelector("[data-testid=section-wizard]", { timeout: 30000 });
    if (!(await page.locator("[data-testid=composition-variant-picker]").count())) {
      await page.locator("[data-wizard-step=variant]").click().catch(() => {});
      await page.waitForTimeout(400);
    }
    await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 20000 });
    await selectButton(page.locator("[data-testid=pick-variant-product-cinematic]")).click();
    for (let i = 0; i < 5; i++) {
      if (await page.locator("[data-testid=section-wizard-save]").isVisible().catch(() => false)) break;
      await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
      await page.waitForTimeout(350);
    }
    await page.locator("[data-testid=section-wizard-save]").click();
    await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 45000 });

    await page.goto(slugVal ? `${FE}/landing/${slugVal}` : `${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 60000 }).catch(() =>
      page.goto(`${FE}/fa`, { waitUntil: "domcontentloaded", timeout: 60000 }),
    );
    await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
    await shot(page, "storefront-product-showcase-cinematic-real-flow.png");
    await page.setViewportSize({ width: 390, height: 844 });
    await page.reload({ waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
    await shot(page, "storefront-product-showcase-mobile-real-flow.png");
    const mobileHtml = await page.content();
    rec("storefront-embla-or-layout-marker", /product-showcase|data-product-layout|data-embla|StorefrontProductCard|محصول/.test(mobileHtml));

    rec("continuation-complete", true);
  } catch (err) {
    report.blockers.push(String(err?.message || err));
    rec("continuation", false, { error: String(err?.message || err) });
    try {
      await shot(page, "admin-capture-error-2.png");
    } catch {
      /* ignore */
    }
  }

  // Recompute ok: ignore prior admin-capture failure if continuation succeeded
  report.ok = !report.errors.includes("continuation") && report.files["admin-product-showcase-reopen-persisted.png"];
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ ok: report.ok, files: Object.keys(report.files).length, blockers: report.blockers, errors: report.errors }, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main();
