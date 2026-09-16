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
});

test("create page type + SEO admin UX groups", () => {
  assert.match(composer, /create-page-type/);
  assert.match(composer, /اطلاعات صفحه/);
  assert.match(composer, /سئو و اشتراک‌گذاری/);
  assert.match(composer, /ترکیب صفحه/);
  assert.match(composer, /page-seo-panel/);
  assert.match(composer, /seo-snippet-preview/);
  assert.match(composer, /تنظیم به عنوان صفحه اصلی/);
  assert.match(composer, /بازگردانی صفحه اصلی پیش‌فرض/);
  assert.match(api, /pageType/);
  assert.match(api, /robotsIndex/);
  assert.match(api, /ogTitle/);
  assert.match(api, /primaryH1/);
  assert.match(api, /\/landing\//);
});

test("canonical landing route + legacy redirect + sitemap", () => {
  assert.match(landingRoute, /\/landing\/\{slug\}|landing\/\[slug\]|data-testid="landing-route"/);
  assert.match(landingRoute, /buildStorePageMetadata|buildStorePageStructuredData/);
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
