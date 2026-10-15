/**
 * TB-SHIPPING-FULL-OPERATIONS-078 — admin client for the existing authorized, read-only Shipping
 * operational quote seam.
 *
 * The 076 backend already owns exactly one route here:
 * `POST /v1/admin/shipping-services/{serviceCode}/operational-quote`. This module only speaks it.
 * The route service code is authoritative; the body never re-declares ownership. Nothing here
 * creates, edits or deletes a rate, coverage, packaging policy or schedule, and no price is ever
 * computed on the client: the backend answer is mapped field by field.
 *
 * Two honesty rules are enforced by the mapping itself:
 *
 * 1. **No fake zero.** A nullable monetary part that the backend omitted stays `null` in this
 *    model. It is never coerced to `0`, because `0` is a real quoted amount.
 * 2. **No invented wire field.** Only fields the backend contract actually declares are read.
 */
import { adminHeaders, type AdminResult } from "../admin-api.ts";
import type { ShippingDestinationScopeWrite } from "./shipping-rates-coverage-api.ts";

/** Pricing outcome contract of the Shipping module, mirrored as a stable string union. */
export type ShippingOperationalQuoteOutcome =
  | "Quoted"
  | "FreightNotQuoted"
  | "PackagingAmbiguous"
  | "CurrencyMismatch";

/** Carrier-window availability contract; `NotEvaluated` is never read as «available». */
export type ShippingScheduleAvailability =
  | "NotConfigured"
  | "InactiveNotEnforced"
  | "Available"
  | "Unavailable"
  | "NotEvaluated"
  | "Ambiguous";

/** Freight payer mode: a quotation fact, never a Payment/COD transaction. */
export type ShippingFreightPayerMode = "Prepaid" | "CollectOnDelivery";

/** Packaging charge payer; in this slice the carrier is never the payer. */
export type ShippingPackagingChargePayer = "Checkout";

/** Capacity enforcement; deliberately a single value in this slice. */
export type ShippingCapacityEnforcement = "NotEnforced";

/** Wire body of the evaluation; the exact DTO shape of the 076 endpoint. */
export type ShippingOperationalQuoteEvaluationRequest = {
  scope: ShippingDestinationScopeWrite;
  cartSubtotalExclusive: number;
  currencyCode?: string | null;
  optionCode?: string | null;
  weightGrams?: number | null;
  itemCount?: number | null;
  requestedPickupAtUtc?: string | null;
};

/** The carrier-freight component, carried through unchanged from the existing rate engine. */
export type ShippingCarrierFreightComponent = {
  outcome: string;
  serviceCode: string;
  chargeAmount: number;
  currencyCode: string | null;
  preparationDays: number;
  leadDays: number;
  reasonCode: string;
  optionCode: string | null;
  isQuoted: boolean;
};

/** The independent packaging component. */
export type ShippingPackagingComponent = {
  isCharged: boolean;
  feeAmount: number;
  currencyCode: string | null;
  freightPayerMode: ShippingFreightPayerMode;
  packagingPayer: ShippingPackagingChargePayer;
  packagingPolicyId: string | null;
  reasonCode: string;
};

/** The carrier-window evaluation, with explicit capacity honesty. */
export type ShippingScheduleEvaluation = {
  availability: ShippingScheduleAvailability;
  reasonCode: string;
  timeZoneId: string | null;
  localWeekday: number | null;
  localTime: string | null;
  matchedWindowId: string | null;
  configuredCapacityTotal: number | null;
  capacityEnforcement: ShippingCapacityEnforcement;
  isConstraintSatisfied: boolean;
};

/** One matched numeric tier of the winning rate; descriptive only. */
export type ShippingNumericTier = {
  min: number;
  max: number | null;
};

export type ShippingConditionMatch = {
  weight: ShippingNumericTier | null;
  count: ShippingNumericTier | null;
};

/**
 * The full, auditable answer. Every monetary field is nullable on purpose: `null` means «not
 * declared» and must never be rendered as `0`.
 */
export type ShippingOperationalQuoteResult = {
  outcome: ShippingOperationalQuoteOutcome;
  serviceCode: string;
  optionCode: string | null;
  currencyCode: string | null;
  freightAmount: number | null;
  packagingAmount: number | null;
  totalServiceExpense: number | null;
  amountPayableAtCheckout: number | null;
  amountCollectOnDelivery: number | null;
  freightPayerMode: ShippingFreightPayerMode;
  packagingPayer: ShippingPackagingChargePayer;
  carrierFreight: ShippingCarrierFreightComponent | null;
  packaging: ShippingPackagingComponent | null;
  schedule: ShippingScheduleEvaluation | null;
  reasonCode: string;
  matchedCondition: ShippingConditionMatch | null;
  isQuoted: boolean;
  isOperationallyEligible: boolean;
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
  const raw = text(value).trim();
  return raw ? raw : null;
}

