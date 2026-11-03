/**
 * Pure create-outcome authority for carrier shipment POST responses.
 * Generic 5xx / network / malformed bodies are unresolved — never definitive refusal or success.
 */

import { decodeShipment, type ShipmentView } from "./order-group-carrier-execution-decode.ts";

export type CarrierCreateOutcomeKind = "succeeded" | "refused" | "unknown";

/** Typed refusals that occur before provider side-effect / claim consumption. */
export const PRE_SIDE_EFFECT_REFUSAL_CODES: ReadonlySet<string> = new Set([
  "shipping.carrier.unconfigured",
  "shipping.carrier.shipment.unsupported",
  "shipping.carrier.shipment.request_invalid",
  "shipping.carrier.shipment.not_eligible",
  "shipping.carrier.shipment.intent_conflict",
  "shipping.carrier.shipment.not_found",
]);

export const AMBIGUOUS_CREATE_CODES: ReadonlySet<string> = new Set([
  "shipping.carrier.shipment.unknown",
]);

export type ClassifyCarrierCreateOutcomeInput = {
  /** True when fetch threw (network loss / abort after send). */
  networkError?: boolean;
  httpStatus?: number;
  errorCode?: string | null;
  payload?: unknown;
  expectedOccurrenceId: string;
  expectedShippingGroupId: string;
};

export type ClassifyCarrierCreateOutcomeResult = {
  outcome: CarrierCreateOutcomeKind;
  packageDispatchId: string | null;
  decoded: ShipmentView | null;
  refusalCode: string | null;
};

function readErrorCode(payload: unknown): string | null {
  if (!payload || typeof payload !== "object") return null;
  const root = payload as Record<string, unknown>;
  const code = root.errorCode ?? root.ErrorCode;
  return typeof code === "string" && code.trim().length > 0 ? code.trim() : null;
}

export function isPreSideEffectRefusalCode(code: string | null | undefined): boolean {
  return typeof code === "string" && PRE_SIDE_EFFECT_REFUSAL_CODES.has(code);
}

export function classifyCarrierShipmentCreateOutcome(
  input: ClassifyCarrierCreateOutcomeInput,
): ClassifyCarrierCreateOutcomeResult {
  const expectedOccurrence = input.expectedOccurrenceId.trim();
  const expectedGroup = input.expectedShippingGroupId.trim();

  if (input.networkError === true) {
    return { outcome: "unknown", packageDispatchId: null, decoded: null, refusalCode: null };
  }

  const status = typeof input.httpStatus === "number" ? input.httpStatus : 0;
  const code = input.errorCode !== undefined ? input.errorCode : readErrorCode(input.payload);

  if (!Number.isFinite(status) || status <= 0) {
    return { outcome: "unknown", packageDispatchId: null, decoded: null, refusalCode: null };
  }

  // Generic server failures may follow a committed side effect — not proof of refusal.
  if (status >= 500) {
    return { outcome: "unknown", packageDispatchId: null, decoded: null, refusalCode: code };
  }

  if (status < 200 || status >= 300) {
    if (code && AMBIGUOUS_CREATE_CODES.has(code)) {
      return { outcome: "unknown", packageDispatchId: null, decoded: null, refusalCode: code };
    }
    if (isPreSideEffectRefusalCode(code)) {
      return { outcome: "refused", packageDispatchId: null, decoded: null, refusalCode: code };
    }
    // Unknown / missing codes on non-OK → unresolved (conservative).
    return { outcome: "unknown", packageDispatchId: null, decoded: null, refusalCode: code };
  }

  const decoded = decodeShipment(input.payload);
  if (!decoded) {
    return { outcome: "unknown", packageDispatchId: null, decoded: null, refusalCode: null };
  }

  if (decoded.shippingGroupId !== expectedGroup) {
    return { outcome: "unknown", packageDispatchId: null, decoded, refusalCode: null };
  }

  const returnedOccurrence =
    typeof decoded.packageDispatchId === "string" ? decoded.packageDispatchId.trim() : "";
  if (!returnedOccurrence || returnedOccurrence !== expectedOccurrence) {
    return { outcome: "unknown", packageDispatchId: null, decoded, refusalCode: null };
  }

  return {
    outcome: "succeeded",
    packageDispatchId: returnedOccurrence,
    decoded,
    refusalCode: null,
  };
}

/**
 * Authoritative shipment list read. HTTP 200 with a non-array body, or a non-empty
 * array where every row fails decode, is NOT a verified empty list.
 */
export type DecodeShipmentListResult =
  | { ok: true; ships: readonly ShipmentView[] }
  | { ok: false; reason: "http" | "malformed" };

export function decodeAuthoritativeShipmentList(
  httpOk: boolean,
  payload: unknown,
): DecodeShipmentListResult {
  if (!httpOk) return { ok: false, reason: "http" };
  if (!Array.isArray(payload)) return { ok: false, reason: "malformed" };
  const ships = payload
    .map(decodeShipment)
    .filter((row): row is ShipmentView => row !== null);
  if (payload.length > 0 && ships.length === 0) {
    return { ok: false, reason: "malformed" };
  }
  return { ok: true, ships };
}

/**
 * Exact-occurrence reconciliation: only a matching row for this occurrence+group resolves uncertainty.
 * Absence of a row preserves the unresolved locator (not permission to mint a replacement).
 */
export function resolveExactOccurrenceFromList(input: {
  ships: readonly ShipmentView[];
  occurrenceId: string;
  shippingGroupId: string;
}): ShipmentView | null {
  const occurrenceId = input.occurrenceId.trim();
  const shippingGroupId = input.shippingGroupId.trim();
  if (!occurrenceId || !shippingGroupId) return null;
  return (
    input.ships.find(
      (row) =>
        row.shippingGroupId === shippingGroupId
        && typeof row.packageDispatchId === "string"
        && row.packageDispatchId.trim() === occurrenceId,
    ) ?? null
  );
}
