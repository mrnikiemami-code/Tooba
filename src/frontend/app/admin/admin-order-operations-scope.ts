/**
 * Scope helpers for Admin order operations menus (pure — no fetch).
 */

export type AdminOrderOperationsScope = "whole-order" | "detail" | "fulfillment-queue";

export type AdminOrderOperationActionLike = {
  code: string;
  fulfillmentId?: string | null;
};

/** عملیات‌هایی که فقط در اقلام و ارسال / جزئیات معنا دارند — از Grid کل‌سفارش حذف می‌شوند. */
export const GRID_EXCLUDED_OPERATION_CODES = new Set([
  "mark_processing",
  "mark_packed",
  "unpack",
  "create_shipment",
  "cancel_shipment",
  "assign_tracking",
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
  "unpack",
  "create_shipment",
  "cancel_shipment",
  "assign_tracking",
  "dispatch_shipment",
  "deliver_shipment",
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
  if (scope !== "whole-order") return actions;
  return actions.filter((action) => !GRID_EXCLUDED_OPERATION_CODES.has(action.code));
}

/** برچسب‌های اقدام سریع فروشنده: کل گروه در برابر انتخاب‌شده‌ها. */
export function sellerQuickActionLabels(hasSelection: boolean): {
  pack: string;
  createShipment: string;
  unpack: string;
  dispatch: string;
} {
  if (hasSelection) {
    return {
      pack: "بسته‌بندی انتخاب‌شده‌ها",
      createShipment: "ایجاد مرسوله از انتخاب‌شده‌ها",
      unpack: "بازگشت از بسته‌بندی انتخاب‌شده‌ها",
      dispatch: "ارسال انتخاب‌شده‌ها",
    };
  }
  return {
    pack: "بسته‌بندی همه اقلام آماده",
    createShipment: "ایجاد مرسوله",
    unpack: "بازگشت از بسته‌بندی",
    dispatch: "ارسال",
  };
}
