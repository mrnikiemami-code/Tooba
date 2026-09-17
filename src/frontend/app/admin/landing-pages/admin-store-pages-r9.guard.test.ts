import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const list = readFileSync(join(dir, "admin-landing-pages-screen.tsx"), "utf8");
const composer = readFileSync(join(dir, "admin-landing-page-composer.tsx"), "utf8");
const api = readFileSync(join(dir, "admin-landing-pages-api.ts"), "utf8");
const chrome = readFileSync(join(dir, "../admin-chrome-messages.ts"), "utf8");
const landingRoute = readFileSync(join(root, "src/frontend/app/landing/[slug]/page.tsx"), "utf8");
const legacySlug = readFileSync(join(root, "src/frontend/app/[slug]/page.tsx"), "utf8");
const sitemap = readFileSync(join(root, "src/frontend/app/sitemap.ts"), "utf8");
const pageSeo = readFileSync(join(root, "src/frontend/app/storefront/storefront-page-seo.ts"), "utf8");
const sections = readFileSync(join(root, "src/frontend/app/storefront/storefront-landing-sections.tsx"), "utf8");
const wizard = readFileSync(join(dir, "admin-section-wizard.tsx"), "utf8");

test("Store Pages rename + AppDataGrid listing columns", () => {
  assert.match(chrome, /landingPages:\s*"صفحات فروشگاه"/);
  assert.match(list, /صفحات فروشگاه/);
  assert.match(list, /data-grid-profile="orders-canonical"/);
  assert.match(list, /نوع صفحه/);
  assert.match(list, /ایندکس‌پذیری/);
  assert.match(list, /صفحه اصلی؟/);
  assert.match(list, /بازگردانی صفحه اصلی پیش‌فرض/);
  assert.match(list, /تنظیم به عنوان صفحه اصلی/);
  assert.match(list, /home-current-indicator/);
  assert.match(list, /landing-home-/);
  assert.match(list, /confirm:/);
  assert.match(list, /pageType === "Landing"/);
  assert.match(api, /pageType === "Home" \? ""/);
  assert.match(api, /restoreDefaultAdminHome/);
  assert.match(api, /setAdminLandingHome\(null\)/);
});

test("create page type + SEO admin UX groups", () => {
  assert.match(composer, /create-page-type/);
  assert.match(composer, /اطلاعات صفحه/);
  assert.match(composer, /اطلاعات سئو/);
  assert.match(composer, /page-meta-tabs|tab-page-info|tab-seo-info/);
  assert.match(composer, /tab-page-warning|tab-seo-warning|AlertTriangle/);
  assert.match(composer, /LandingPublishReadinessCard|landing-publish-readiness-card/);
  assert.match(composer, /LandingPublishIssuesModal|landing-publish-issues-modal/);
  assert.match(composer, /بخش‌های صفحه|unified-section-workspace/);
  assert.match(composer, /page-seo-panel/);
  assert.match(composer, /seo-snippet-preview/);
  assert.match(composer, /تنظیم به عنوان صفحه اصلی/);
  assert.match(composer, /set-as-home/);
  assert.match(composer, /pageType === "Landing"/);
  assert.match(composer, /بازگشت به فهرست/);
  assert.match(composer, /page-workspace-actions/);
  assert.match(composer, /delete-page|deleteAdminLandingPage/);
  assert.match(composer, /hideSlugField|pageType === "Landing"/);
  assert.doesNotMatch(composer, /برای صفحات فرود باید آدرس عمومی/);
  assert.doesNotMatch(composer, /apply-template-action/);
  assert.doesNotMatch(composer, /\/admin\/landing-pages\/\$\{page\.pageId\}\/preview/);
  assert.match(api, /pageType/);
  assert.match(api, /robotsIndex/);
  assert.match(api, /ogTitle/);
  assert.match(api, /primaryH1/);
  assert.match(api, /\/landing\//);
});

test("canonical landing route + legacy redirect + sitemap", () => {
  assert.match(landingRoute, /\/landing\/\{slug\}|landing\/\[slug\]|data-testid="landing-route"/);
  assert.match(landingRoute, /buildStorePageMetadata|buildStorePageStructuredData/);
  assert.match(landingRoute, /resolvePublishedStorePage|resolveLandingRouteModel/);
  assert.match(legacySlug, /permanentRedirect/);
  assert.match(legacySlug, /\/landing\//);
  assert.match(sitemap, /listIndexableLandingPages/);
  assert.match(sitemap, /\/landing\//);
  assert.match(pageSeo, /WebPage/);
  assert.match(pageSeo, /WebSite|BreadcrumbList/);
  assert.match(sections, /store-page-primary-h1/);
});

test("section wizard has no mandatory SEO step", () => {
  assert.doesNotMatch(wizard, /step.*[Ss]eo|سئو.*گام|mandatory.*seo/i);
  assert.match(wizard, /نوع بخش|ظاهر|منبع|تنظیمات|بازبینی/);
});
