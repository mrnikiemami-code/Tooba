import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { DEFAULT_HOME_SECTION_ORDER } from "../composition/composition-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const homeSource = fs.readFileSync(path.join(root, "app/storefront/storefront-home.tsx"), "utf8");
const repairSource = fs.readFileSync(path.join(root, "app/storefront/storefront-home-repair-sections.tsx"), "utf8");
const storiesSource = fs.readFileSync(path.join(root, "app/storefront/stories/home-stories.tsx"), "utf8");
const combinedSource = `${homeSource}\n${repairSource}\n${storiesSource}`;

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

const SECTION_TYPE_MARKERS: Record<string, string> = {
  hero: 'data-testid="home-hero"',
  stories: "<HomeStoriesSection",
  category_grid: 'data-testid="home-categories"',
  product_rail_flash: 'testId="home-flash-sales"',
  best_sellers: "<HomeBestSellersSection",
  product_rail_most_viewed: 'testId="home-most-viewed"',
  middle_banners: 'data-testid="home-middle-banners"',
  brands: "<HomeBrandsSection",
  newest_products: "<HomeNewProductsSection",
  customer_reviews: "<HomeTestimonialsSection",
  latest_articles: "<HomeArticlesSection",
};

test("home guard keeps Shopeiva section markers", () => {
  for (const marker of REQUIRED_MARKERS) {
    assert.ok(combinedSource.includes(marker), `missing Home marker: ${marker}`);
  }
});

test("home guard preserves canonical default section order", () => {
  assert.deepEqual([...DEFAULT_HOME_SECTION_ORDER], [
    "hero",
    "stories",
    "category_grid",
    "product_rail_flash",
    "best_sellers",
    "product_rail_most_viewed",
    "middle_banners",
    "brands",
    "newest_products",
    "customer_reviews",
    "latest_articles",
  ]);
  for (const sectionType of DEFAULT_HOME_SECTION_ORDER) {
    const marker = SECTION_TYPE_MARKERS[sectionType];
    assert.ok(marker, `missing marker mapping for ${sectionType}`);
    assert.ok(homeSource.includes(marker), `missing renderer marker for ${sectionType}`);
  }
});

test("home guard rejects giant catalog dump on Home rail", () => {
  assert.equal(homeSource.includes('data-testid="home-all-categories"'), false);
  assert.match(homeSource, /homeCategories\.map/);
  assert.doesNotMatch(homeSource, /\{categories\.map\(/);
});

test("home guard keeps composition renderer switch cases", () => {
  assert.match(homeSource, /function renderHomeSection/);
  for (const sectionType of DEFAULT_HOME_SECTION_ORDER) {
    assert.match(homeSource, new RegExp(`case "${sectionType}":`));
  }
});

test("home stories use live Host binding without fake STORY_IMAGES", () => {
  assert.doesNotMatch(homeSource, /STORY_IMAGES/);
  assert.match(storiesSource, /fetchPublicStories/);
  assert.match(storiesSource, /data-testid="home-stories"/);
  assert.match(storiesSource, /#E53935/);
});
