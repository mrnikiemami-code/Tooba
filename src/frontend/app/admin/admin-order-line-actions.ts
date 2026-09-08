import type { AdminOrderOperationAction } from "./admin-order-operations";

export type LineActionLike = Pick<AdminOrderOperationAction, "code" | "orderLineId">;

const ROW_CODES = new Set(["pack_selected", "unpack"]);

/** اقدام‌های ردیف برای یک line — فقط projection بک‌اند با orderLineId. */
export function rowActionsForLine<T extends LineActionLike>(actions: T[], orderLineId: string): T[] {
  return actions.filter((action) => ROW_CODES.has(action.code) && action.orderLineId === orderLineId);
}

/** اشتراک کدهای lifecycle انتخاب‌ها. خالی = ناسازگار. */
export function compatibleBulkCodes<T extends LineActionLike>(
  actions: T[],
  selectedLineIds: string[],
): Set<string> {
  if (selectedLineIds.length === 0) return new Set();
  const perLine = selectedLineIds.map((id) => new Set(rowActionsForLine(actions, id).map((a) => a.code)));
  if (perLine.some((set) => set.size === 0)) return new Set();
  const shared = [...perLine[0]!].filter((code) => perLine.every((set) => set.has(code)));
  return new Set(shared);
}

export function compatibleBulkCode<T extends LineActionLike>(
  actions: T[],
  selectedLineIds: string[],
): "pack_selected" | "unpack" | null {
  const shared = compatibleBulkCodes(actions, selectedLineIds);
  if (shared.has("pack_selected") && !shared.has("unpack")) return "pack_selected";
  if (shared.has("unpack") && !shared.has("pack_selected")) return "unpack";
  return null;
}

export const MIXED_SELECTION_MESSAGE_FA = "ردیف‌های انتخاب‌شده در وضعیت‌های متفاوت یا ناسازگار هستند.";
