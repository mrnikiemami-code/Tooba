/** ردیف گالری رسانهٔ محصول (انتساب؛ بدون باینری). */
export type ProductMediaItem = {
  mediaAssetId: string;
  primary: boolean;
  displayOrder: number;
  altText: string | null;
};

/** آمادگی گالری از Host. */
export type ProductMediaReadiness = {
  hasPrimaryImage: boolean;
  mediaCount: number;
  isReady: boolean;
  messageFa: string | null;
};

/** ترتیب نمایش: تصویر اصلی اول، سپس DisplayOrder. */
export function sortMediaItems(items: ProductMediaItem[]): ProductMediaItem[] {
  return [...items].sort((a, b) => {
    if (a.primary !== b.primary) return a.primary ? -1 : 1;
    return (a.displayOrder ?? 0) - (b.displayOrder ?? 0);
  });
}

/** جابه‌جایی یک ردیف در فهرست ordered ids. */
export function moveMediaAssetId(
  orderedIds: string[],
  mediaAssetId: string,
  direction: -1 | 1,
): string[] | null {
  const index = orderedIds.indexOf(mediaAssetId);
  const nextIndex = index + direction;
  if (index < 0 || nextIndex < 0 || nextIndex >= orderedIds.length) {
    return null;
  }
  const next = [...orderedIds];
  const tmp = next[index]!;
  next[index] = next[nextIndex]!;
  next[nextIndex] = tmp;
  return next;
}

/** برچسب تعداد رسانه برای UI. */
export function formatMediaCountLabel(count: number): string {
  return `${new Intl.NumberFormat("fa-IR").format(count)} رسانه`;
}

/** متن آمادگی انسانی. */
export function formatMediaReadinessLabel(readiness: ProductMediaReadiness | null): string {
  if (!readiness) return "—";
  if (readiness.isReady) return readiness.messageFa ?? "رسانه کامل است";
  if (!readiness.hasPrimaryImage || readiness.mediaCount === 0) {
    return readiness.messageFa ?? "تصویر اصلی تعیین نشده";
  }
  return readiness.messageFa ?? formatMediaCountLabel(readiness.mediaCount);
}

/** آیا پیش‌نویس alt نسبت به سرور کثیف است؟ */
export function isAltDraftDirty(items: ProductMediaItem[], drafts: Record<string, string>): boolean {
  for (const item of items) {
    const draft = drafts[item.mediaAssetId] ?? "";
    const baseline = item.altText ?? "";
    if (draft !== baseline) return true;
  }
  return false;
}

/** مقدار اولیهٔ پیش‌نویس alt از فهرست سرور. */
export function altDraftsFromItems(items: ProductMediaItem[]): Record<string, string> {
  const next: Record<string, string> = {};
  for (const item of items) {
    next[item.mediaAssetId] = item.altText ?? "";
  }
  return next;
}

/** نرمال‌سازی ردیف‌های Host به مدل پنل. */
export function normalizeMediaItems(
  items: Array<{
    mediaAssetId: string;
    primary: boolean;
    displayOrder?: number;
    altText?: string | null;
  }>,
): ProductMediaItem[] {
  return items.map((item) => ({
    mediaAssetId: item.mediaAssetId,
    primary: item.primary,
    displayOrder: item.displayOrder ?? 0,
    altText: item.altText ?? null,
  }));
}
