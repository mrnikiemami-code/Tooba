/**
 * TB-SHIPPING-MVP-ADMIN-LIVE-WIRING-055 — admin client for Shipping-owned rate policies and
 * coverage grants, addressed by the opaque Fulfillment service `code` from the route.
 *
 * The route service code is authoritative: the body never re-declares the owner, and the response
 * is always re-read from the server instead of being patched in memory.
 */
import { adminHeaders, type AdminResult } from "../admin-api.ts";
import type { ShippingRateConditionWrite } from "./shipping-rate-tier-model.ts";

export type ShippingDestinationScopeWrite = {
  countryId: string;
  administrativeAreaId?: string | null;
  cityId?: string | null;
  serviceAreaId?: string | null;
};

export type ShippingRatePolicyWriteRequest = {
  scope: ShippingDestinationScopeWrite;
  /**
   * TB-SHIPPING-MVP-FINAL-CLOSURE-070 — the Fulfillment option this rate prices, or `null` for the
   * service-level policy that applies to single-option/no-option services and acts as the fallback.
   * The value is never derived from the service code; it is chosen from the real service tree.
   */
  optionCode?: string | null;
  chargeAmount: number;
  currencyCode: string;
  preparationDays: number;
  leadDays: number;
  freeAboveThreshold?: number | null;
  /**
   * TB-SHIPPING-FULL-OPERATIONS-074 — the 073 weight/item-count condition of this rate, or `null`
   * for an unconditioned rule. The shape is the canonical wire contract and is never reshaped here.
   */
  condition?: ShippingRateConditionWrite | null;
};

export type ShippingCoverageGrantWriteRequest = {
  scope: ShippingDestinationScopeWrite;
};

export type ShippingRatePolicyAdminItem = {
  ratePolicyId: string;
  serviceCode: string;
  /** Option this policy prices; `null` means the service-level fallback policy. */
  optionCode: string | null;
  countryId: string;
  administrativeAreaId: string | null;
  cityId: string | null;
  serviceAreaId: string | null;
  level: string;
  chargeAmount: number;
  currencyCode: string;
  preparationDays: number;
  leadDays: number;
  freeAboveThreshold: number | null;
  isActive: boolean;
  /** TB-SHIPPING-FULL-OPERATIONS-074 — persisted 073 tier condition; `null` means unconditioned. */
  condition: ShippingRateConditionWrite | null;
};

export type ShippingCoverageGrantAdminItem = {
  coverageGrantId: string;
  serviceCode: string;
  countryId: string;
  administrativeAreaId: string | null;
  cityId: string | null;
  serviceAreaId: string | null;
  level: string;
  isActive: boolean;
};

