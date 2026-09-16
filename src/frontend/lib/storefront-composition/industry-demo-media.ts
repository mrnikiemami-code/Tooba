/**
 * Batch A Template Catalog media — local static assets only (no Fashion reuse / no hotlinks).
 */

export const BATCH_A_TEMPLATE_KEYS = [
  "auto-parts",
  "building-materials",
  "tools-hardware",
] as const;

export type BatchATemplateKey = (typeof BATCH_A_TEMPLATE_KEYS)[number];

const MEDIA_FOLDERS: Record<BatchATemplateKey, string> = {
  "auto-parts": "template-auto-parts",
  "building-materials": "template-building-materials",
  "tools-hardware": "template-tools-hardware",
};

/** Deterministic Template Catalog media GUIDs per pack (IndustryBatchATemplateCatalogIds). */
const MEDIA_GUID_PREFIX: Record<BatchATemplateKey, string> = {
  "auto-parts": "019022b1-0000-7000-8000-00000000a",
  "building-materials": "019022b3-0000-7000-8000-00000000a",
  "tools-hardware": "019022b5-0000-7000-8000-00000000a",
};

export function isBatchATemplateKey(key: string): key is BatchATemplateKey {
  return (BATCH_A_TEMPLATE_KEYS as readonly string[]).includes(key);
}

export function industryTemplateImages(templateKey: BatchATemplateKey): readonly string[] {
  const folder = MEDIA_FOLDERS[templateKey];
  return Array.from({ length: 8 }, (_, i) => `/images/${folder}/${i + 1}.jpg`);
}

export function industryDemoMediaUrl(
  templateKey: BatchATemplateKey,
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

export function industryDemoOrigin(templateKey: BatchATemplateKey): string {
  return `${templateKey}-template-catalog-persisted`;
}

export const INDUSTRY_STORE_ORIGIN = "operational-store-catalog" as const;
