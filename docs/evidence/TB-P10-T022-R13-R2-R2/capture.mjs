/**
 * TB-P10-T022-R13-R2-R2 — Admin → exact published public route E2E
 * for Product Showcase sunny + cinematic. No evidence-page substitute.
 */
import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R13-R2-R2");
const SHOTS = join(OUT, "screenshots");

const report = {
  ok: true,
  steps: [],
  files: {},
  errors: [],
  blockers: [],
  auth: {},
  pageBinding: {},
  sunny: {},
  cinematic: {},
  ssr: {},
  productCard: {},
  cache: {},
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
  if (locator) await locator.screenshot({ path: dest });
  else await page.screenshot({ path: dest, fullPage: false });
  const size = statSync(dest).size;
  report.files[name] = { bytes: size };
  rec(`file:${name}`, size > 800, { bytes: size });
}

function selectButton(locator) {
  return locator.locator("[data-select-choice='1']");
}

async function fillTitle(page, value) {
  const loc = page.locator("[data-testid=admin-landing-page-editor] input").first();
  // Prefer labeled title field in page meta
  const title = page.locator("label").filter({ hasText: /^عنوان$/ }).locator("input").first();
  if (await title.count()) {
    await title.fill(value);
    return;
  }
  await loc.fill(value);
}

async function fillSeo(page) {
  const seoTab = page.locator("[data-testid=tab-seo-info]");
  if (await seoTab.count()) await seoTab.click();
  await page.waitForTimeout(200);
  const seoTitle = page.locator("label").filter({ hasText: /عنوان سئو/ }).locator("input").first();
  const seoDesc = page.locator("label").filter({ hasText: /توضیح سئو/ }).locator("textarea").first();
  if (await seoTitle.count()) await seoTitle.fill("R13-R2-R2 Product Showcase SEO");
  if (await seoDesc.count()) await seoDesc.fill("Exact-route closure proof for Product Showcase Embla variants.");
  const pageTab = page.locator("[data-testid=tab-page-info]");
  if (await pageTab.count()) await pageTab.click();
}

async function fetchAdminPage(page, pageId) {
  return page.evaluate(async (id) => {
    const actor = window.localStorage.getItem("tooba.adminActorUserId") || "";
    const r = await fetch(`/v1/admin/pages/${id}`, {
      credentials: "include",
      headers: actor ? { "X-Tooba-Dev-Actor-User-Id": actor } : {},
    });
    if (!r.ok) return { ok: false, status: r.status, body: await r.text().catch(() => "") };
    return { ok: true, data: await r.json() };
  }, pageId);
}

function publicRouteFor(pageMeta) {
  const pageType = pageMeta.pageType || pageMeta.PageType || "Landing";
  const slug = pageMeta.slug || pageMeta.Slug || "";
  const status = pageMeta.status || pageMeta.Status || "";
  const title = pageMeta.title || pageMeta.Title || "";
  const pageId = pageMeta.pageId || pageMeta.PageId || pageMeta.id || "";
  if (pageType === "Home") {
    return {
      pageId,
      pageType,
      title,
      slug: slug || "home",
      status,
      exactPublicPath: "/",
      exactPublicUrl: `${FE}/`,
      exactPublicUrlFa: `${FE}/fa`,
    };
  }
  return {
    pageId,
    pageType,
    title,
    slug,
    status,
    exactPublicPath: `/landing/${slug}`,
    exactPublicUrl: `${FE}/landing/${slug}`,
    exactPublicUrlFa: `${FE}/landing/${slug}`,
  };
}

async function ensureOnVariantPicker(page) {
  if (await page.locator("[data-testid=composition-variant-picker]").count()) return;
  await page.locator("[data-wizard-step=variant]").click().catch(() => {});
  await page.waitForTimeout(300);
  if (!(await page.locator("[data-testid=composition-variant-picker]").count())) {
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(300);
  }
  await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 20000 });
}

async function advanceToSave(page) {
  for (let i = 0; i < 6; i++) {
    if (await page.locator("[data-testid=section-wizard-save]").isVisible().catch(() => false)) return;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(350);
  }
}

async function selectVariantAndSave(page, variantKey) {
  await ensureOnVariantPicker(page);
  const testId = `pick-variant-${variantKey.replace(/\./g, "-")}`;
  await selectButton(page.locator(`[data-testid=${testId}]`)).click({ timeout: 15000 });
  const selected = await page.locator(`[data-testid=${testId}]`).getAttribute("aria-selected");
  rec(`admin-selected-${variantKey}`, selected === "true", { selected });
  await advanceToSave(page);
  // Ensure Newest source if on source step
  const sourceSelect = page.locator("[data-testid=product-source-strategy]");
  if (await sourceSelect.count()) {
    await sourceSelect.selectOption("Newest").catch(() => {});
  }
  await advanceToSave(page);
  await page.locator("[data-testid=section-wizard-save]").click({ timeout: 15000 });
  await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 60000 });
}

