/**
 * کلاینت عملیات lifecycle سفارش Admin — فقط API-driven actions.
 */
import { adminHeaders, type AdminResult } from "./admin-api";
import {
  mapAdminErrorMessage,
  type AdminErrorLocale,
} from "./admin-error-map";
import {
  filterOperationsForScope,
  GRID_EXCLUDED_OPERATION_CODES,
  sellerQuickActionLabels,
  type AdminOrderOperationsScope,
} from "./admin-order-operations-scope";

export type { AdminOrderOperationsScope };
export { filterOperationsForScope, GRID_EXCLUDED_OPERATION_CODES, sellerQuickActionLabels };

export type AdminOrderOperationAction = {
  code: string;
  labelFa: string;
  labelEn: string;
  sellerOrderId: string | null;
  fulfillmentId: string | null;
  shipmentId: string | null;
  returnRequestId: string | null;
  requiredPermission: string;
  requiresConfirm: boolean;
  confirmMessageFa: string | null;
};

export type AdminOrderOperationsPage = {
  checkoutId: string;
  actions: AdminOrderOperationAction[];
  returnEligibility: unknown[];
};

export type AdminOrderOperationRequest = {
  code: string;
  sellerOrderId?: string | null;
  fulfillmentId?: string | null;
  shipmentId?: string | null;
  returnRequestId?: string | null;
  carrierDisplayName?: string | null;
  trackingReference?: string | null;
  reason?: string | null;
  idempotencyKey?: string | null;
  selections?: Array<{ orderLineId: string; quantity: number }> | null;
};

/** نگاشت خطای عملیات سفارش به FA. */
export function mapOrderOperationError(
  codeOrMessage: string | null | undefined,
  detail?: string | null,
  locale: AdminErrorLocale = "fa",
): string {
  const code = codeOrMessage ?? null;
  if (code && (code === "order.operation.denied" || code === "order.operation.invalid" || code === "order.operation.failed")) {
    if (detail && detail.trim() && !/bad request|http\s*\d|exception|stack/i.test(detail)) {
      return detail.trim();
    }
    return mapAdminErrorMessage(code, locale);
  }
  if (detail && detail.trim() && !/bad request|http\s*\d|exception|stack/i.test(detail)) {
    return detail.trim();
  }
  return mapAdminErrorMessage(codeOrMessage, locale);
}

function asString(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function mapAction(raw: unknown): AdminOrderOperationAction | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const code = asString(row.code) ?? asString(row.Code);
  const labelFa = asString(row.labelFa) ?? asString(row.LabelFa);
  if (!code || !labelFa) return null;
  return {
    code,
    labelFa,
    labelEn: asString(row.labelEn) ?? asString(row.LabelEn) ?? code,
    sellerOrderId: asString(row.sellerOrderId) ?? asString(row.SellerOrderId),
    fulfillmentId: asString(row.fulfillmentId) ?? asString(row.FulfillmentId),
    shipmentId: asString(row.shipmentId) ?? asString(row.ShipmentId),
    returnRequestId: asString(row.returnRequestId) ?? asString(row.ReturnRequestId),
    requiredPermission: asString(row.requiredPermission) ?? asString(row.RequiredPermission) ?? "",
    requiresConfirm: Boolean(row.requiresConfirm ?? row.RequiresConfirm),
    confirmMessageFa: asString(row.confirmMessageFa) ?? asString(row.ConfirmMessageFa),
  };
}

function mapPage(raw: unknown): AdminOrderOperationsPage | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const checkoutId = asString(row.checkoutId) ?? asString(row.CheckoutId);
  const actionsRaw = row.actions ?? row.Actions;
  if (!checkoutId || !Array.isArray(actionsRaw)) return null;
  return {
    checkoutId,
    actions: actionsRaw.map(mapAction).filter((x): x is AdminOrderOperationAction => x !== null),
    returnEligibility: Array.isArray(row.returnEligibility ?? row.ReturnEligibility)
      ? ((row.returnEligibility ?? row.ReturnEligibility) as unknown[])
      : [],
  };
}

async function parseError(response: Response): Promise<{ code: string; detail: string | null }> {
  const payload = await response.json().catch(() => null);
  if (!payload || typeof payload !== "object") {
    return { code: `admin.http.${response.status}`, detail: null };
  }
  const row = payload as Record<string, unknown>;
  const code =
    asString(row.errorCode) ??
    asString(row.ErrorCode) ??
    `admin.http.${response.status}`;
  const detail = asString(row.detail) ?? asString(row.Detail) ?? asString(row.title) ?? asString(row.Title);
  return { code, detail };
}

/** عملیات مجاز را از Host می‌خواند. */
export async function loadAdminOrderOperations(
  checkoutId: string,
): Promise<AdminResult<AdminOrderOperationsPage>> {
  try {
    const response = await fetch(`/v1/admin/orders/${encodeURIComponent(checkoutId)}/operations`, {
      headers: adminHeaders(),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      const err = await parseError(response);
      return {
        state: "error",
        data: null,
        status: response.status,
        message: mapOrderOperationError(err.code, err.detail),
      };
    }
    const payload = await response.json().catch(() => null);
    const data = mapPage(payload);
    return data
      ? { state: "ok", data, status: response.status }
      : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
  } catch {
    return { state: "error", data: null, status: 0, message: mapOrderOperationError("host-unreachable") };
  }
}

/** یک عملیات را اجرا می‌کند. */
export async function executeAdminOrderOperation(
  checkoutId: string,
  body: AdminOrderOperationRequest,
): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(`/v1/admin/orders/${encodeURIComponent(checkoutId)}/operations`, {
      method: "POST",
      headers: adminHeaders({ "content-type": "application/json" }),
      body: JSON.stringify({
        code: body.code,
        sellerOrderId: body.sellerOrderId ?? null,
        fulfillmentId: body.fulfillmentId ?? null,
        shipmentId: body.shipmentId ?? null,
        returnRequestId: body.returnRequestId ?? null,
        carrierDisplayName: body.carrierDisplayName ?? null,
        trackingReference: body.trackingReference ?? null,
        reason: body.reason ?? null,
        idempotencyKey: body.idempotencyKey ?? null,
        selections: body.selections?.map((s) => ({
          orderLineId: s.orderLineId,
          quantity: s.quantity,
        })) ?? null,
      }),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "order.operation.denied" };
    }
    if (!response.ok) {
      const err = await parseError(response);
      return {
        state: "error",
        data: null,
        status: response.status,
        message: mapOrderOperationError(err.code, err.detail),
      };
    }
    const payload = await response.json().catch(() => null);
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: mapOrderOperationError("host-unreachable") };
  }
}
