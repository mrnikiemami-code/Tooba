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
  /** TB-270-R3: the captured receiving zone shown in the review (comparison evidence, never authority). */
  receivingTimeZone: string | null;
  attempts: number;
  lastFailedAtUtc: string | null;
  lastMessageFa: string | null;
  /**
   * TB-270-R3: the EXACT request body that was reviewed and sent (serialized JSON). While the outcome is
   * uncertain this string is re-sent byte-for-byte, so no field can drift between attempts.
   */
  requestBody: string | null;
  /** TB-270-R3: the server-issued review fingerprint that was echoed in `requestBody`. */
  reviewFingerprint: string | null;
  /** TB-270-R3: the last outcome was genuinely unknown (transport loss / 5xx / timeout) and must be retried as-is. */
  uncertain: boolean;
  /**
   * TB-270-R4 Repair A: this occurrence may already have been SENT. It is written (with the exact body) BEFORE the
   * operation executor is invoked, read back, and is only ever cleared by the response that proves the outcome of
   * THAT exact occurrence/body. A reload, unmount, close or process interruption therefore recovers the identical
   * intent instead of minting a second review. Records written by TB-270-R3 default to `true` on read: a retained
   * body with no authoritative terminal result cannot be assumed never sent.
   */
  unresolved: boolean;
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
      receivingTimeZone: coalesce(parsed.receivingTimeZone) || null,
      attempts: Number.isFinite(parsed.attempts) ? Math.max(0, Number(parsed.attempts)) : 0,
      lastFailedAtUtc: coalesce(parsed.lastFailedAtUtc) || null,
      lastMessageFa: coalesce(parsed.lastMessageFa) || null,
      requestBody: coalesce(parsed.requestBody) || null,
      reviewFingerprint: coalesce(parsed.reviewFingerprint) || null,
      uncertain: parsed.uncertain === true,
      // TB-270-R4: a record retained by the older shape cannot prove its outcome ended; only an explicit `false`
      // (never written by any previous version) marks it resolved.
      unresolved: parsed.unresolved !== false,
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
  receivingTimeZone?: string | null;
  attempts?: number;
  lastFailedAtUtc?: string | null;
  lastMessageFa?: string | null;
  requestBody?: string | null;
  reviewFingerprint?: string | null;
  uncertain?: boolean;
  unresolved?: boolean;
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
    receivingTimeZone: coalesce(input.receivingTimeZone) || null,
    attempts: Math.max(1, input.attempts ?? 1),
    lastFailedAtUtc: input.lastFailedAtUtc ?? null,
    lastMessageFa: coalesce(input.lastMessageFa) || null,
    requestBody: coalesce(input.requestBody) || null,
    reviewFingerprint: coalesce(input.reviewFingerprint) || null,
    uncertain: input.uncertain === true,
    // TB-270-R4 Repair A: a write that carries a reviewed body is by definition an UNRESOLVED intent.
    unresolved: input.unresolved !== false,
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

/** Records one failed/uncertain outcome while KEEPING the exact key, selection and reviewed body. */
export function markShipmentPlanAttemptFailed(
  scope: ShipmentPlanAttemptScope,
  attempt: ShipmentPlanAttempt,
  messageFa?: string | null,
  uncertain = false,
): ShipmentPlanAttempt {
  return writeShipmentPlanAttempt({
    scope,
    occurrenceKey: attempt.occurrenceKey,
    receivingDate: attempt.receivingDate,
    receivingOpensAt: attempt.receivingOpensAt,
    receivingClosesAt: attempt.receivingClosesAt,
    receivingTimeZone: attempt.receivingTimeZone,
    attempts: attempt.attempts + 1,
    lastFailedAtUtc: new Date().toISOString(),
    lastMessageFa: messageFa ?? null,
    requestBody: attempt.requestBody,
    reviewFingerprint: attempt.reviewFingerprint,
    uncertain,
    unresolved: true,
  })?.attempt ?? attempt;
}

/**
 * TB-270-R4 Repair A: a retained reviewed body is an UNRESOLVED send, never "history".
 *
 * The record is written (with the exact serialized body and its read-back verification) BEFORE the first POST, so a
 * reload/unmount/close/process kill in that window leaves exactly this state behind. TB-270-R3 records that carried a
 * body but `uncertain: false` are migrated conservatively: without an explicit `unresolved: false` the outcome is
 * still unknown, so the exact body — never a freshly rebuilt selection — is what may be sent again.
 */
export function isUnresolvedShipmentPlanAttempt(
  attempt: ShipmentPlanAttempt | null,
): attempt is ShipmentPlanAttempt {
  return Boolean(attempt && attempt.requestBody && attempt.unresolved !== false);
}

/**
 * TB-270-R4 Repair D: compare-and-clear ONE exact occurrence/body.
 *
 * A late answer must never erase a newer review that was recorded for the same viewer/checkout/parcel in the
 * meantime, so the currently stored record is compared by occurrence AND body before anything is removed.
 * Returns `true` only when THIS exact intent was the one cleared.
 */
export function clearShipmentPlanAttemptIfCurrent(
  scope: ShipmentPlanAttemptScope,
  occurrenceKey: string,
  requestBody?: string | null,
): boolean {
  const recorded = readShipmentPlanAttempt(scope);
  if (!recorded) return false;
  if (recorded.occurrenceKey !== coalesce(occurrenceKey)) return false;
  const body = coalesce(requestBody);
  if (body && recorded.requestBody && recorded.requestBody !== body) return false;
  clearShipmentPlanAttempt(scope);
  return true;
}

/**
 * TB-270-R4 Repair D: compare-and-update ONE exact occurrence.
 *
 * An outcome that arrives after the surface was closed/unmounted/retargeted still records the truth for ITS OWN
 * occurrence, but it must never overwrite the state of a newer occurrence stored for the same scope. Returns the
 * recorded attempt only when it was this exact occurrence that was updated.
 */
export function markShipmentPlanAttemptFailedIfCurrent(
  scope: ShipmentPlanAttemptScope,
  attempt: ShipmentPlanAttempt,
  messageFa?: string | null,
  uncertain = false,
): ShipmentPlanAttempt | null {
  const recorded = readShipmentPlanAttempt(scope);
  if (!recorded) return null;
  if (recorded.occurrenceKey !== attempt.occurrenceKey) return null;
  if (recorded.requestBody !== attempt.requestBody) return null;
  return markShipmentPlanAttemptFailed(scope, attempt, messageFa, uncertain);
}

/**
 * TB-270-R3: a deterministic refusal ends this review: the recorded body/key are cleared so the next intent can
 * never reuse a stale reviewed context. The caller displays the exact prior outcome before asking for a new review.
 */
export function endShipmentPlanReview(scope: ShipmentPlanAttemptScope): void {
  clearShipmentPlanAttempt(scope);
}
