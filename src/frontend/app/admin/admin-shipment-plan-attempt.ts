/**
 * TB-270-R2 — the retained review envelope of ONE parcel's own dispatch plan.
 *
 * The reviewed occurrence key and the operator's chosen customer date/slot are persisted BEFORE the
 * first `plan_shipment_dispatch` POST, scoped to viewer + checkout + exact parcel. A lost response, a
 * 5xx/timeout, a typed refusal or a remount therefore reconciles the SAME intent instead of minting a
 * second review, while another viewer session can never inherit it.
 *
 * This is deliberately NOT authorization: the server re-checks the actor, the parcel's live `Created`
 * state, the source/hold binding and the freshly computed feasible calendar on every request, and an
 * expired selection is refused rather than clamped.
 *
 * The create occurrence and this schedule occurrence are separate identities: confirming a plan never
 * rewrites the already-recorded parcel-creation intent.
 */

/** Identity of ONE reviewed planning surface: viewer session, checkout and exact shipment. */
export type ShipmentPlanAttemptScope = {
  viewerScopeKey?: string | null;
  checkoutId: string;
  shipmentId: string;
};

/** Durable client record of one prebound plan confirmation. */
export type ShipmentPlanAttempt = {
  viewerScopeKey: string;
  checkoutId: string;
  shipmentId: string;
  /** The reviewed occurrence key, sent as `idempotencyKey`. */
  occurrenceKey: string;
  receivingDate: string | null;
  receivingOpensAt: string | null;
  receivingClosesAt: string | null;
  attempts: number;
  lastFailedAtUtc: string | null;
  lastMessageFa: string | null;
};

const VERSION = "v1";
const DEFAULT_SCOPE = "__viewer__";

/** Same-mount convenience only; it is NOT durability and never authorizes a new mutation by itself. */
const memoryFallback = new Map<string, ShipmentPlanAttempt>();

function coalesce(value: string | null | undefined): string {
  return typeof value === "string" ? value.trim() : "";
}

function normalizeScope(scope: ShipmentPlanAttemptScope): ShipmentPlanAttemptScope | null {
  const checkoutId = coalesce(scope.checkoutId);
  const shipmentId = coalesce(scope.shipmentId);
  if (!checkoutId || !shipmentId) return null;
  return {
    viewerScopeKey: coalesce(scope.viewerScopeKey) || DEFAULT_SCOPE,
    checkoutId,
    shipmentId,
  };
}

function storageKey(scope: ShipmentPlanAttemptScope): string {
  return [
    `tb.shipment-plan-attempt.${VERSION}`,
    scope.checkoutId,
    scope.viewerScopeKey ?? DEFAULT_SCOPE,
    scope.shipmentId,
  ].join(":");
}

/** Probe on EVERY call, never latch: a transient storage failure must not disable durability forever. */
function sessionStorageApi(): Storage | null {
  try {
    const api = typeof sessionStorage === "undefined" ? null : sessionStorage;
    if (!api || typeof api.getItem !== "function" || typeof api.setItem !== "function") return null;
    return api;
  } catch {
    return null;
  }
}

function mintId(): string {
  const cryptoApi = globalThis.crypto;
  if (cryptoApi && typeof cryptoApi.randomUUID === "function") return cryptoApi.randomUUID();
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (token) => {
    const random = Math.floor(Math.random() * 16);
    const value = token === "x" ? random : (random & 0x3) | 0x8;
    return value.toString(16);
  });
}

/** A new reviewed occurrence for a genuinely new planning review. */
export function mintShipmentPlanOccurrence(): string {
  return mintId();
}

function readStored(key: string): ShipmentPlanAttempt | null {
  const api = sessionStorageApi();
  if (!api) return memoryFallback.get(key) ?? null;
  try {
    const raw = api.getItem(key);
    if (typeof raw !== "string" || raw.trim().length === 0) return null;
    const parsed = JSON.parse(raw) as Partial<ShipmentPlanAttempt> | null;
    if (!parsed || typeof parsed !== "object" || !parsed.occurrenceKey) return null;
    return {
      viewerScopeKey: coalesce(parsed.viewerScopeKey) || DEFAULT_SCOPE,
      checkoutId: coalesce(parsed.checkoutId),
      shipmentId: coalesce(parsed.shipmentId),
      occurrenceKey: coalesce(parsed.occurrenceKey),
      receivingDate: coalesce(parsed.receivingDate) || null,
      receivingOpensAt: coalesce(parsed.receivingOpensAt) || null,
      receivingClosesAt: coalesce(parsed.receivingClosesAt) || null,
      attempts: Number.isFinite(parsed.attempts) ? Math.max(0, Number(parsed.attempts)) : 0,
      lastFailedAtUtc: coalesce(parsed.lastFailedAtUtc) || null,
      lastMessageFa: coalesce(parsed.lastMessageFa) || null,
    };
  } catch {
    return memoryFallback.get(key) ?? null;
  }
}