/**
 * Reads a nullable decimal. An omitted or unreadable value stays `null` — never `0` — because a
 * quoted zero and a not-declared amount are different facts.
 */
function nullableNumber(value: unknown): number | null {
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function requiredNumber(value: unknown): number | null {
  return nullableNumber(value);
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
 * `errors` map (`shipping.validation.operational_weight_negative` and friends). Surfacing only the
 * envelope would hide the actionable code, so the first non-envelope child code wins.
 */
export function operationalQuoteErrorCode(payload: unknown): string {
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

function mapOutcome(value: unknown): ShippingOperationalQuoteOutcome {
  const raw = text(value).trim();
  if (raw === "FreightNotQuoted" || raw === "PackagingAmbiguous" || raw === "CurrencyMismatch") {
    return raw;
  }
  return "Quoted";
}

function mapAvailability(value: unknown): ShippingScheduleAvailability {
  const raw = text(value).trim();
  if (
    raw === "NotConfigured" ||
    raw === "InactiveNotEnforced" ||
    raw === "Available" ||
    raw === "Unavailable" ||
    raw === "NotEvaluated" ||
    raw === "Ambiguous"
  ) {
    return raw;
  }
  // An unrecognized availability is reported as «not evaluated»: it is never upgraded to
  // «available», which would be an unearned promise.
  return "NotEvaluated";
}

/**
 * Payer mode is a stable code contract. Anything outside it is reported as `Prepaid` **only**
 * because that is the persisted server default, and the raw backend reason code stays visible.
 */
function mapPayerMode(value: unknown): ShippingFreightPayerMode {
  return text(value).trim() === "CollectOnDelivery" ? "CollectOnDelivery" : "Prepaid";
}

function mapTier(value: unknown): ShippingNumericTier | null {
  const row = asRecord(value);
  if (!row) return null;
  const min = nullableNumber(pick(row, "min", "Min"));
  if (min == null) return null;
  return { min, max: nullableNumber(pick(row, "max", "Max")) };
}

function mapConditionMatch(value: unknown): ShippingConditionMatch | null {
  const row = asRecord(value);
  if (!row) return null;
  const weight = mapTier(pick(row, "weight", "Weight"));
  const count = mapTier(pick(row, "count", "Count"));
  return weight || count ? { weight, count } : null;
}

function mapCarrierFreight(value: unknown): ShippingCarrierFreightComponent | null {
  const row = asRecord(value);
  if (!row) return null;
  const chargeAmount = requiredNumber(pick(row, "chargeAmount", "ChargeAmount"));
  if (chargeAmount == null) return null;
  return {
    outcome: text(pick(row, "outcome", "Outcome")),
    serviceCode: text(pick(row, "serviceCode", "ServiceCode")),
    chargeAmount,
    currencyCode: optionalText(pick(row, "currencyCode", "CurrencyCode")),
    preparationDays: requiredNumber(pick(row, "preparationDays", "PreparationDays")) ?? 0,
    leadDays: requiredNumber(pick(row, "leadDays", "LeadDays")) ?? 0,
    reasonCode: text(pick(row, "reasonCode", "ReasonCode")),
    optionCode: optionalText(pick(row, "optionCode", "OptionCode")),
    isQuoted: Boolean(pick(row, "isQuoted", "IsQuoted")),
  };
}

function mapPackaging(value: unknown): ShippingPackagingComponent | null {
  const row = asRecord(value);
  if (!row) return null;
  const feeAmount = requiredNumber(pick(row, "feeAmount", "FeeAmount"));
  if (feeAmount == null) return null;
  return {
    isCharged: Boolean(pick(row, "isCharged", "IsCharged")),
    feeAmount,
    currencyCode: optionalText(pick(row, "currencyCode", "CurrencyCode")),
    freightPayerMode: mapPayerMode(pick(row, "freightPayerMode", "FreightPayerMode")),
    packagingPayer: "Checkout",
    packagingPolicyId: optionalText(pick(row, "packagingPolicyId", "PackagingPolicyId")),
    reasonCode: text(pick(row, "reasonCode", "ReasonCode")),
  };
}

function mapSchedule(value: unknown): ShippingScheduleEvaluation | null {
  const row = asRecord(value);
  if (!row) return null;
  return {
    availability: mapAvailability(pick(row, "availability", "Availability")),
    reasonCode: text(pick(row, "reasonCode", "ReasonCode")),
    timeZoneId: optionalText(pick(row, "timeZoneId", "TimeZoneId")),
    localWeekday: nullableNumber(pick(row, "localWeekday", "LocalWeekday")),
    localTime: optionalText(pick(row, "localTime", "LocalTime")),
    matchedWindowId: optionalText(pick(row, "matchedWindowId", "MatchedWindowId")),
    configuredCapacityTotal: nullableNumber(pick(row, "configuredCapacityTotal", "ConfiguredCapacityTotal")),
    capacityEnforcement: "NotEnforced",
    isConstraintSatisfied: Boolean(pick(row, "isConstraintSatisfied", "IsConstraintSatisfied")),
  };
}

/** Maps the raw backend answer into the typed model; returns `null` only for a non-object body. */
export function mapOperationalQuote(raw: unknown): ShippingOperationalQuoteResult | null {
  const source = asRecord(unwrap(raw));
  if (!source) return null;
  const serviceCode = text(pick(source, "serviceCode", "ServiceCode"));
  const reasonCode = text(pick(source, "reasonCode", "ReasonCode"));
  if (!serviceCode || !reasonCode) return null;

  return {
    outcome: mapOutcome(pick(source, "outcome", "Outcome")),
    serviceCode,
    optionCode: optionalText(pick(source, "optionCode", "OptionCode")),
    currencyCode: optionalText(pick(source, "currencyCode", "CurrencyCode")),
    freightAmount: nullableNumber(pick(source, "freightAmount", "FreightAmount")),
    packagingAmount: nullableNumber(pick(source, "packagingAmount", "PackagingAmount")),
    totalServiceExpense: nullableNumber(pick(source, "totalServiceExpense", "TotalServiceExpense")),
    amountPayableAtCheckout: nullableNumber(pick(source, "amountPayableAtCheckout", "AmountPayableAtCheckout")),
    amountCollectOnDelivery: nullableNumber(pick(source, "amountCollectOnDelivery", "AmountCollectOnDelivery")),
    freightPayerMode: mapPayerMode(pick(source, "freightPayerMode", "FreightPayerMode")),
    packagingPayer: "Checkout",
    carrierFreight: mapCarrierFreight(pick(source, "carrierFreight", "CarrierFreight")),
    packaging: mapPackaging(pick(source, "packaging", "Packaging")),
    schedule: mapSchedule(pick(source, "schedule", "Schedule")),
    reasonCode,
    matchedCondition: mapConditionMatch(pick(source, "matchedCondition", "MatchedCondition")),
    isQuoted: Boolean(pick(source, "isQuoted", "IsQuoted")),
    isOperationallyEligible: Boolean(pick(source, "isOperationallyEligible", "IsOperationallyEligible")),
  };
}

function base(serviceCode: string): string {
  return `/v1/admin/shipping-services/${encodeURIComponent(serviceCode)}/operational-quote`;
}

/**
 * Evaluates one diagnostic operational quote through the existing authorized read-only route.
 *
 * The request body carries only the fields the 076 DTO declares. Absent optional inputs are sent as
 * `null` so the backend sees an explicit «not declared» instead of a missing key it must guess.
 */
export async function evaluateAdminShippingOperationalQuote(
  serviceCode: string,
  body: ShippingOperationalQuoteEvaluationRequest,
): Promise<AdminResult<ShippingOperationalQuoteResult>> {
  const payload = {
    scope: body.scope,
    cartSubtotalExclusive: body.cartSubtotalExclusive,
    currencyCode: body.currencyCode ?? null,
    optionCode: body.optionCode ?? null,
    weightGrams: body.weightGrams ?? null,
    itemCount: body.itemCount ?? null,
    requestedPickupAtUtc: body.requestedPickupAtUtc ?? null,
  };
  try {
    const response = await fetch(base(serviceCode), {
      method: "POST",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify(payload),
    });
    const raw = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: operationalQuoteErrorCode(raw) || "shipping_operational_quote.evaluate_failed",
      };
    }
    const mapped = mapOperationalQuote(raw);
    if (!mapped) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: "shipping_operational_quote.invalid_response",
      };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