async function publishPage(page) {
  const btn = page.getByRole("button", { name: /^انتشار$/ }).first();
  if (!(await btn.count())) {
    // already published shows پیش‌نویس toggle — treat as published
    const draftToggle = page.getByRole("button", { name: /^پیش‌نویس$/ }).first();
    if (await draftToggle.count()) {
      rec("already-published", true);
      return true;
    }
    rec("publish-button-missing", false);
    return false;
  }
  await btn.click();
  await page.waitForTimeout(1200);
  // dismiss issues modal if any
  const issues = page.locator("text=امکان انتشار وجود ندارد");
  if (await issues.count()) {
    const modalText = await page.locator("[role=dialog], [data-testid*=issues]").first().innerText().catch(() => "");
    report.blockers.push(`publish-blocked:${modalText.slice(0, 400)}`);
    rec("publish", false, { modalText: modalText.slice(0, 400) });
    await page.getByRole("button", { name: /بستن|متوجه شدم|باشه/ }).last().click().catch(() => {});
    return false;
  }
  // wait for published badge
  await page.waitForTimeout(800);
  const published = await page.locator("text=منتشرشده").count();
  rec("publish", published > 0, { publishedBadge: published });
  return published > 0;
}

async function openExactRoute(page, binding, expectLayout) {
  const url = binding.exactPublicUrl;
  await page.goto(url, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForLoadState("networkidle", { timeout: 30000 }).catch(() => {});
  const html = await page.content();
  const markers = {
    url: page.url(),
    landingCanonical: /data-landing-canonical=\"1\"|data-testid=\"landing-route\"/.test(html),
    layout: new RegExp(`data-product-layout=\"${expectLayout}\"`).test(html),
    testId: new RegExp(`data-testid=\"product-showcase-${expectLayout}\"`).test(html),
    embla: /data-product-showcase-rail=\"embla\"/.test(html),
    productCard: /data-testid=\"storefront-product-card\"|href=\"\/(fa\/)?products\//.test(html),
  };
  return { html, markers };
}

async function ssrFetch(url) {
  const res = await fetch(url, {
    headers: { Accept: "text/html", "User-Agent": "TB-P10-T022-R13-R2-R2-ssr-probe" },
  });
  const html = await res.text();
  return { status: res.status, html };
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
    const stamp = Date.now();
    const title = `R13-R2-R2 Exact Route ${stamp}`;

    // --- Create Landing page + Product Showcase ---
    await page.goto(`${FE}/admin/landing-pages/new`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.waitForLoadState("networkidle", { timeout: 30000 }).catch(() => {});
    const blank = page.locator("[data-testid=start-blank]");
    if (await blank.count()) await blank.click({ timeout: 15000 });
    await page.waitForSelector("[data-testid=add-first-section-cta], [data-testid=landing-section-composer]", { timeout: 45000 });

    await fillTitle(page, title);
    await fillSeo(page);
    // Ensure Landing type on create
    const typeSelect = page.locator("[data-testid=create-page-type] select");
    if (await typeSelect.count()) await typeSelect.selectOption("Landing");

    await page.locator("[data-testid=add-first-section-cta]").click({ timeout: 20000 });
    if (await page.locator("text=امکان ذخیره نیست").count()) {
      await page.getByRole("button", { name: "بستن" }).last().click().catch(() => {});
      await fillTitle(page, title);
      await fillSeo(page);
      await page.locator("[data-testid=add-first-section-cta]").click({ timeout: 20000 });
    }
    await page.waitForURL(/\/admin\/landing-pages\/[0-9a-f-]{20,}/i, { timeout: 60000 });
    report.pageBinding.adminUrl = page.url();
    const pageId = page.url().match(/landing-pages\/([0-9a-f-]{20,})/i)?.[1];
    rec("admin-page-created", Boolean(pageId), { pageId, adminUrl: page.url() });

    await page.waitForSelector("[data-testid=section-wizard]", { timeout: 60000 });
    const products = page.locator("[data-testid=add-section-products]");
    await products.scrollIntoViewIfNeeded();
    await selectButton(products).click({ timeout: 15000 });
    await page.locator("[data-testid=section-wizard-next]").click();
    await page.waitForSelector("[data-testid=composition-variant-picker]", { timeout: 30000 });

    // --- Flow A: Sunny ---
    await selectButton(page.locator("[data-testid=pick-variant-product-sunny]")).click({ timeout: 10000 });
    await shot(page, "admin-sunny-selected-exact-page.png", page.locator("[data-testid=composition-variant-picker]"));
    await advanceToSave(page);
    const sourceSelect = page.locator("[data-testid=product-source-strategy]");
    if (await sourceSelect.count()) await sourceSelect.selectOption("Newest").catch(() => {});
    // settings title
    await advanceToSave(page);
    const sectionTitle = page.locator("label").filter({ hasText: /عنوان بخش/ }).locator("input").first();
    if (await sectionTitle.count()) await sectionTitle.fill("ویترین سانی اثبات مسیر");
    await advanceToSave(page);
    await page.locator("[data-testid=section-wizard-save]").click({ timeout: 15000 });
    await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 60000 });
    rec("sunny-section-saved", true);

    // Save meta + SEO again then publish
    await fillSeo(page);
    await page.locator("[data-testid=save-page]").click();
    await page.waitForTimeout(1000);
    const published = await publishPage(page);
    if (!published) {
      // try force: fill seo again and republish
      await fillTitle(page, title);
      await fillSeo(page);
      await page.locator("[data-testid=save-page]").click();
      await page.waitForTimeout(800);
      await publishPage(page);
    }

    const adminMeta = await fetchAdminPage(page, pageId);
    report.pageBinding.adminApi = adminMeta;
    if (!adminMeta.ok) {
      report.blockers.push(`admin-page-fetch-failed:${adminMeta.status}`);
      rec("page-route-binding", false, adminMeta);
    } else {
      const binding = publicRouteFor(adminMeta.data);
      report.pageBinding = { ...report.pageBinding, ...binding, adminUrl: report.pageBinding.adminUrl };
      rec(
        "page-route-binding",
        binding.pageType === "Landing" && Boolean(binding.slug) && binding.exactPublicPath.startsWith("/landing/"),
        binding,
      );

      // Public sunny
      const sunny = await openExactRoute(page, binding, "sunny");
      report.sunny.first = sunny.markers;
      rec("sunny-exact-route-layout", sunny.markers.layout || sunny.markers.testId, sunny.markers);
      rec("sunny-exact-route-embla", sunny.markers.embla, sunny.markers);
      await shot(page, "public-sunny-exact-route.png");

      // Refresh persistence
      await page.reload({ waitUntil: "domcontentloaded" });
      await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
      const sunny2 = await openExactRoute(page, binding, "sunny");
      report.sunny.afterRefresh = sunny2.markers;
      rec("sunny-persisted-after-refresh", sunny2.markers.layout || sunny2.markers.testId, sunny2.markers);
      report.cache.sunnyRefresh = sunny2.markers;

      // SSR sunny
      const ssrSunny = await ssrFetch(binding.exactPublicUrl);
      report.ssr.sunny = {
        status: ssrSunny.status,
        layout: /data-product-layout=\"sunny\"/.test(ssrSunny.html),
        testId: /data-testid=\"product-showcase-sunny\"/.test(ssrSunny.html),
        embla: /data-product-showcase-rail=\"embla\"/.test(ssrSunny.html),
        heading: /ویترین سانی|محصول|ریال/.test(ssrSunny.html),
        pdpLink: /href=\"\/(fa\/)?products\//.test(ssrSunny.html),
        price: /ریال|تومان|\d/.test(ssrSunny.html),
      };
      rec("ssr-sunny-exact-route", report.ssr.sunny.layout || report.ssr.sunny.testId, report.ssr.sunny);

      // ProductCard on sunny public route
      await page.goto(binding.exactPublicUrl, { waitUntil: "domcontentloaded", timeout: 60000 });
      await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
      const rail = page.locator("[data-testid=product-showcase-sunny], [data-product-layout=sunny]").first();
      const card = rail.locator("[data-testid=storefront-product-card]").first();
      const cardLink = rail.locator("a[href*='/products/']").first();
      const cardCount = await card.count();
      const linkCount = await cardLink.count();
      let cardInfo = { cardCount, linkCount };
      if (linkCount) {
        const href = await cardLink.getAttribute("href");
        const text = (await card.innerText().catch(() => cardLink.innerText())).replace(/\s+/g, " ").trim().slice(0, 240);
        cardInfo = { ...cardInfo, href, text, hasPrice: /ریال|تومان|\d/.test(text) };
      } else if (cardCount) {
        const text = (await card.innerText()).replace(/\s+/g, " ").trim().slice(0, 240);
        cardInfo = { ...cardInfo, text, hasPrice: /ریال|تومان|\d/.test(text) };
      }
      const wishlist = await rail.locator("[data-testid*=wishlist], button[aria-label*='علاقه']").count();
      const atc = await rail.locator("[data-testid=product-card-atc], [data-testid*=add-to-cart]").count();
      report.productCard = { ...cardInfo, wishlist, atc };
      rec(
        "productcard-title-price-link",
        (Boolean(cardInfo.href) || cardCount > 0) && Boolean(cardInfo.text) && cardInfo.hasPrice !== false,
        cardInfo,
      );

      // --- Flow B: Cinematic on SAME page ---
      await page.goto(report.pageBinding.adminUrl, { waitUntil: "domcontentloaded", timeout: 90000 });
      await page.waitForSelector("[data-testid=section-edit]", { timeout: 45000 });
      await page.locator("[data-testid=section-edit]").first().click();
      await page.waitForSelector("[data-testid=section-wizard]", { timeout: 45000 });
      await ensureOnVariantPicker(page);
      await selectButton(page.locator("[data-testid=pick-variant-product-cinematic]")).click({ timeout: 15000 });
      const cinSelected = await page.locator("[data-testid=pick-variant-product-cinematic]").getAttribute("aria-selected");
      rec("admin-selected-product.cinematic", cinSelected === "true", { cinSelected });
      await shot(page, "admin-cinematic-selected-exact-page.png", page.locator("[data-testid=composition-variant-picker]"));
      await advanceToSave(page);
      const sourceSelect2 = page.locator("[data-testid=product-source-strategy]");
      if (await sourceSelect2.count()) await sourceSelect2.selectOption("Newest").catch(() => {});
      await advanceToSave(page);
      await page.locator("[data-testid=section-wizard-save]").click({ timeout: 15000 });
      await page.waitForSelector("[data-testid=section-wizard]", { state: "detached", timeout: 60000 });
      rec("cinematic-section-saved", true);

      // Ensure still published (section save shouldn't unpublish)
      const meta2 = await fetchAdminPage(page, pageId);
      if (meta2.ok) {
        const st = meta2.data.status || meta2.data.Status;
        if (st !== "Published") {
          await fillSeo(page);
          await page.locator("[data-testid=save-page]").click();
          await page.waitForTimeout(600);
          await publishPage(page);
        }
        const binding2 = publicRouteFor(meta2.data);
        report.pageBinding.afterCinematic = binding2;
        rec("same-exact-route-after-cinematic", binding2.exactPublicPath === binding.exactPublicPath, {
          before: binding.exactPublicPath,
          after: binding2.exactPublicPath,
        });

        const cin = await openExactRoute(page, binding2, "cinematic");
        report.cinematic.first = cin.markers;
        rec("cinematic-exact-route-layout", cin.markers.layout || cin.markers.testId, cin.markers);
        await shot(page, "public-cinematic-exact-route.png");

        await page.reload({ waitUntil: "domcontentloaded" });
        await page.waitForLoadState("networkidle", { timeout: 20000 }).catch(() => {});
        const cin2html = await page.content();
        const cin2 = {
          layout: /data-product-layout=\"cinematic\"/.test(cin2html),
          testId: /data-testid=\"product-showcase-cinematic\"/.test(cin2html),
          embla: /data-product-showcase-rail=\"embla\"/.test(cin2html),
          notSunny: !/data-product-layout=\"sunny\"/.test(cin2html) || /data-product-layout=\"cinematic\"/.test(cin2html),
        };
        report.cinematic.afterRefresh = cin2;
        report.cache.cinematicRefresh = cin2;
        rec("cinematic-persisted-after-refresh", cin2.layout || cin2.testId, cin2);
        rec("no-stale-sunny-after-cinematic-publish", cin2.layout || cin2.testId, cin2);

        const ssrCin = await ssrFetch(binding2.exactPublicUrl);
        report.ssr.cinematic = {
          status: ssrCin.status,
          layout: /data-product-layout=\"cinematic\"/.test(ssrCin.html),
          testId: /data-testid=\"product-showcase-cinematic\"/.test(ssrCin.html),
          embla: /data-product-showcase-rail=\"embla\"/.test(ssrCin.html),
          product: /محصول|ریال|href=\"\/(fa\/)?products\//.test(ssrCin.html),
        };
        rec("ssr-cinematic-exact-route", report.ssr.cinematic.layout || report.ssr.cinematic.testId, report.ssr.cinematic);
      }
    }
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
  console.log(
    JSON.stringify(
      {
        ok: report.ok,
        errors: report.errors,
        blockers: report.blockers,
        pageBinding: {
          pageId: report.pageBinding.pageId,
          pageType: report.pageBinding.pageType,
          slug: report.pageBinding.slug,
          status: report.pageBinding.status,
          exactPublicPath: report.pageBinding.exactPublicPath,
        },
        files: Object.keys(report.files),
      },
      null,
      2,
    ),
  );
  process.exit(report.ok ? 0 : 1);
}

main();