/** The recorded envelope for exactly this viewer/checkout/parcel, if any. */
export function readShipmentPlanAttempt(scope: ShipmentPlanAttemptScope): ShipmentPlanAttempt | null {
  const normalized = normalizeScope(scope);
  if (!normalized) return null;
  const recorded = readStored(storageKey(normalized));
  if (!recorded) return null;
  if (recorded.checkoutId !== normalized.checkoutId
    || recorded.shipmentId !== normalized.shipmentId
    || recorded.viewerScopeKey !== normalized.viewerScopeKey) {
    return null;
  }
  return recorded;
}

/** The outcome of persisting a reviewed intent; `durable` is true only after a verified read-back. */
export type ShipmentPlanAttemptWrite = {
  attempt: ShipmentPlanAttempt;
  durable: boolean;
};

/** Persists the reviewed intent BEFORE the first POST. */
export function writeShipmentPlanAttempt(input: {
  scope: ShipmentPlanAttemptScope;
  occurrenceKey: string;
  receivingDate?: string | null;
  receivingOpensAt?: string | null;
  receivingClosesAt?: string | null;
  attempts?: number;
  lastFailedAtUtc?: string | null;
  lastMessageFa?: string | null;
}): ShipmentPlanAttemptWrite | null {
  const normalized = normalizeScope(input.scope);
  const occurrenceKey = coalesce(input.occurrenceKey);
  if (!normalized || !occurrenceKey) return null;
  const attempt: ShipmentPlanAttempt = {
    viewerScopeKey: normalized.viewerScopeKey ?? DEFAULT_SCOPE,
    checkoutId: normalized.checkoutId,
    shipmentId: normalized.shipmentId,
    occurrenceKey,
    receivingDate: coalesce(input.receivingDate) || null,
    receivingOpensAt: coalesce(input.receivingOpensAt) || null,
    receivingClosesAt: coalesce(input.receivingClosesAt) || null,
    attempts: Math.max(1, input.attempts ?? 1),
    lastFailedAtUtc: input.lastFailedAtUtc ?? null,
    lastMessageFa: coalesce(input.lastMessageFa) || null,
  };
  const key = storageKey(normalized);
  const api = sessionStorageApi();
  if (api) {
    try {
      const serialized = JSON.stringify(attempt);
      api.setItem(key, serialized);
      if (api.getItem(key) === serialized) {
        memoryFallback.delete(key);
        return { attempt, durable: true };
      }
    } catch {
      // Storage refused (quota / private mode / throwing shim): fall through to the memory mirror.
    }
  }
  memoryFallback.set(key, attempt);
  return { attempt, durable: false };
}

/** Clears the envelope ONLY after an authoritative confirmation of this exact review. */
export function clearShipmentPlanAttempt(scope: ShipmentPlanAttemptScope): void {
  const normalized = normalizeScope(scope);
  if (!normalized) return;
  const key = storageKey(normalized);
  memoryFallback.delete(key);
  const api = sessionStorageApi();
  if (!api) return;
  try {
    api.removeItem(key);
  } catch {
    // A throwing storage cannot hold a durable record to clear.
  }
}

/** Records one failed/uncertain outcome while KEEPING the exact key and selection. */
export function markShipmentPlanAttemptFailed(
  scope: ShipmentPlanAttemptScope,
  attempt: ShipmentPlanAttempt,
  messageFa?: string | null,
): ShipmentPlanAttempt {
  return writeShipmentPlanAttempt({
    scope,
    occurrenceKey: attempt.occurrenceKey,
    receivingDate: attempt.receivingDate,
    receivingOpensAt: attempt.receivingOpensAt,
    receivingClosesAt: attempt.receivingClosesAt,
    attempts: attempt.attempts + 1,
    lastFailedAtUtc: new Date().toISOString(),
    lastMessageFa: messageFa ?? null,
  })?.attempt ?? attempt;
}
