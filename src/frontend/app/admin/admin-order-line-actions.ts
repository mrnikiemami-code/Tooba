import type { AdminOrderOperationAction } from "./admin-order-operations";

export type LineActionLike = Pick<AdminOrderOperationAction, "code" | "orderLineId">;

const ROW_CODES = new Set(["pack_selected", "unpack"]);

/** اقدام‌های ردیف برای یک line — فقط projection بک‌اند با orderLineId. */
export function rowActionsForLine<T extends LineActionLike>(actions: T[], orderLineId: string): T[] {
  return actions.filter((action) => ROW_CODES.has(action.code) && action.orderLineId === orderLineId);
}

/** اگر projection خطی نبود، از اقدام seller-level + تعداد قابل عملیات استفاده می‌شود. */
export function lineLifecycleActions<T extends LineActionLike>(
  actions: T[],
  orderLineId: string,
  opts: { packable: boolean; unpackable: boolean },
): T[] {
  const row = rowActionsForLine(actions, orderLineId);
  if (row.length > 0) return row;
  const result: T[] = [];
  if (opts.packable) {
    const pack = actions.find((action) => action.code === "pack_selected" && !action.orderLineId);
    if (pack) result.push(pack);
  }
  if (opts.unpackable) {
    const unpack = actions.find((action) => action.code === "unpack" && !action.orderLineId);
    if (unpack) result.push(unpack);
  }
  return result;
}

export function intersectLifecycleCodes(perLine: Set<string>[]): Set<string> {
  if (perLine.length === 0) return new Set();
  if (perLine.some((set) => set.size === 0)) return new Set();
  return new Set([...perLine[0]!].filter((code) => perLine.every((set) => set.has(code))));
}

/** هشدار ناسازگاری فقط وقتی بیش از یک ردیف و اشتراک خالی است. انتخاب تکی هرگز mixed نیست. */
export function isIncompatibleSelection(selectedCount: number, shared: Set<string>, perLine: Set<string>[]): boolean {
  if (selectedCount <= 1) return false;
  if (shared.size > 0) return false;
  return perLine.some((set) => set.size > 0);
}

/** اشتراک کدهای lifecycle انتخاب‌ها. خالی = ناسازگار یا بدون اقدام. */
export function compatibleBulkCodes<T extends LineActionLike>(
  actions: T[],
  selectedLineIds: string[],
): Set<string> {
  if (selectedLineIds.length === 0) return new Set();
  const perLine = selectedLineIds.map((id) => new Set(rowActionsForLine(actions, id).map((a) => a.code)));
  return intersectLifecycleCodes(perLine);
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

/** یک نمایش مهلت/باقیمانده؛ اگر هر دو یکسان باشند تکرار نمی‌شود. */
export function canonicalReturnDisplay(input: {
  returnStatusCode?: string | null;
  isReturnable?: boolean | null;
  returnDeadlineDisplay?: string | null;
  returnRemainingDisplay?: string | null;
  returnPolicyLabel?: string | null;
}): string {
  if (input.returnStatusCode === "non_returnable" || input.isReturnable === false) {
    return input.returnDeadlineDisplay || "غیرقابل مرجوعی";
  }
  if (input.returnStatusCode === "expired") {
    return "مهلت مرجوعی تمام شده";
  }
  const deadline = input.returnDeadlineDisplay?.trim() || "";
  const remaining = input.returnRemainingDisplay?.trim() || "";
  if (deadline && remaining && deadline !== remaining) {
    return input.returnStatusCode === "eligible" || input.returnStatusCode === "partial_eligible"
      ? remaining
      : deadline;
  }
  return deadline || remaining || input.returnPolicyLabel || "—";
}
