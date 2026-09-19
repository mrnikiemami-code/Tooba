import { mkdirSync, writeFileSync, copyFileSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "../../../.tmp-r24r1r2-pw/node_modules/playwright/index.mjs";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const FE = "http://127.0.0.1:3000";
const HOST = "http://127.0.0.1:5088";
const OUT = join(ROOT, "docs/evidence/TB-P10-T022-R17");
const SHOTS = join(OUT, "screenshots");
mkdirSync(SHOTS, { recursive: true });

const report = { ok: true, steps: [], errors: [], auth: {}, pageBinding: {}, persistence: {}, preview: {}, storefront: {}, files: {} };

function rec(name, pass, data) {
  report.steps.push({ name, pass, ...(data ? { data } : {}) });
  if (!pass) {
    report.ok = false;
    report.errors.push(name);
  }
}

async function hostJson(path, { method = "GET", body, actor } = {}) {
  const headers = { Accept: "application/json" };
  if (actor) headers["X-Tooba-Dev-Actor-User-Id"] = actor;
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const r = await fetch(`${HOST}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const text = await r.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  return { ok: r.ok, status: r.status, data };
}

async function shot(page, name) {
  const dest = join(SHOTS, name);
  await page.screenshot({ path: dest, fullPage: false });
  report.files[name] = { bytes: (await import("node:fs")).statSync(dest).size };
  rec(`file:${name}`, report.files[name].bytes > 800);
}

const stamp = Date.now();
const slug = `r17-amazing-${stamp}`;
const dev = await hostJson("/v1/admin/dev-context");
report.auth.devContext = dev.data;
rec("dev-actor", Boolean(dev.data?.actorUserId));
const actor = dev.data?.actorUserId;

const created = await hostJson("/v1/admin/pages", {
  method: "POST",
  actor,
  body: {
    title: `R17 Amazing ${stamp}`,
    slug,
    locale: "fa",
    pageType: "Landing",
    seoTitle: "R17 Amazing",
    seoDescription: "PromotionCampaign source proof",
  },
});
rec("create-page", created.ok, { status: created.status, data: created.data });
const pageId = created.data?.pageId || created.data?.PageId;
report.pageBinding.pageId = pageId;
report.pageBinding.slug = slug;

const config = {
  title: "پیشنهاد شگفت‌انگیز",
  source: "PromotionCampaign",
  promotionTypeCode: "AMAZING",
  campaignId: null,
  take: 8,
  variantKey: "product.card-carousel",
};
const section = await hostJson(`/v1/admin/pages/${pageId}/sections`, {
  method: "POST",
  actor,
  body: {
    sectionType: "ProductCollection",
    config: JSON.stringify(config),
    isEnabled: true,
  },
});
rec("add-section", section.ok, { status: section.status, data: section.data });

const sections = await hostJson(`/v1/admin/pages/${pageId}/sections`, { actor });
const row = (sections.data || [])[0];
const persisted = typeof row?.config === "string" ? JSON.parse(row.config) : row?.config || (typeof row?.Config === "string" ? JSON.parse(row.Config) : {});
report.persistence = persisted;
rec("persist-PromotionCampaign", persisted.source === "PromotionCampaign");
rec("persist-AMAZING", persisted.promotionTypeCode === "AMAZING");
rec("persist-campaignId-null", persisted.campaignId == null);
rec("persist-no-member-snapshot", !Array.isArray(persisted.productIds) || persisted.productIds.length === 0);

const preview = await hostJson(`/v1/admin/pages/${pageId}/preview`, { actor });
rec("preview-ok", preview.ok, { status: preview.status });
const previewSection = (preview.data?.sections || preview.data?.Sections || []).find(
  (s) => (s.sectionType || s.SectionType) === "ProductCollection",
);
const previewItems = previewSection?.items || previewSection?.Items || [];
const previewProducts = preview.data?.products || preview.data?.Products || [];
report.preview = { itemCount: previewItems.length, productCount: previewProducts.length, firstItem: previewItems[0] };
rec("preview-has-campaign-items", previewItems.length > 0);
rec("preview-has-product-cards", previewProducts.length > 0);

const published = await hostJson(`/v1/admin/pages/${pageId}/status`, {
  method: "PUT",
  actor,
  body: { status: "Published" },
});
rec("publish", published.ok || published.status === 200, { status: published.status, data: published.data });

const publicFa = await hostJson(`/v1/storefront/pages/${slug}?locale=fa`);
rec("public-fa", publicFa.ok, { status: publicFa.status });
const faSection = (publicFa.data?.sections || publicFa.data?.Sections || []).find(
  (s) => (s.sectionType || s.SectionType) === "ProductCollection",
);
const faItems = faSection?.items || faSection?.Items || [];
const faProducts = publicFa.data?.products || publicFa.data?.Products || [];
report.storefront.fa = { itemCount: faItems.length, productCount: faProducts.length };
rec("storefront-fa-items", faItems.length > 0);
rec("storefront-fa-cards", faProducts.length > 0);

// EN page: create sibling or request with locale if supported
const publicEn = await hostJson(`/v1/storefront/pages/${slug}?locale=en`);
report.storefront.en = { status: publicEn.status, ok: publicEn.ok };

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "fa-IR" });
await context.addInitScript((id) => localStorage.setItem("tooba.adminActorUserId", id), actor);
const page = await context.newPage();
try {
  // Builder UI proof: open edit, go to source step
  await page.goto(`${FE}/admin/landing-pages/${pageId}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForLoadState("networkidle", { timeout: 30000 }).catch(() => {});
  await page.waitForSelector("[data-testid=section-edit]", { timeout: 45000 });
  await page.locator("[data-testid=section-edit]").first().click();
  await page.waitForSelector("[data-testid=section-wizard]", { timeout: 45000 });
  // navigate to source via Next clicks until strategy visible
  for (let i = 0; i < 6; i++) {
    if (await page.locator("[data-testid=product-source-strategy]").count()) break;
    if (await page.locator("[data-testid=section-wizard-next]").count()) {
      await page.locator("[data-testid=section-wizard-next]").click();
      await page.waitForTimeout(400);
    }
  }
  const options = await page.locator("[data-testid=product-source-strategy] option").allTextContents().catch(() => []);
  const value = await page.locator("[data-testid=product-source-strategy]").inputValue().catch(() => "");
  rec("ui-source-value", value === "PromotionCampaign", { value, options });
  rec("ui-amazing-label", options.some((t) => t.includes("پیشنهاد شگفت‌انگیز")), { options });
  rec("ui-no-raw-codes", !options.some((t) => /PromotionCampaign|AMAZING/.test(t)));
  await shot(page, "builder-source-amazing.png");
  // review step
  for (let i = 0; i < 4; i++) {
    if (await page.locator("[data-testid=section-wizard-save]").isVisible().catch(() => false)) break;
    await page.locator("[data-testid=section-wizard-next]").click().catch(() => {});
    await page.waitForTimeout(300);
  }
  await shot(page, "builder-review.png");
  await page.locator("[data-testid=section-wizard-save], button:has-text('بستن')").first().click().catch(() => {});
  await page.keyboard.press("Escape").catch(() => {});

  await page.goto(`${FE}/landing/${slug}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.waitForLoadState("networkidle", { timeout: 25000 }).catch(() => {});
  await shot(page, "storefront-fa.png");
  const cards = await page.locator("[data-testid=landing-products] a, [data-testid=storefront-product-card]").count();
  rec("browser-fa-cards", cards > 0, { cards });
  const ssr = await fetch(`${FE}/landing/${slug}`, { headers: { Accept: "text/html" } }).then((r) => r.text());
  rec("ssr-html-has-products", cards > 0 && /landing-products|product/i.test(ssr));
  rec("no-fake-59-discount", !/۵۹٪|59%/.test(ssr));

  // EN landing clone via API
  const enSlug = `${slug}-en`;
  const enPage = await hostJson("/v1/admin/pages", {
    method: "POST",
    actor,
    body: {
      title: `R17 Amazing EN ${stamp}`,
      slug: enSlug,
      locale: "en",
      pageType: "Landing",
      seoTitle: "R17 Amazing EN",
      seoDescription: "EN PromotionCampaign",
    },
  });
  const enId = enPage.data?.pageId || enPage.data?.PageId;
  if (enId) {
    await hostJson(`/v1/admin/pages/${enId}/sections`, {
      method: "POST",
      actor,
      body: {
        sectionType: "ProductCollection",
        config: JSON.stringify({ ...config, title: "Amazing Offers" }),
        isEnabled: true,
      },
    });
    await hostJson(`/v1/admin/pages/${enId}/status`, { method: "PUT", actor, body: { status: "Published" } });
    await page.goto(`${FE}/en/landing/${enSlug}`, { waitUntil: "domcontentloaded", timeout: 90000 }).catch(async () => {
      await page.goto(`${FE}/landing/${enSlug}`, { waitUntil: "domcontentloaded", timeout: 90000 });
    });
    await page.waitForTimeout(1500);
    await shot(page, "storefront-en.png");
    const enCards = await page.locator("[data-testid=landing-products] a, [data-testid=storefront-product-card]").count();
    report.storefront.enBrowserCards = enCards;
    rec("browser-en-cards", enCards > 0, { enCards });
  } else {
    rec("browser-en-cards", false, { reason: "en page create failed", data: enPage });
  }
} catch (e) {
  report.ok = false;
  report.errors.push(String(e));
  await shot(page, "capture-error.png").catch(() => {});
} finally {
  await browser.close();
  writeFileSync(join(OUT, "runtime-report.json"), JSON.stringify(report, null, 2));
  writeFileSync(join(OUT, "capture.log"), JSON.stringify(report, null, 2));
}

process.exit(report.ok && report.errors.length === 0 ? 0 : 1);
