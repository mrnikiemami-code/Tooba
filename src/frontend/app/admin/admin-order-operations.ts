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
  orderLineId?: string | null;
  consolidatedPackageId?: string | null;
  requiredPermission: string;
  requiresConfirm: boolean;
  confirmMessageFa: string | null;
};

export type AdminOrderLineCapability = {
  orderLineId: string;
  sellerOrderId: string;
  selectable: boolean;
  selectableQuantityMax: number;
  rowActionCodes: string[];
  bulkActionCodes: string[];
  shipmentEligibleQuantity: number;
  lockedReasonCode: string | null;
  lockedReasonFa: string | null;
};

export type AdminSellerCapability = {
  sellerOrderId: string;
  selectionAllowed: boolean;
  paymentLocked: boolean;
  infoMessageFa: string | null;
  wholeGroupActionCodes: string[];
  shipmentCreationPossible: boolean;
};

export type AdminOrderOperationsPage = {
  checkoutId: string;
  actions: AdminOrderOperationAction[];
  returnEligibility: unknown[];
  lineCapabilities: AdminOrderLineCapability[];
  sellerCapabilities: AdminSellerCapability[];
  inventoryRecoveryWarningFa?: string | null;
  inventoryRecoveryClass?: string | null;
  supplyStatus?: string | null;
  supplyMessageFa?: string | null;
  canConfirmDeposit?: boolean;
  canRecoverInventory?: boolean;
  supplyLines?: Array<{
    itemTitle: string | null;
    unitCode: string | null;
    required: number;
    available: number;
    shortage: number;
  }>;
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
  shippingMethodCode?: string | null;
  providerMetadataJson?: string | null;
  consolidatedPackageId?: string | null;
  shipmentIds?: string[] | null;
  note?: string | null;
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
    orderLineId: asString(row.orderLineId) ?? asString(row.OrderLineId),
    consolidatedPackageId: asString(row.consolidatedPackageId) ?? asString(row.ConsolidatedPackageId),
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
    lineCapabilities: mapLineCaps(row.lineCapabilities ?? row.LineCapabilities),
    sellerCapabilities: mapSellerCaps(row.sellerCapabilities ?? row.SellerCapabilities),
    inventoryRecoveryWarningFa:
      asString(row.inventoryRecoveryWarningFa) ?? asString(row.InventoryRecoveryWarningFa),
    inventoryRecoveryClass:
      asString(row.inventoryRecoveryClass) ?? asString(row.InventoryRecoveryClass),
    supplyStatus: asString(row.supplyStatus) ?? asString(row.SupplyStatus),
    supplyMessageFa: asString(row.supplyMessageFa) ?? asString(row.SupplyMessageFa),
    canConfirmDeposit: Boolean(row.canConfirmDeposit ?? row.CanConfirmDeposit),
    canRecoverInventory: Boolean(row.canRecoverInventory ?? row.CanRecoverInventory),
    supplyLines: Array.isArray(row.supplyLines ?? row.SupplyLines)
      ? ((row.supplyLines ?? row.SupplyLines) as unknown[]).flatMap((item) => {
          if (!item || typeof item !== "object") return [];
          const line = item as Record<string, unknown>;
          return [{
            itemTitle: asString(line.itemTitle) ?? asString(line.ItemTitle),
            unitCode: asString(line.unitCode) ?? asString(line.UnitCode),
            required: Number(line.required ?? line.Required ?? 0),
            available: Number(line.available ?? line.Available ?? 0),
            shortage: Number(line.shortage ?? line.Shortage ?? 0),
          }];
        })
      : [],
  };
}

function mapLineCaps(raw: unknown): AdminOrderLineCapability[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((item) => {
    if (!item || typeof item !== "object") return [];
    const row = item as Record<string, unknown>;
    const orderLineId = asString(row.orderLineId) ?? asString(row.OrderLineId);
    const sellerOrderId = asString(row.sellerOrderId) ?? asString(row.SellerOrderId);
    if (!orderLineId || !sellerOrderId) return [];
    return [{
      orderLineId,
      sellerOrderId,
      selectable: Boolean(row.selectable ?? row.Selectable),
      selectableQuantityMax: Number(row.selectableQuantityMax ?? row.SelectableQuantityMax ?? 0),
      rowActionCodes: stringList(row.rowActionCodes ?? row.RowActionCodes),
      bulkActionCodes: stringList(row.bulkActionCodes ?? row.BulkActionCodes),
      shipmentEligibleQuantity: Number(row.shipmentEligibleQuantity ?? row.ShipmentEligibleQuantity ?? 0),
      lockedReasonCode: asString(row.lockedReasonCode) ?? asString(row.LockedReasonCode),
      lockedReasonFa: asString(row.lockedReasonFa) ?? asString(row.LockedReasonFa),
    }];
  });
}

function mapSellerCaps(raw: unknown): AdminSellerCapability[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((item) => {
    if (!item || typeof item !== "object") return [];
    const row = item as Record<string, unknown>;
    const sellerOrderId = asString(row.sellerOrderId) ?? asString(row.SellerOrderId);
    if (!sellerOrderId) return [];
    return [{
      sellerOrderId,
      selectionAllowed: Boolean(row.selectionAllowed ?? row.SelectionAllowed),
      paymentLocked: Boolean(row.paymentLocked ?? row.PaymentLocked),
      infoMessageFa: asString(row.infoMessageFa) ?? asString(row.InfoMessageFa),
      wholeGroupActionCodes: stringList(row.wholeGroupActionCodes ?? row.WholeGroupActionCodes),
      shipmentCreationPossible: Boolean(row.shipmentCreationPossible ?? row.ShipmentCreationPossible),
    }];
  });
}

function stringList(raw: unknown): string[] {
  return Array.isArray(raw) ? raw.filter((x): x is string => typeof x === "string") : [];
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
        shippingMethodCode: body.shippingMethodCode ?? null,
        providerMetadataJson: body.providerMetadataJson ?? null,
        consolidatedPackageId: body.consolidatedPackageId ?? null,
        shipmentIds: body.shipmentIds ?? null,
        note: body.note ?? null,
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
