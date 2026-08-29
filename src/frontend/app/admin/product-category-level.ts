/**
 * قواعد سطح دسته برای اختصاص محصول: Level = 1 + تعداد اجداد؛ فقط سطح ۳ قابل انتخاب است.
 */

export type CategoryLevelNode = {
  id: string;
  parentId: string | null;
};

export const PRODUCT_ASSIGNABLE_CATEGORY_LEVEL = 3;

export const PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA =
  "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.";

/** سطح رده = ۱ + تعداد اجداد از طریق parentId. */
export function getCategoryLevel(
  nodes: readonly CategoryLevelNode[],
  categoryId: string,
): number | null {
  const parentById = new Map<string, string | null>();
  for (const n of nodes) {
    parentById.set(n.id, n.parentId);
  }
  if (!parentById.has(categoryId)) return null;
  let ancestors = 0;
  let current: string | null = categoryId;
  const seen = new Set<string>();
  while (current && parentById.has(current) && !seen.has(current)) {
    seen.add(current);
    const parentId: string | null = parentById.get(current) ?? null;
    if (!parentId) break;
    ancestors += 1;
    current = parentId;
  }
  return 1 + ancestors;
}

export function isAssignableProductCategory(
  nodes: readonly CategoryLevelNode[],
  categoryId: string | null | undefined,
): boolean {
  if (!categoryId) return false;
  return getCategoryLevel(nodes, categoryId) === PRODUCT_ASSIGNABLE_CATEGORY_LEVEL;
}

/** فرزندان مستقیم به ترتیب sortOrder در صورت وجود. */
export function listCategoryChildren<T extends CategoryLevelNode & { sortOrder?: number }>(
  nodes: readonly T[],
  parentId: string | null,
): T[] {
  return nodes
    .filter((n) => n.parentId === parentId)
    .slice()
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.id.localeCompare(b.id));
}
