import type { GridServerQuery } from "../data-grid/types.ts";
import { shouldCommitGridQuery } from "./filter-commit.ts";

/** draft جستجو را به query اعمال‌شده commit می‌کند — یک درخواست در صورت تغییر. */
export function commitSearchQuery(
  current: GridServerQuery,
  searchDraft: string,
): GridServerQuery | null {
  const nextSearch = searchDraft.trim() || undefined;
  const next: GridServerQuery = { ...current, page: 1, search: nextSearch };
  if (!shouldCommitGridQuery(current, next)) return null;
  return next;
}
