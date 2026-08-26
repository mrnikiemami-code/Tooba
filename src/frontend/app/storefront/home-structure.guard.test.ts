import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const homeSource = fs.readFileSync(path.join(root, "app/storefront/storefront-home.tsx"), "utf8");
const repairSource = fs.readFileSync(path.join(root, "app/storefront/storefront-home-repair-sections.tsx"), "utf8");
const combinedSource = `${homeSource}\n${repairSource}`;

const REQUIRED_MARKERS = [
  'data-testid="storefront-home"',
  'data-testid="home-hero"',
  'data-testid="home-stories"',
  'data-testid="home-categories"',
  'testId="home-flash-sales"',
  'data-testid="home-best-sellers"',
  'testId="home-most-viewed"',
  'data-testid="home-middle-banners"',
  'data-testid="home-brands"',
  'data-testid="home-new-products"',
  'data-testid="home-testimonials"',
  'data-testid="home-articles"',
  'home-new-products-carousel',
  'modules={[FreeMode, Autoplay]}',
] as const;

const ORDER_MARKERS = [
  'data-testid="home-hero"',
  'data-testid="home-stories"',
  'data-testid="home-categories"',
  'testId="home-flash-sales"',
  '<HomeBestSellersSection',
  'testId="home-most-viewed"',
  'data-testid="home-middle-banners"',
  '<HomeBrandsSection',
  '<HomeNewProductsSection',
  '<HomeTestimonialsSection',
  '<HomeArticlesSection',
] as const;

test("home guard keeps Shopeiva section markers", () => {
  for (const marker of REQUIRED_MARKERS) {
    assert.ok(combinedSource.includes(marker), `missing Home marker: ${marker}`);
  }
});

test("home guard preserves section order in main composition", () => {
  const start = homeSource.indexOf("return (");
  const composition = homeSource.slice(start, homeSource.indexOf("function HomeHeroSlider"));
  let cursor = -1;
  for (const marker of ORDER_MARKERS) {
    const next = composition.indexOf(marker, cursor + 1);
    assert.ok(next > cursor, `Home order broken around ${marker}`);
    cursor = next;
  }
});

test("home guard rejects giant catalog dump on Home rail", () => {
  assert.equal(homeSource.includes('data-testid="home-all-categories"'), false);
  assert.match(homeSource, /homeCategories\.map/);
  assert.doesNotMatch(homeSource, /\{categories\.map\(/);
});
