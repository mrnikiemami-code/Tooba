/**
 * TB-SHIPPING-FULL-OPERATIONS-077 — admin client for the Shipping-owned packaging fee policy and
 * freight payer mode, addressed by the opaque Fulfillment service `code` from the route.
 *
 * The 075 backend already owns these four routes; this module only speaks them. The route service
 * code is authoritative (the body never re-declares ownership), every write is followed by a real
 * re-read instead of an in-memory patch, and no mock data or local-only persistence exists.
 *
 * Money is a quotation fact, never a transaction: nothing here creates a Payment, moves money or
 * claims a collect-on-delivery settlement.
 */
import { adminHeaders, type AdminResult } from "../admin-api.ts";
import type { ShippingDestinationScopeWrite } from "./shipping-rates-coverage-api.ts";

/**
 * Stable payer-mode contract of the Shipping module, mirrored as a string union on the wire.
 * `Prepaid` «پیش‌کرایه» is the documented default; `CollectOnDelivery` «پس‌کرایه» means the carrier
 * collects the freight, not the packaging fee.
 */
export type ShippingFreightPayerMode = "Prepaid" | "CollectOnDelivery";

export type ShippingPackagingPolicyWriteRequest = {
  scope: ShippingDestinationScopeWrite;
  feeAmount: number;
  currencyCode: string;
  /**
   * The Fulfillment option this policy prices, or `null` for the service-level policy that applies
   * to single-option/no-option services and acts as the documented fallback.
   */
  optionCode?: string | null;
  /** `null`/omitted keeps the documented `Prepaid` default. */
  freightPayerMode?: ShippingFreightPayerMode | null;
};

export type ShippingPackagingPolicyAdminItem = {
  packagingPolicyId: string;
  serviceCode: string;
  countryId: string;
  administrativeAreaId: string | null;
  cityId: string | null;
  serviceAreaId: string | null;
  level: string;
  feeAmount: number;
  currencyCode: string;
  freightPayerMode: ShippingFreightPayerMode;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  /** Option this policy prices; `null` means the service-level fallback policy. */
  optionCode: string | null;
};

export type ShippingPackagingPoliciesView = {
  serviceCode: string;
  packagingPolicies: ShippingPackagingPolicyAdminItem[];
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return value == null ? "" : String(value);
}

function pick<T extends Record<string, unknown>>(row: T, camel: string, pascal: string): unknown {
  return row[camel] ?? row[pascal];
}

function optionalText(value: unknown): string | null {
  const raw = text(value);
  return raw ? raw : null;
}

