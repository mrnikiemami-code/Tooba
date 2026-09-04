/**
 * مدل و قوانین Tooba برای AppCategoryTree — بدون نشت نوع Ant Design.
 */

export type CategoryNodeStatus = "Draft" | "Published" | "Archived";

export interface AppCategoryTreeNode {
  id: string;
  parentId: string | null;
  name: string;
  slug: string;
  status: CategoryNodeStatus;
  sortOrder: number;
  isVisible: boolean;
  hasChildren: boolean;
  productCount: number | null;
  children?: AppCategoryTreeNode[];
}

export type CategoryDropPosition = "before" | "inside" | "after";

export interface CategoryDropRequest {
  dragId: string;
  dropId: string;
  position: CategoryDropPosition;
}

export type TranslationReadiness = "complete" | "partial" | "missing";

export interface LocaleTranslationStatus {
  locale: string;
  label: string;
  readiness: TranslationReadiness;
}

/** حداکثر عمق درخت دسته Catalog (ریشه = ۱)؛ افزودن زیرمجموعه زیر سطح ۳ ممنوع است. */
export const MAX_CATEGORY_DEPTH = 3;

/** حداکثر عمق درخت دسته‌بندی مقاله Content (ریشه=۱، زیردسته=۲). */
export const MAX_CONTENT_CATEGORY_DEPTH = 2;

export const MAX_CATEGORY_DEPTH_MESSAGE_FA =
  "عمیق‌تر از سطح سوم برای دسته‌بندی مجاز نیست.";

export const MAX_CONTENT_CATEGORY_DEPTH_MESSAGE_FA =
  "دسته‌بندی مقاله حداکثر می‌تواند دو سطح داشته باشد.";
export const MAX_CONTENT_CATEGORY_DEPTH_MESSAGE_EN =
  "Article categories can have at most two levels.";

const LOCALE_LABELS: Record<string, string> = {
  "fa-IR": "فارسی",
  "en-US": "English",
  "ar-SA": "العربية",
};

/** برچسب انسانی وضعیت انتشار. */
export function categoryStatusLabel(status: CategoryNodeStatus, locale: "fa" | "en" = "fa"): string {
  if (locale === "en") {
    if (status === "Draft") return "Draft";
    if (status === "Published") return "Published";
    return "Archived";
  }
  if (status === "Draft") return "پیش‌نویس";
  if (status === "Published") return "منتشر شده";
  return "بایگانی";
}

/** برچسب آمادگی ترجمه. */
export function translationReadinessLabel(
  readiness: TranslationReadiness,
  uiLocale: "fa" | "en" = "fa",
): string {
  if (uiLocale === "en") {
    if (readiness === "complete") return "Complete";
    if (readiness === "partial") return "Incomplete";
    return "Not created";
  }
  if (readiness === "complete") return "کامل";
  if (readiness === "partial") return "ناقص";
  return "ایجاد نشده";
}

/**
 * وضعیت ترجمه بر اساس حضور name+slug.
 * کامل = هر دو پر؛ ناقص = یکی؛ ایجاد نشده = هیچ.
 */
export function resolveTranslationReadiness(
  name: string | null | undefined,
  slug: string | null | undefined,
): TranslationReadiness {
  const hasName = Boolean(name && name.trim());
  const hasSlug = Boolean(slug && slug.trim());
  if (hasName && hasSlug) return "complete";
  if (hasName || hasSlug) return "partial";
  return "missing";
}

/** وضعیت ترجمه‌ها برای localeهای استاندارد Admin. */
export function buildTranslationStatuses(
  translations: ReadonlyArray<{ locale: string; name: string; slug: string }>,
  locales: readonly string[] = ["fa-IR", "en-US", "ar-SA"],
): LocaleTranslationStatus[] {
  const byLocale = new Map(translations.map((t) => [t.locale, t]));
  return locales.map((locale) => {
    const row = byLocale.get(locale);
    return {
      locale,
      label: LOCALE_LABELS[locale] ?? locale,
      readiness: row
        ? resolveTranslationReadiness(row.name, row.slug)
        : ("missing" as TranslationReadiness),
    };
  });
}

