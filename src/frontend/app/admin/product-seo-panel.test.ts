import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  draftFromSeoDetail,
  formatSeoReadinessLabel,
  isSeoDraftDirty,
  mapSeoDetail,
  resolveSeoPreviewTitle,
  type ProductSeoDetail,
} from "./product-seo-panel-model.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));

function sampleDetail(partial: Partial<ProductSeoDetail> = {}): ProductSeoDetail {
  return {
    productId: "11111111-1111-7111-8111-111111111111",
    locale: "fa-IR",
    slug: "گوشی-سامسونگ",
    seoTitle: "عنوان",
    seoDescription: "توضیح",
    productName: "گوشی",
    titleFallback: "عنوان",
    publicPath: "/fa/products/گوشی-سامسونگ",
    readiness: {
      hasValidSlug: true,
      hasSeoTitleOrFallback: true,
      hasSeoDescription: true,
      hasLocalizedIdentity: true,
      isReady: true,
      messageFa: "اطلاعات سئو کامل است",
    },
    updatedAt: "2026-08-29T00:00:00Z",
    ...partial,
  };
}

test("mapSeoDetail and dirty/preview helpers", () => {
  const mapped = mapSeoDetail({
    productId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    locale: "en",
    slug: "linen-shirt",
    seoTitle: null,
    seoDescription: "desc",
    productName: "Shirt",
    titleFallback: "Shirt",
    publicPath: "/en/products/linen-shirt",
    readiness: {
      hasValidSlug: true,
      hasSeoTitleOrFallback: true,
      hasSeoDescription: true,
      hasLocalizedIdentity: true,
      isReady: true,
      messageFa: "اطلاعات سئو کامل است",
    },
    updatedAt: "2026-08-29T01:00:00Z",
  });
  assert.ok(mapped);
  assert.equal(mapped!.slug, "linen-shirt");
  assert.equal(mapped!.seoTitle, null);

  const detail = sampleDetail();
  const draft = draftFromSeoDetail(detail);
  assert.equal(isSeoDraftDirty(detail, draft), false);
  draft.seoTitle = "دیگر";
  assert.equal(isSeoDraftDirty(detail, draft), true);
  assert.equal(resolveSeoPreviewTitle(detail, { ...draft, seoTitle: "" }), "عنوان");
});

test("readiness labels are Persian", () => {
  assert.equal(
    formatSeoReadinessLabel({
      hasValidSlug: false,
      hasSeoTitleOrFallback: false,
      hasSeoDescription: false,
      hasLocalizedIdentity: false,
      isReady: false,
      messageFa: "آدرس محصول تکمیل نشده است",
    }),
    "آدرس محصول تکمیل نشده است",
  );
  assert.equal(
    formatSeoReadinessLabel({
      hasValidSlug: true,
      hasSeoTitleOrFallback: true,
      hasSeoDescription: true,
      hasLocalizedIdentity: true,
      isReady: true,
      messageFa: "اطلاعات سئو کامل است",
    }),
    "اطلاعات سئو کامل است",
  );
});

test("panel source has VIEW/EDIT labels Save/Cancel SERP and no AgGrid/Price/Stock", () => {
  const src = fs.readFileSync(path.join(root, "product-seo-panel.tsx"), "utf8");
  assert.match(src, /ProductSeoPanelMode/);
  assert.match(src, /آدرس محصول/);
  assert.match(src, /عنوان برای موتورهای جستجو/);
  assert.match(src, /توضیح نتیجه جستجو/);
  assert.match(src, /product-seo-serp-preview/);
  assert.match(src, /product-seo-save/);
  assert.match(src, /product-seo-cancel/);
  assert.match(src, /product-seo-locale-switcher/);
  assert.match(src, /editable/);
  assert.doesNotMatch(src, /AgGridReact/);
  assert.doesNotMatch(src, /\bPrice\b|\bStock\b/);
  assert.doesNotMatch(src, /\/product\//);
  assert.match(src, /toast\.success/);
  assert.match(src, /تغییرات محصول ذخیره شد/);
});

test("workspace wires ProductSeoPanel into SEO tab", () => {
  const screen = fs.readFileSync(path.join(root, "product-workspace-screen.tsx"), "utf8");
  assert.match(screen, /ProductSeoPanel/);
  assert.match(screen, /label:\s*"SEO"/);
  assert.match(screen, /admin-product-seo/);
  assert.doesNotMatch(screen, /product-seo-placeholder/);
  assert.doesNotMatch(screen, /ویرایش پیشرفته SEO در تسک بعدی/);
});

test("host-client exposes SEO GET/PUT/readiness without Pricing joins", () => {
  const host = fs.readFileSync(path.join(root, "host-client.ts"), "utf8");
  assert.match(host, /getAdminProductSeo/);
  assert.match(host, /updateAdminProductSeo/);
  assert.match(host, /getAdminProductSeoReadiness/);
  assert.match(host, /\/v1\/admin\/products\/\$\{productId\}\/seo/);
  assert.doesNotMatch(host, /\/pricing\//i);
  assert.doesNotMatch(host, /\/inventory\//i);
});