export type ShippingRatesAndCoverageView = {
  serviceCode: string;
  ratePolicies: ShippingRatePolicyAdminItem[];
  coverageGrants: ShippingCoverageGrantAdminItem[];
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
 * Maps one persisted tier axis. A missing `min` means the axis is not conditioned, so the axis is
 * dropped rather than invented as `[0, ...)`.
 */
function mapTierAxis(value: unknown): { min: number; max: number | null } | null {
  const row = asRecord(value);
  if (!row) return null;
  const min = numberOrNull(pick(row, "min", "Min"));
  if (min == null) return null;
  return { min, max: numberOrNull(pick(row, "max", "Max")) };
}

/** Maps the persisted condition; an empty/absent condition stays `null` (unconditioned). */
function mapCondition(value: unknown): ShippingRateConditionWrite | null {
  const row = asRecord(value);
  if (!row) return null;
  const weightGrams = mapTierAxis(pick(row, "weightGrams", "WeightGrams"));
  const itemCount = mapTierAxis(pick(row, "itemCount", "ItemCount"));
  if (!weightGrams && !itemCount) return null;
  return { weightGrams, itemCount };
}

function mapRate(raw: unknown): ShippingRatePolicyAdminItem | null {
  const row = asRecord(raw);
  if (!row) return null;
  const ratePolicyId = text(pick(row, "ratePolicyId", "RatePolicyId"));
  const serviceCode = text(pick(row, "serviceCode", "ServiceCode"));
  const countryId = text(pick(row, "countryId", "CountryId"));
  if (!ratePolicyId || !serviceCode || !countryId) return null;
  return {
    ratePolicyId,
    serviceCode,
    optionCode: optionalText(pick(row, "optionCode", "OptionCode")),
    countryId,
    administrativeAreaId: optionalText(pick(row, "administrativeAreaId", "AdministrativeAreaId")),
    cityId: optionalText(pick(row, "cityId", "CityId")),
    serviceAreaId: optionalText(pick(row, "serviceAreaId", "ServiceAreaId")),
    level: text(pick(row, "level", "Level")),
    chargeAmount: numberOrNull(pick(row, "chargeAmount", "ChargeAmount")) ?? 0,
    currencyCode: text(pick(row, "currencyCode", "CurrencyCode")),
    preparationDays: numberOrNull(pick(row, "preparationDays", "PreparationDays")) ?? 0,
    leadDays: numberOrNull(pick(row, "leadDays", "LeadDays")) ?? 0,
    freeAboveThreshold: numberOrNull(pick(row, "freeAboveThreshold", "FreeAboveThreshold")),
    isActive: Boolean(pick(row, "isActive", "IsActive")),
    condition: mapCondition(pick(row, "condition", "Condition")),
  };
}

function mapGrant(raw: unknown): ShippingCoverageGrantAdminItem | null {
  const row = asRecord(raw);
  if (!row) return null;
  const coverageGrantId = text(pick(row, "coverageGrantId", "CoverageGrantId"));
  const serviceCode = text(pick(row, "serviceCode", "ServiceCode"));
  const countryId = text(pick(row, "countryId", "CountryId"));
  if (!coverageGrantId || !serviceCode || !countryId) return null;
  return {
    coverageGrantId,
    serviceCode,
    countryId,
    administrativeAreaId: optionalText(pick(row, "administrativeAreaId", "AdministrativeAreaId")),
    cityId: optionalText(pick(row, "cityId", "CityId")),
    serviceAreaId: optionalText(pick(row, "serviceAreaId", "ServiceAreaId")),
    level: text(pick(row, "level", "Level")),
    isActive: Boolean(pick(row, "isActive", "IsActive")),
  };
}

function mapView(raw: unknown): ShippingRatesAndCoverageView | null {
  const row = asRecord(raw);
  if (!row) return null;
  const serviceCode = text(pick(row, "serviceCode", "ServiceCode"));
  if (!serviceCode) return null;
  const rates = pick(row, "ratePolicies", "RatePolicies");
  const grants = pick(row, "coverageGrants", "CoverageGrants");
  return {
    serviceCode,
    ratePolicies: (Array.isArray(rates) ? rates : [])
      .map(mapRate)
      .filter((x): x is ShippingRatePolicyAdminItem => Boolean(x)),
    coverageGrants: (Array.isArray(grants) ? grants : [])
      .map(mapGrant)
      .filter((x): x is ShippingCoverageGrantAdminItem => Boolean(x)),
  };
}

function errorCode(payload: unknown, fallback: string): string {
  const row = asRecord(payload);
  return text(row?.errorCode ?? row?.ErrorCode ?? row?.title ?? row?.Title) || fallback;
}

function base(serviceCode: string): string {
  return `/v1/admin/shipping-services/${encodeURIComponent(serviceCode)}`;
}

async function sendJson<T>(
  url: string,
  method: "POST" | "PUT" | "PATCH",
  body: unknown,
  fallback: string,
  map: (raw: unknown) => T | null,
): Promise<AdminResult<T>> {
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
      return { state: "error", data: null, status: response.status, message: errorCode(payload, fallback) };
    }
    const mapped = map(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: `${fallback}.invalid_response` };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Reads the real rate policies and coverage grants of one service code. */
export async function loadAdminShippingRates(
  serviceCode: string,
): Promise<AdminResult<ShippingRatesAndCoverageView>> {
  try {
    const response = await fetch(`${base(serviceCode)}/rates`, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: errorCode(payload, "shipping_rates.load_failed"),
      };
    }
    const mapped = mapView(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "shipping_rates.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Creates a rate policy under the route service code. */
export function createAdminShippingRate(
  serviceCode: string,
  body: ShippingRatePolicyWriteRequest,
): Promise<AdminResult<ShippingRatePolicyAdminItem>> {
  return sendJson(
    `${base(serviceCode)}/rates`,
    "POST",
    { serviceCode, optionCode: body.optionCode ?? null, scope: body.scope, chargeAmount: body.chargeAmount, currencyCode: body.currencyCode, preparationDays: body.preparationDays, leadDays: body.leadDays, freeAboveThreshold: body.freeAboveThreshold ?? null, condition: body.condition ?? null },
    "shipping_rate.create_failed",
    mapRate,
  );
}

/** Updates the money/lead-time/condition fields of one existing rate policy. */
export function updateAdminShippingRate(
  serviceCode: string,
  ratePolicyId: string,
  body: ShippingRatePolicyWriteRequest,
): Promise<AdminResult<ShippingRatePolicyAdminItem>> {
  return sendJson(
    `${base(serviceCode)}/rates/${encodeURIComponent(ratePolicyId)}`,
    "PUT",
    { serviceCode, optionCode: body.optionCode ?? null, scope: body.scope, chargeAmount: body.chargeAmount, currencyCode: body.currencyCode, preparationDays: body.preparationDays, leadDays: body.leadDays, freeAboveThreshold: body.freeAboveThreshold ?? null, condition: body.condition ?? null },
    "shipping_rate.update_failed",
    mapRate,
  );
}

/** Activates or deactivates one rate policy. */
export function setAdminShippingRateActive(
  serviceCode: string,
  ratePolicyId: string,
  isActive: boolean,
): Promise<AdminResult<ShippingRatePolicyAdminItem>> {
  return sendJson(
    `${base(serviceCode)}/rates/${encodeURIComponent(ratePolicyId)}/active`,
    "PATCH",
    { isActive },
    "shipping_rate.active_failed",
    mapRate,
  );
}

/** Creates an explicit coverage grant under the route service code. */
export function createAdminShippingCoverageGrant(
  serviceCode: string,
  body: ShippingCoverageGrantWriteRequest,
): Promise<AdminResult<ShippingCoverageGrantAdminItem>> {
  return sendJson(
    `${base(serviceCode)}/coverage-grants`,
    "POST",
    { serviceCode, scope: body.scope },
    "shipping_coverage.create_failed",
    mapGrant,
  );
}

/** Activates or deactivates one coverage grant. */
export function setAdminShippingCoverageGrantActive(
  serviceCode: string,
  coverageGrantId: string,
  isActive: boolean,
): Promise<AdminResult<ShippingCoverageGrantAdminItem>> {
  return sendJson(
    `${base(serviceCode)}/coverage-grants/${encodeURIComponent(coverageGrantId)}/active`,
    "PATCH",
    { isActive },
    "shipping_coverage.active_failed",
    mapGrant,
  );
}