/** فهرست تخت → درخت تو در تو با ترتیب SortOrder. */
export function buildCategoryForest(flat: readonly AppCategoryTreeNode[]): AppCategoryTreeNode[] {
  const map = new Map<string, AppCategoryTreeNode>();
  for (const node of flat) {
    map.set(node.id, { ...node, children: [] });
  }
  const roots: AppCategoryTreeNode[] = [];
  for (const node of map.values()) {
    if (node.parentId && map.has(node.parentId)) {
      map.get(node.parentId)!.children!.push(node);
    } else {
      roots.push(node);
    }
  }
  const sortRecursive = (nodes: AppCategoryTreeNode[]) => {
    nodes.sort((a, b) => a.sortOrder - b.sortOrder || a.id.localeCompare(b.id));
    for (const n of nodes) {
      if (n.children?.length) sortRecursive(n.children);
      else if (n.children) delete n.children;
    }
  };
  sortRecursive(roots);
  return roots;
}

/** نقشهٔ parent برای هر id. */
export function buildParentMap(flat: readonly AppCategoryTreeNode[]): Map<string, string | null> {
  return new Map(flat.map((n) => [n.id, n.parentId]));
}

/** سطح رده = ۱ + تعداد اجداد؛ ریشه بدون والد = ۱. */
export function getCategoryTreeLevel(
  flat: readonly AppCategoryTreeNode[],
  categoryId: string,
): number | null {
  const parentMap = buildParentMap(flat);
  if (!parentMap.has(categoryId)) return null;
  let ancestors = 0;
  let current: string | null = categoryId;
  const seen = new Set<string>();
  while (current && parentMap.has(current) && !seen.has(current)) {
    seen.add(current);
    const parentId: string | null = parentMap.get(current) ?? null;
    if (!parentId) break;
    ancestors += 1;
    current = parentId;
  }
  return 1 + ancestors;
}

/** آیا می‌توان زیر این رده فرزند ساخت؟ (سطح والد < maxDepth) */
export function canAddCategoryChild(
  flat: readonly AppCategoryTreeNode[],
  parentId: string | null | undefined,
  maxDepth: number = MAX_CATEGORY_DEPTH,
): boolean {
  if (!parentId) return true;
  const level = getCategoryTreeLevel(flat, parentId);
  return level != null && level < maxDepth;
}

/** آیا candidate نسل drag است (یا خودش). */
export function isSelfOrDescendant(
  parentMap: Map<string, string | null>,
  dragId: string,
  candidateId: string,
): boolean {
  if (dragId === candidateId) return true;
  let current: string | null | undefined = candidateId;
  const seen = new Set<string>();
  while (current) {
    if (seen.has(current)) break;
    seen.add(current);
    if (current === dragId) return true;
    current = parentMap.get(current) ?? null;
  }
  return false;
}

/** اجداد یک گره (از نزدیک به دور، بدون خود گره). */
export function collectAncestorIds(
  parentMap: Map<string, string | null>,
  id: string,
): string[] {
  const ancestors: string[] = [];
  let current = parentMap.get(id) ?? null;
  const seen = new Set<string>();
  while (current) {
    if (seen.has(current)) break;
    seen.add(current);
    ancestors.push(current);
    current = parentMap.get(current) ?? null;
  }
  return ancestors;
}

/** مسیر نام‌ها از ریشه تا گره. */
export function buildCategoryPath(
  flat: readonly AppCategoryTreeNode[],
  id: string,
): string[] {
  const byId = new Map(flat.map((n) => [n.id, n]));
  const parentMap = buildParentMap(flat);
  const chain = [...collectAncestorIds(parentMap, id)].reverse();
  chain.push(id);
  return chain.map((cid) => byId.get(cid)?.name || "—").filter(Boolean);
}

/** تعداد فرزندان مستقیم. */
export function countDirectChildren(flat: readonly AppCategoryTreeNode[], parentId: string): number {
  return flat.filter((n) => n.parentId === parentId).length;
}

/** شناسهٔ همهٔ گره‌هایی که فرزند دارند — برای expand-all. */
export function collectExpandableParentIds(flat: readonly AppCategoryTreeNode[]): string[] {
  const parents = new Set<string>();
  for (const node of flat) {
    if (node.parentId) parents.add(node.parentId);
    if (node.hasChildren) parents.add(node.id);
  }
  return [...parents];
}

/** خواهر/برادرهای یک والد به ترتیب. */
export function listSiblingIds(
  flat: readonly AppCategoryTreeNode[],
  parentId: string | null,
): string[] {
  return flat
    .filter((n) => n.parentId === parentId)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.id.localeCompare(b.id))
    .map((n) => n.id);
}

export interface CategorySearchResult {
  filteredForest: AppCategoryTreeNode[];
  matchedIds: Set<string>;
  autoExpandKeys: string[];
}

/**
 * فیلتر محلی روی نام؛ اجداد برای انسجام درخت نگه داشته می‌شوند.
 */
