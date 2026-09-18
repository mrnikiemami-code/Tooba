/**
 * TB-P10-T022-R13-R1 — Hero Slider Builder repair guards.
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  HERO_DESTINATION_TYPES,
  HERO_HEIGHT_PRESETS,
  HERO_SLIDER_VARIANTS,
  heroImageGuidance,
  mapLegacyHeightPxToPreset,
  normalizeHeroSlides,
  resolveHeroSlideHref,
  validateHeroSliderDetailed,
} from "../../../lib/storefront-composition/hero-slider-config.ts";
import { implementedVariantsForSection } from "../../../lib/storefront-composition/registry.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const settings = readFileSync(join(dir, "admin-hero-slider-settings.tsx"), "utf8");
const wizard = readFileSync(join(dir, "admin-section-wizard.tsx"), "utf8");
const blocks = readFileSync(join(dir, "../../storefront/storefront-landing-blocks.tsx"), "utf8");
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");

test("Hero picker exposes exactly six canonical variants", () => {
  assert.equal(HERO_SLIDER_VARIANTS.length, 6);
  const keys = HERO_SLIDER_VARIANTS.map((row) => row.id);
  assert.deepEqual(keys, ["fullscreen", "shapes", "diagonal", "cinematic", "split", "editorial"]);
  assert.deepEqual(
    HERO_SLIDER_VARIANTS.map((row) => row.nameFa),
    ["الماس", "سیمین", "کیمیا", "فاخته", "صبا", "عقیق"],
  );
  const implemented = implementedVariantsForSection("HeroCarousel");
  assert.equal(implemented.length, 6);
  assert.deepEqual(
    implemented.map((row) => row.key),
    HERO_SLIDER_VARIANTS.map((row) => row.variantKey),
  );
});

test("single HeroSlider/Swiper engine — no second library", () => {
  assert.match(blocks, /export function HeroSlider/);
  assert.match(blocks, /from "swiper\/react"/);
  assert.doesNotMatch(blocks, /keen-slider|splide|embla|slick/i);
  assert.equal((blocks.match(/from "swiper\/react"/g) ?? []).length, 1);
});

test("height presets only — no raw pixel admin input", () => {
  assert.deepEqual([...HERO_HEIGHT_PRESETS], ["Medium", "Large", "ExtraLarge"]);
  assert.doesNotMatch(settings, /اندازه نمایش \(پیکسل\)/);
  assert.doesNotMatch(settings, /hero-display-height/);
  assert.match(settings, /hero-height-preset/);
  assert.equal(mapLegacyHeightPxToPreset(420), "Large");
});

test("image guidance differs by variant and height", () => {
  const a = heroImageGuidance("fullscreen", "Medium");
  const b = heroImageGuidance("cinematic", "ExtraLarge");
  const c = heroImageGuidance("split", "Medium");
  assert.notEqual(a.summaryFa, b.summaryFa);
  assert.notEqual(a.aspectRatio, c.aspectRatio);
  assert.match(settings, /hero-image-guidance/);
});

test("slide SEO fake fields removed; visible title/description used", () => {
  assert.doesNotMatch(settings, /عنوان سئو اسلاید|توضیح سئو اسلاید|seoTitle|seoDescription/);
  assert.match(settings, />عنوان</);
  assert.match(settings, />توضیح</);
  assert.match(settings, /Alt تصویر/);
});

test("structured CTA destination model", () => {
  assert.deepEqual([...HERO_DESTINATION_TYPES], ["none", "all-products", "product", "category", "custom-url"]);
  assert.match(settings, /hero-slide-destination-type/);
  assert.match(settings, /AdminResourceSelector/);
  assert.equal(resolveHeroSlideHref({
    destinationType: "all-products",
    targetId: "",
    targetSlug: "",
    customUrl: "",
    href: "",
  }), "/products");
  assert.equal(resolveHeroSlideHref({
    destinationType: "category",
    targetId: "cat-1",
    targetSlug: "",
    customUrl: "",
    href: "",
  }), "/products?categoryId=cat-1");
});

test("validation summary + live contract", () => {
  const incomplete = validateHeroSliderDetailed({
    heightPreset: "Medium",
    slideIntervalSec: 5,
    slideCount: 1,
    slides: [{ title: "", alt: "", mediaAssetId: "", imageUrl: "" }],
  });
  assert.equal(incomplete.ok, false);
  assert.match(incomplete.summaryFa ?? "", /اسلاید/);
  assert.equal(incomplete.firstInvalidField, "image");

  const complete = validateHeroSliderDetailed({
    heightPreset: "Medium",
    slideIntervalSec: 5,
    slideCount: 1,
    slides: [{
      title: "عنوان",
      alt: "alt",
      mediaAssetId: "m1",
      imageUrl: "",
      description: "",
      ctaLabel: "",
      destinationType: "none",
      targetId: "",
      targetSlug: "",
      targetLabel: "",
      customUrl: "",
      href: "",
    }],
  });
  assert.equal(complete.ok, true);

  assert.match(wizard, /hero-validation-modal/);
  assert.match(wizard, /heroShowErrors/);
  assert.match(wizard, /data-step-error/);
  assert.match(settings, /data-has-error/);
});

test("legacy slide seo* and href remain adaptable", () => {
  const slides = normalizeHeroSlides([{
    title: "",
    seoTitle: "قدیمی",
    seoDescription: "توضیح قدیمی",
    alt: "a",
    imageUrl: "/x.jpg",
    href: "/products",
  }], 1);
  assert.equal(slides[0]!.title, "قدیمی");
  assert.equal(slides[0]!.description, "توضیح قدیمی");
  assert.equal(slides[0]!.destinationType, "all-products");
});

test("optional CTA and none destination do not error", () => {
  const noneOnly = validateHeroSliderDetailed({
    heightPreset: "Medium",
    slideIntervalSec: 5,
    slideCount: 1,
    slides: [{
      title: "عنوان",
      alt: "alt",
      mediaAssetId: "m1",
      imageUrl: "",
      description: "",
      ctaLabel: "مشاهده",
      destinationType: "none",
      targetId: "",
      targetSlug: "",
      targetLabel: "",
      customUrl: "",
      href: "",
    }],
  });
  assert.equal(noneOnly.ok, true);

  const destWithoutCta = validateHeroSliderDetailed({
    heightPreset: "Medium",
    slideIntervalSec: 5,
    slideCount: 1,
    slides: [{
      title: "عنوان",
      alt: "alt",
      mediaAssetId: "m1",
      imageUrl: "",
      description: "",
      ctaLabel: "",
      destinationType: "all-products",
      targetId: "",
      targetSlug: "",
      targetLabel: "",
      customUrl: "",
      href: "/products",
    }],
  });
  assert.equal(destWithoutCta.ok, true);
});

test("LOCK-SF-367…374 present", () => {
  for (const id of [367, 368, 369, 370, 371, 372, 373, 374]) {
    assert.match(locks, new RegExp(`LOCK-SF-${id}`));
  }
});
