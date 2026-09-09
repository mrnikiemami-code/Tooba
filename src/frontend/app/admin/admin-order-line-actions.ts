import type { AdminOrderOperationAction } from "./admin-order-operations";

export type LineActionLike = Pick<AdminOrderOperationAction, "code" | "orderLineId">;

const ROW_CODES = new Set(["mark_processing", "pack_selected", "unprocess", "unpack"]);

/** اقدام‌های ردیف برای یک line — فقط projection بک‌اند با orderLineId. */
export function rowActionsForLine<T extends LineActionLike>(actions: T[], orderLineId: string): T[] {
  return actions.filter((action) => ROW_CODES.has(action.code) && action.orderLineId === orderLineId);
}

/** اگر projection خطی نبود، از اقدام seller-level + قابلیت خط استفاده می‌شود. */
export function lineLifecycleActions<T extends LineActionLike>(
  actions: T[],
  orderLineId: string,
  opts: { packable: boolean; unpackable: boolean; startable?: boolean; unprocessable?: boolean },
): T[] {
  const row = rowActionsForLine(actions, orderLineId);
  if (row.length > 0) return row;
  const result: T[] = [];
  if (opts.startable) {
    const start = actions.find((action) => action.code === "mark_processing" && !action.orderLineId)
      ?? actions.find((action) => action.code === "mark_processing");
    if (start) result.push(start);
  }
  if (opts.packable) {
    const pack = actions.find((action) => action.code === "pack_selected" && !action.orderLineId);
    if (pack) result.push(pack);
  }
  if (opts.unprocessable) {
    const unprocess = actions.find((action) => action.code === "unprocess" && !action.orderLineId);
    if (unprocess) result.push(unprocess);
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

export const MIXED_SELECTION_MESSAGE_FA = "برای عملیات گروهی، اقلام هم‌مرحله را انتخاب کنید.";
export const PAYMENT_LOCKED_BANNER_FA =
  "پرداخت این بخش از سفارش هنوز تأیید نشده است؛ پس از تأیید پرداخت، عملیات پردازش و ارسال فعال می‌شود.";

export type LineCapability = {
  selectable: boolean;
  selectableQuantityMax: number;
  rowActionCodes: string[];
  bulkActionCodes: string[];
  shipmentEligibleQuantity: number;
  paymentLocked: boolean;
};

export function isPaymentLockedSeller(input: {
  status?: string | null;
  paymentState?: string | null;
  fulfillmentId?: string | null;
  fulfillmentStatus?: string | null;
}): boolean {
  const status = input.status ?? "";
  const payment = input.paymentState ?? "";
  if (status === "Cancelled" || payment === "Cancelled") return false;
  if (status === "PendingPayment" || payment === "PendingPayment") return true;
  if (status === "Paid" || payment === "Paid") return false;
  return !input.fulfillmentId && !input.fulfillmentStatus;
}

export function deriveLineCapability(input: {
  paymentLocked: boolean;
  cancelled?: boolean;
  operationalStatus?: string | null;
  packable: number;
  unpackable: number;
  unprocessable?: number;
  shippable: number;
  quantity: number;
  projectedCodes: string[];
}): LineCapability {
  if (input.cancelled || input.paymentLocked) {
    return {
      selectable: false,
      selectableQuantityMax: 0,
      rowActionCodes: [],
      bulkActionCodes: [],
      shipmentEligibleQuantity: 0,
      paymentLocked: input.paymentLocked,
    };
  }
  const codes = new Set(input.projectedCodes);
  const row: string[] = [];
  const bulk: string[] = [];
  const ready = input.operationalStatus === "ReadyToFulfill" || input.operationalStatus === "ReadyToProcess";
  const unprocessable = input.unprocessable ?? 0;
  if (ready && codes.has("mark_processing")) {
    row.push("mark_processing");
    bulk.push("mark_processing");
  }
  if (input.packable > 0 && codes.has("pack_selected")) {
    row.push("pack_selected");
    bulk.push("pack_selected");
  }
  if (unprocessable > 0 && codes.has("unprocess")) {
    row.push("unprocess");
    bulk.push("unprocess");
  }
  if (input.unpackable > 0 && codes.has("unpack")) {
    row.push("unpack");
    bulk.push("unpack");
  }
  if (input.shippable > 0 && codes.has("create_shipment")) {
    bulk.push("create_shipment");
  }
  const selectable = row.length > 0 || bulk.length > 0;
  let max = 0;
  if (row.includes("pack_selected") || row.includes("unprocess")) max = Math.max(max, input.packable, unprocessable);
  if (row.includes("unpack")) max = Math.max(max, input.unpackable);
  if (bulk.includes("create_shipment")) max = Math.max(max, input.shippable);
  if (row.includes("mark_processing")) max = Math.max(max, input.quantity);
  return {
    selectable,
    selectableQuantityMax: selectable ? Math.max(1, max) : 0,
    rowActionCodes: row,
    bulkActionCodes: bulk,
    shipmentEligibleQuantity: input.shippable,
    paymentLocked: false,
  };
}

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
