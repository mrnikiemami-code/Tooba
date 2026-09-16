/**
 * Persisted industry Template Catalog media — local static assets only (no Fashion reuse / no hotlinks).
 */

export const BATCH_A_TEMPLATE_KEYS = [
  "auto-parts",
  "building-materials",
  "tools-hardware",
] as const;

export const BATCH_B_TEMPLATE_KEYS = [
  "tile-ceramic",
  "interior-decor",
  "home-appliances",
] as const;

export const BATCH_C_TEMPLATE_KEYS = [
  "shoes",
  "plants",
  "beauty",
] as const;

export const INDUSTRY_CATALOG_TEMPLATE_KEYS = [
  ...BATCH_A_TEMPLATE_KEYS,
  ...BATCH_B_TEMPLATE_KEYS,
  ...BATCH_C_TEMPLATE_KEYS,
] as const;

export type BatchATemplateKey = (typeof BATCH_A_TEMPLATE_KEYS)[number];
export type BatchBTemplateKey = (typeof BATCH_B_TEMPLATE_KEYS)[number];
export type BatchCTemplateKey = (typeof BATCH_C_TEMPLATE_KEYS)[number];
export type IndustryCatalogTemplateKey = (typeof INDUSTRY_CATALOG_TEMPLATE_KEYS)[number];

const MEDIA_FOLDERS: Record<IndustryCatalogTemplateKey, string> = {
  "auto-parts": "template-auto-parts",
  "building-materials": "template-building-materials",
  "tools-hardware": "template-tools-hardware",
  "tile-ceramic": "template-tile-ceramic",
  "interior-decor": "template-interior-decor",
  "home-appliances": "template-home-appliances",
  shoes: "template-shoes",
  plants: "template-plants",
  beauty: "template-beauty",
};

/** Deterministic Template Catalog media GUIDs per pack. */
const MEDIA_GUID_PREFIX: Record<IndustryCatalogTemplateKey, string> = {
  "auto-parts": "019022b1-0000-7000-8000-00000000a",
  "building-materials": "019022b3-0000-7000-8000-00000000a",
  "tools-hardware": "019022b5-0000-7000-8000-00000000a",
  "tile-ceramic": "019022b7-0000-7000-8000-00000000a",
  "interior-decor": "019022b9-0000-7000-8000-00000000a",
  "home-appliances": "019022bb-0000-7000-8000-00000000a",
  shoes: "019022bd-0000-7000-8000-00000000a",
  plants: "019022bf-0000-7000-8000-00000000a",
  beauty: "019022c1-0000-7000-8000-00000000a",
};

export function isBatchATemplateKey(key: string): key is BatchATemplateKey {
  return (BATCH_A_TEMPLATE_KEYS as readonly string[]).includes(key);
}

export function isBatchBTemplateKey(key: string): key is BatchBTemplateKey {
  return (BATCH_B_TEMPLATE_KEYS as readonly string[]).includes(key);
}

export function isBatchCTemplateKey(key: string): key is BatchCTemplateKey {
  return (BATCH_C_TEMPLATE_KEYS as readonly string[]).includes(key);
}

export function isIndustryCatalogTemplateKey(key: string): key is IndustryCatalogTemplateKey {
  return (INDUSTRY_CATALOG_TEMPLATE_KEYS as readonly string[]).includes(key);
}

export function industryTemplateImages(templateKey: IndustryCatalogTemplateKey): readonly string[] {
  const folder = MEDIA_FOLDERS[templateKey];
  return Array.from({ length: 8 }, (_, i) => `/images/${folder}/${i + 1}.jpg`);
}

export function industryDemoMediaUrl(
  templateKey: IndustryCatalogTemplateKey,
  assetId: string | null | undefined,
): string | null {
  if (!assetId) return null;
  const prefix = MEDIA_GUID_PREFIX[templateKey];
  const folder = MEDIA_FOLDERS[templateKey];
  const lower = assetId.toLowerCase();
  for (let i = 1; i <= 8; i++) {
    const guid = `${prefix}${String(i).padStart(3, "0")}`;
    if (lower === guid) return `/images/${folder}/${i}.jpg`;
  }
  if (assetId.startsWith(`/images/${folder}/`)) return assetId;
  return null;
}

export function industryDemoOrigin(templateKey: IndustryCatalogTemplateKey): string {
  return `${templateKey}-template-catalog-persisted`;
}

export const INDUSTRY_STORE_ORIGIN = "operational-store-catalog" as const;