export function filterCategoryForest(
  flat: readonly AppCategoryTreeNode[],
  query: string,
): CategorySearchResult {
  const needle = query.trim().toLowerCase();
  if (!needle) {
    return {
      filteredForest: buildCategoryForest(flat),
      matchedIds: new Set(),
      autoExpandKeys: [],
    };
  }

  const parentMap = buildParentMap(flat);
  const matchedIds = new Set<string>();
  for (const node of flat) {
    if (node.name.toLowerCase().includes(needle)) {
      matchedIds.add(node.id);
    }
  }

  const keep = new Set<string>(matchedIds);
  for (const id of matchedIds) {
    for (const ancestor of collectAncestorIds(parentMap, id)) {
      keep.add(ancestor);
    }
  }

  const filteredFlat = flat.filter((n) => keep.has(n.id));
  const autoExpandKeys = [...new Set(
    [...matchedIds].flatMap((id) => collectAncestorIds(parentMap, id)),
  )];

  return {
    filteredForest: buildCategoryForest(filteredFlat),
    matchedIds,
    autoExpandKeys,
  };
}

/** آیا drop معتبر است (خود/نسل ممنوع؛ داخل سطح maxDepth ممنوع). */
export function isValidCategoryDrop(
  flat: readonly AppCategoryTreeNode[],
  request: CategoryDropRequest,
  maxDepth: number = MAX_CATEGORY_DEPTH,
): boolean {
  if (!request.dragId || !request.dropId) return false;
  if (request.dragId === request.dropId) return false;
  const parentMap = buildParentMap(flat);
  if (request.position === "inside" && isSelfOrDescendant(parentMap, request.dragId, request.dropId)) {
    return false;
  }
  // before/after روی نسل: والد جدید = والد drop؛ اگر drop نسل drag باشد باطل است
  if (request.position !== "inside" && isSelfOrDescendant(parentMap, request.dragId, request.dropId)) {
    return false;
  }
  if (request.position === "inside" && !canAddCategoryChild(flat, request.dropId, maxDepth)) {
    return false;
  }
  return true;
}

export interface ResolvedCategoryDrop {
  newParentId: string | null;
  orderedSiblingIds: string[];
  needsMove: boolean;
  needsReorder: boolean;
}

/**
 * تبدیل رویداد drop به move/reorder برای API.
 */
export function resolveCategoryDropPlan(
  flat: readonly AppCategoryTreeNode[],
  request: CategoryDropRequest,
  maxDepth: number = MAX_CATEGORY_DEPTH,
): ResolvedCategoryDrop | null {
  if (!isValidCategoryDrop(flat, request, maxDepth)) return null;

  const byId = new Map(flat.map((n) => [n.id, n]));
  const drag = byId.get(request.dragId);
  const drop = byId.get(request.dropId);
  if (!drag || !drop) return null;

  let newParentId: string | null;
  let insertIndex: number;

  if (request.position === "inside") {
    newParentId = drop.id;
    const siblings = listSiblingIds(flat, newParentId).filter((id) => id !== drag.id);
    insertIndex = siblings.length;
    const ordered = [...siblings, drag.id];
    return {
      newParentId,
      orderedSiblingIds: ordered,
      needsMove: drag.parentId !== newParentId,
      needsReorder: true,
    };
  }

  newParentId = drop.parentId;
  const siblings = listSiblingIds(flat, newParentId).filter((id) => id !== drag.id);
  const dropIndex = siblings.indexOf(drop.id);
  insertIndex = request.position === "before" ? dropIndex : dropIndex + 1;
  if (insertIndex < 0) insertIndex = 0;
  const ordered = [...siblings];
  ordered.splice(insertIndex, 0, drag.id);

  return {
    newParentId,
    orderedSiblingIds: ordered,
    needsMove: drag.parentId !== newParentId,
    needsReorder: true,
  };
}

/** برجسته‌سازی تطابق جستجو در نام. */
export function splitHighlight(name: string, query: string): { text: string; match: boolean }[] {
  const needle = query.trim();
  if (!needle) return [{ text: name, match: false }];
  const lower = name.toLowerCase();
  const idx = lower.indexOf(needle.toLowerCase());
  if (idx < 0) return [{ text: name, match: false }];
  return [
    { text: name.slice(0, idx), match: false },
    { text: name.slice(idx, idx + needle.length), match: true },
    { text: name.slice(idx + needle.length), match: false },
  ].filter((p) => p.text.length > 0);
}
