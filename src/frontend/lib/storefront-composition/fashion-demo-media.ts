/**
 * Fashion Template Catalog media — local static assets only (no third-party hotlinks).
 * Maps deterministic Template Catalog media asset IDs and legacy demo ids.
 */

const FASHION_IMAGES = [
  "/images/fashion-template/1.jpg",
  "/images/fashion-template/2.jpg",
  "/images/fashion-template/3.jpg",
  "/images/fashion-template/4.jpg",
  "/images/fashion-template/5.jpg",
  "/images/fashion-template/6.jpg",
  "/images/fashion-template/7.jpg",
  "/images/fashion-template/8.jpg",
] as const;

/** Deterministic Template Catalog media GUIDs from FashionTemplateCatalogIds.MediaAsset(n). */
const TEMPLATE_MEDIA_GUIDS = [
  "019022a5-0000-7000-8000-00000000a001",
  "019022a5-0000-7000-8000-00000000a002",
  "019022a5-0000-7000-8000-00000000a003",
  "019022a5-0000-7000-8000-00000000a004",
  "019022a5-0000-7000-8000-00000000a005",
  "019022a5-0000-7000-8000-00000000a006",
  "019022a5-0000-7000-8000-00000000a007",
  "019022a5-0000-7000-8000-00000000a008",
] as const;

export function fashionDemoMediaUrl(assetId: string | null | undefined): string | null {
  if (!assetId) return null;
  const guidIndex = TEMPLATE_MEDIA_GUIDS.findIndex((g) => g === assetId.toLowerCase());
  if (guidIndex >= 0) return FASHION_IMAGES[guidIndex]!;
  if (assetId.startsWith("demo-fashion-media-")) {
    const n = Number(assetId.replace("demo-fashion-media-", "")) || 1;
    return FASHION_IMAGES[(n - 1) % FASHION_IMAGES.length]!;
  }
  if (assetId.startsWith("/images/fashion-template/")) return assetId;
  return null;
}

export { FASHION_IMAGES, TEMPLATE_MEDIA_GUIDS };