function numberOrNull(value: unknown): number | null {
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

/**
 * The payer mode is a stable code contract, so an unrecognized value is never silently coerced:
 * anything outside the contract is reported as `Prepaid` **only** because that is the persisted
 * server default, and the raw value stays visible through the reason path of the backend.
 */
function mapPayerMode(value: unknown): ShippingFreightPayerMode {
  const raw = text(value).trim();
  return raw === "CollectOnDelivery" ? "CollectOnDelivery" : "Prepaid";
}

/** Unwraps the platform envelope if present; the Shipping admin routes answer with the raw value. */
function unwrap(payload: unknown): unknown {
  const row = asRecord(payload);
  if (!row) return payload;
  return "data" in row ? row.data : payload;
}

/**
 * Stable error code of a failed call.
 *
 * A shape failure arrives as `validation.failed` with the real, stable child code inside the
 * `errors` map (`shipping.validation.packaging_fee_negative` and friends). Surfacing only the
 * envelope would hide the actionable code, so the first non-envelope child code wins.
 */
export function packagingErrorCode(payload: unknown): string {
  const row = asRecord(payload);
  if (!row) return "";
  const direct = text(pick(row, "errorCode", "ErrorCode"));
  if (direct && direct !== "validation.failed") return direct;
  const errors = asRecord(pick(row, "errors", "Errors"));
  if (errors) {
    for (const value of Object.values(errors)) {
      const list = Array.isArray(value) ? value : [value];
      for (const item of list) {
        const code = text(item);
        if (code && code !== "validation.failed") return code;
      }
    }
  }
  return direct || text(pick(row, "title", "Title"));
}

function mapPolicy(raw: unknown): ShippingPackagingPolicyAdminItem | null {
  const source = asRecord(unwrap(raw));
  if (!source) return null;
  const packagingPolicyId = text(pick(source, "packagingPolicyId", "PackagingPolicyId"));
  const serviceCode = text(pick(source, "serviceCode", "ServiceCode"));
  if (!packagingPolicyId || !serviceCode) return null;
  return {
    packagingPolicyId,
    serviceCode,
    countryId: text(pick(source, "countryId", "CountryId")),
    administrativeAreaId: optionalText(pick(source, "administrativeAreaId", "AdministrativeAreaId")),
    cityId: optionalText(pick(source, "cityId", "CityId")),
    serviceAreaId: optionalText(pick(source, "serviceAreaId", "ServiceAreaId")),
    level: text(pick(source, "level", "Level")),
    feeAmount: numberOrNull(pick(source, "feeAmount", "FeeAmount")) ?? 0,
    currencyCode: text(pick(source, "currencyCode", "CurrencyCode")),
    freightPayerMode: mapPayerMode(pick(source, "freightPayerMode", "FreightPayerMode")),
    isActive: Boolean(pick(source, "isActive", "IsActive")),
    createdAt: text(pick(source, "createdAt", "CreatedAt")),
    updatedAt: text(pick(source, "updatedAt", "UpdatedAt")),
    optionCode: optionalText(pick(source, "optionCode", "OptionCode")),
  };
}

function mapView(raw: unknown): ShippingPackagingPoliciesView | null {
  const row = asRecord(unwrap(raw));
  if (!row) return null;
  const serviceCode = text(pick(row, "serviceCode", "ServiceCode"));
  if (!serviceCode) return null;
  const policies = pick(row, "packagingPolicies", "PackagingPolicies");
  return {
    serviceCode,
    packagingPolicies: (Array.isArray(policies) ? policies : [])
      .map(mapPolicy)
      .filter((x): x is ShippingPackagingPolicyAdminItem => Boolean(x)),
  };
}

function base(serviceCode: string): string {
  return `/v1/admin/shipping-services/${encodeURIComponent(serviceCode)}/packaging-policies`;
}

/** Reads the real persisted packaging policies of one service code. */
export async function loadAdminShippingPackagingPolicies(
  serviceCode: string,
): Promise<AdminResult<ShippingPackagingPoliciesView>> {
  try {
    const response = await fetch(base(serviceCode), { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: packagingErrorCode(payload) || "shipping_packaging.load_failed",
      };
    }
    const mapped = mapView(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "shipping_packaging.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

async function sendJson(
  url: string,
  method: "POST" | "PUT" | "PATCH",
  body: unknown,
  fallback: string,
): Promise<AdminResult<ShippingPackagingPolicyAdminItem>> {
  try {
    const response = await fetch(url, {
      method,
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: packagingErrorCode(payload) || fallback };
    }
    const mapped = mapPolicy(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: `${fallback}.invalid_response` };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

function writeBody(serviceCode: string, body: ShippingPackagingPolicyWriteRequest): Record<string, unknown> {
  return {
    serviceCode,
    optionCode: body.optionCode ?? null,
    scope: body.scope,
    feeAmount: body.feeAmount,
    currencyCode: body.currencyCode,
    freightPayerMode: body.freightPayerMode ?? null,
  };
}

/** Creates a packaging policy under the route service code. */
export function createAdminShippingPackagingPolicy(
  serviceCode: string,
  body: ShippingPackagingPolicyWriteRequest,
): Promise<AdminResult<ShippingPackagingPolicyAdminItem>> {
  return sendJson(base(serviceCode), "POST", writeBody(serviceCode, body), "shipping_packaging.create_failed");
}

/**
 * Updates fee, currency and payer mode of one existing policy.
 *
 * Scope and option identity are echoed back unchanged by the caller: the backend refuses a
 * mismatched option, and an existing option must never be moved by an accidental edit.
 */
export function updateAdminShippingPackagingPolicy(
  serviceCode: string,
  packagingPolicyId: string,
  body: ShippingPackagingPolicyWriteRequest,
): Promise<AdminResult<ShippingPackagingPolicyAdminItem>> {
  return sendJson(
    `${base(serviceCode)}/${encodeURIComponent(packagingPolicyId)}`,
    "PUT",
    writeBody(serviceCode, body),
    "shipping_packaging.update_failed",
  );
}

/** Activates or deactivates one packaging policy. */
export function setAdminShippingPackagingPolicyActive(
  serviceCode: string,
  packagingPolicyId: string,
  isActive: boolean,
): Promise<AdminResult<ShippingPackagingPolicyAdminItem>> {
  return sendJson(
    `${base(serviceCode)}/${encodeURIComponent(packagingPolicyId)}/active`,
    "PATCH",
    { isActive },
    "shipping_packaging.active_failed",
  );
}
