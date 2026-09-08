/**
 * Scope helpers for Admin order operations menus (pure — no fetch).
 */

export type AdminOrderOperationsScope = "whole-order" | "detail" | "fulfillment-queue";

export type AdminOrderOperationActionLike = {
  code: string;
  fulfillmentId?: string | null;
  sellerOrderId?: string | null;
};

/** عملیات‌هایی که فقط در اقلام و ارسال / جزئیات معنا دارند — از Grid کل‌سفارش حذف می‌شوند. */
export const GRID_EXCLUDED_OPERATION_CODES = new Set([
  "mark_processing",
  "mark_packed",
  "pack_selected",
  "unpack",
  "create_shipment",
  "cancel_shipment",
  "assign_tracking",
  "correct_tracking",
  "dispatch_shipment",
  "deliver_shipment",
  "request_return",
  "approve_return",
  "reject_return",
  "retry_refund",
]);

/** کدهای مجاز در kebab صف کار ارسال و تحویل. */
export const FULFILLMENT_QUEUE_OPERATION_CODES = new Set([
  "mark_processing",
  "mark_packed",
  "pack_selected",
  "unpack",
  "create_shipment",
  "cancel_shipment",
  "assign_tracking",
  "correct_tracking",
  "dispatch_shipment",
  "deliver_shipment",
]);

const WHOLE_ORDER_DEDUPE_CODES = new Set([
  "cancel",
  "confirm_deposit",
  "reject_deposit",
  "restore_deposit",
  "restore_cancelled_order",
]);

/** فیلتر scope منوی عملیات؛ whole-order فقط اقدامات امن کل سفارش. */
export function filterOperationsForScope<T extends AdminOrderOperationActionLike>(
  actions: T[],
  scope: AdminOrderOperationsScope = "detail",
  fulfillmentId?: string | null,
): T[] {
  if (scope === "fulfillment-queue") {
    return actions.filter((action) => {
      if (!FULFILLMENT_QUEUE_OPERATION_CODES.has(action.code)) return false;
      if (fulfillmentId && action.fulfillmentId && action.fulfillmentId !== fulfillmentId) return false;
      return true;
    });
  }
  if (scope !== "whole-order" && scope !== "detail") return actions;
  const filtered = actions.filter((action) => !GRID_EXCLUDED_OPERATION_CODES.has(action.code));
  return dedupeWholeOrderActions(filtered);
}

/** هر کد کل‌سفارش یک‌بار؛ ترجیح با sellerOrderId خالی. */
export function dedupeWholeOrderActions<T extends AdminOrderOperationActionLike>(actions: T[]): T[] {
  const preferred = new Map<string, T>();
  for (const action of actions) {
    if (!WHOLE_ORDER_DEDUPE_CODES.has(action.code)) continue;
    const existing = preferred.get(action.code);
    if (!existing || action.sellerOrderId == null) {
      preferred.set(action.code, action);
    }
  }
  const seen = new Set<string>();
  const result: T[] = [];
  for (const action of actions) {
    if (!WHOLE_ORDER_DEDUPE_CODES.has(action.code)) {
      result.push(action);
      continue;
    }
    if (seen.has(action.code)) continue;
    seen.add(action.code);
    result.push(preferred.get(action.code) ?? action);
  }
  return result;
}

/** برچسب‌های اقدام سریع فروشنده: کل گروه در برابر انتخاب‌شده‌ها. */
export function sellerQuickActionLabels(hasSelection: boolean): {
  startProcessing: string;
  pack: string;
  createShipment: string;
  unpack: string;
  dispatch: string;
} {
  if (hasSelection) {
    return {
      startProcessing: "شروع پردازش انتخاب‌شده‌ها",
      pack: "بسته‌بندی انتخاب‌شده‌ها",
      createShipment: "ایجاد مرسوله از انتخاب‌شده‌ها",
      unpack: "بازگشت از بسته‌بندی انتخاب‌شده‌ها",
      dispatch: "ارسال انتخاب‌شده‌ها",
    };
  }
  return {
    startProcessing: "شروع پردازش",
    pack: "بسته‌بندی همه اقلام آماده",
    createShipment: "ایجاد مرسوله جدید",
    unpack: "بازگشت از بسته‌بندی",
    dispatch: "ارسال",
  };
}
