/**
 * TB-253-R3/R4: client-side prebound occurrence for the ONE Marketplace operational group workflow.
 *
 * The occurrence is minted BEFORE the first mutation and the COMPLETE reviewed envelope (member
 * groups, per-line quantities, explicit reuse parcels and the actor/viewer scope) is persisted before
 * the first POST. A lost response, 5xx, cancellation or remount therefore reconciles the SAME
 * occurrence instead of allocating a new parcel set, while a CHANGED reviewed request is a genuinely
 * different operation and gets a NEW occurrence — the server treats a changed envelope under a
 * recorded occurrence as a typed intent conflict, so the client must never reuse it there.
 *
 * Only an explicit successful completion clears the attempt, so the next deliberate run is a new
 * occurrence with its own distinct parcels.
 *
 * This is never authorization: the server validates group ownership, quantities and the occurrence
 * key itself. The stored envelope is scoped to the viewer/owner so one admin session cannot inherit
 * another's in-flight occurrence.
 */

export type OperationalGroupOccurrenceSelection = {
  orderLineId: string;
  quantity: number;
};

/** The exact reviewed request persisted before the first mutation. */
export type OperationalGroupOccurrenceRequest = {
  members: string[];
  selections: OperationalGroupOccurrenceSelection[];
  reuseShipmentIds: string[];
};

/** Durable client record of one prebound workflow occurrence. */
export type OperationalGroupAttempt = OperationalGroupOccurrenceRequest & {
  occurrenceId: string;
  checkoutId: string;
  operationalGroupKey: string;
  scopeKey: string;
  fingerprint: string;
  attempts: number;
  lastFailedAtUtc: string | null;
  lastErrorCode: string | null;
};

/** Resolution of the occurrence a new review attempt must use. */
export type OperationalGroupAttemptResolution = {
  occurrenceId: string;
  attempt: OperationalGroupAttempt;
  /** True when an earlier attempt with the SAME reviewed envelope is being reconciled/retried. */
  resumingRecordedAttempt: boolean;
  /** True when a previously recorded attempt existed but the reviewed envelope changed. */
  changedRequest: boolean;
};

const VERSION = "v2";
const GROUP_SCOPE = "__group__";

function storageKey(checkoutId: string, operationalGroupKey: string, scopeKey: string): string {
  return `tb.operational-group-attempt.${VERSION}:${checkoutId}:${scopeKey}:${operationalGroupKey}`;
}

function canUseSessionStorage(): boolean {
  try {
    return typeof sessionStorage !== "undefined" && typeof sessionStorage.getItem === "function";
  } catch {
    return false;
  }
}

function newOccurrenceId(): string {
  const cryptoApi = globalThis.crypto;
  if (cryptoApi && typeof cryptoApi.randomUUID === "function") {
    return cryptoApi.randomUUID();
  }
  // Deterministic-format fallback for runtimes without crypto.randomUUID.
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (token) => {
    const random = Math.floor(Math.random() * 16);
    const value = token === "x" ? random : (random & 0x3) | 0x8;
    return value.toString(16);
  });
}

export function mintOperationalGroupOccurrence(): string {
  return newOccurrenceId();
}

function normalizeScopeKey(scopeKey: string | null | undefined): string {
  const trimmed = typeof scopeKey === "string" ? scopeKey.trim() : "";
  return trimmed.length > 0 ? trimmed : GROUP_SCOPE;
}

function normalizeQuantity(quantity: number): number {
  if (!Number.isFinite(quantity) || quantity <= 0) return 0;
  // Quantities are decimal in the domain; keep a stable text form so 2 and 2.0 cannot differ.
  return Math.round(quantity * 1_000_000) / 1_000_000;
}

function canonicalSelections(
  selections: readonly OperationalGroupOccurrenceSelection[],
): OperationalGroupOccurrenceSelection[] {
  return selections
    .map((selection) => ({
      orderLineId: String(selection.orderLineId ?? "").trim(),
      quantity: normalizeQuantity(selection.quantity),
    }))
    .filter((selection) => selection.orderLineId.length > 0 && selection.quantity > 0)
    .sort((a, b) => a.orderLineId.localeCompare(b.orderLineId));
}

function canonicalIds(ids: readonly string[] | null | undefined): string[] {
  const unique = new Set<string>();
  for (const id of ids ?? []) {
    const trimmed = String(id ?? "").trim();
    if (trimmed.length > 0) unique.add(trimmed);
  }
  return [...unique].sort((a, b) => a.localeCompare(b));
}

/**
 * Stable fingerprint of the COMPLETE reviewed envelope. Order-insensitive: the same reviewed set
 * always yields the same fingerprint, and any changed member, quantity or reuse parcel does not.
 */
export function operationalGroupRequestFingerprint(
  request: OperationalGroupOccurrenceRequest,
): string {
  const canonical = JSON.stringify({
    members: canonicalIds(request.members),
    selections: canonicalSelections(request.selections).map((s) => [s.orderLineId, s.quantity]),
    reuse: canonicalIds(request.reuseShipmentIds),
  });
  // FNV-1a 64-bit-ish hash in two 32-bit lanes; collision-free enough for a session key and never a
  // security boundary (the server owns authority).
  let high = 0x811c9dc5;
  let low = 0x01000193;
  for (let index = 0; index < canonical.length; index += 1) {
    const code = canonical.charCodeAt(index);
    high = Math.imul(high ^ code, 0x01000193) >>> 0;
    low = Math.imul(low ^ ((code << 1) | (code >> 15)), 0x85ebca6b) >>> 0;
  }
  return `${high.toString(16).padStart(8, "0")}${low.toString(16).padStart(8, "0")}`;
}

/** @deprecated TB-253-R4 superseded the occurrence-only helpers by the full attempt record. */
export function readOperationalGroupOccurrence(
  checkoutId: string,
  operationalGroupKey: string,
): string | null {
  return readOperationalGroupAttempt(coalesce(checkoutId), coalesce(operationalGroupKey), null)
    ?.occurrenceId ?? null;
}

/** @deprecated TB-253-R4 superseded the occurrence-only helpers by the full attempt record. */
export function writeOperationalGroupOccurrence(
  checkoutId: string,
  operationalGroupKey: string,
  occurrenceId: string,
): void {
  if (!checkoutId || !operationalGroupKey || !occurrenceId) return;
  const existing = readOperationalGroupAttempt(checkoutId, operationalGroupKey, null);
  writeOperationalGroupAttempt({
    occurrenceId: occurrenceId.trim(),
    checkoutId,
    operationalGroupKey,
    scopeKey: GROUP_SCOPE,
    fingerprint: existing?.fingerprint ?? operationalGroupRequestFingerprint({
      members: [],
      selections: [],
      reuseShipmentIds: [],
    }),
    members: existing?.members ?? [],
    selections: existing?.selections ?? [],
    reuseShipmentIds: existing?.reuseShipmentIds ?? [],
    attempts: existing?.attempts ?? 0,
    lastFailedAtUtc: existing?.lastFailedAtUtc ?? null,
    lastErrorCode: existing?.lastErrorCode ?? null,
  });
}

/** @deprecated TB-253-R4 superseded the occurrence-only helpers by the full attempt record. */
export function clearOperationalGroupOccurrence(
  checkoutId: string,
  operationalGroupKey: string,
): void {
  clearOperationalGroupAttempt(checkoutId, operationalGroupKey, null);
}

function coalesce(value: string | null | undefined): string {
  return typeof value === "string" ? value.trim() : "";
}

/** Reads the recorded attempt for one checkout/group within one viewer/owner scope. */
export function readOperationalGroupAttempt(
  checkoutId: string,
  operationalGroupKey: string,
  scopeKey: string | null | undefined,
): OperationalGroupAttempt | null {
  if (!checkoutId || !operationalGroupKey || !canUseSessionStorage()) return null;
  try {
    const raw = sessionStorage.getItem(
      storageKey(checkoutId, operationalGroupKey, normalizeScopeKey(scopeKey)),
    );
    if (typeof raw !== "string" || raw.trim().length === 0) return null;
    const parsed = JSON.parse(raw) as Partial<OperationalGroupAttempt> | null;
    if (!parsed || typeof parsed !== "object") return null;
    const occurrenceId = coalesce(parsed.occurrenceId);
    if (occurrenceId.length === 0) return null;
    const selections = Array.isArray(parsed.selections)
      ? canonicalSelections(parsed.selections as OperationalGroupOccurrenceSelection[])
      : [];
    const attempt: OperationalGroupAttempt = {
      occurrenceId,
      checkoutId: coalesce(parsed.checkoutId) || checkoutId,
      operationalGroupKey: coalesce(parsed.operationalGroupKey) || operationalGroupKey,
      scopeKey: coalesce(parsed.scopeKey) || normalizeScopeKey(scopeKey),
      fingerprint: coalesce(parsed.fingerprint),
      members: canonicalIds(parsed.members ?? []),
      selections,
      reuseShipmentIds: canonicalIds(parsed.reuseShipmentIds ?? []),
      attempts: Number.isFinite(parsed.attempts) ? Math.max(0, Number(parsed.attempts)) : 0,
      lastFailedAtUtc: coalesce(parsed.lastFailedAtUtc) || null,
      lastErrorCode: coalesce(parsed.lastErrorCode) || null,
    };
    // A record from another checkout/group/scope can never be inherited.
    if (attempt.checkoutId !== checkoutId || attempt.operationalGroupKey !== operationalGroupKey) {
      return null;
    }
    return attempt;
  } catch {
    return null;
  }
}

/** Persists the COMPLETE prebound envelope BEFORE the first mutation. */
export function writeOperationalGroupAttempt(attempt: OperationalGroupAttempt): void {
  if (!attempt.checkoutId
    || !attempt.operationalGroupKey
    || !attempt.occurrenceId
    || !canUseSessionStorage()) {
    return;
  }
  try {
    sessionStorage.setItem(
      storageKey(
        attempt.checkoutId,
        attempt.operationalGroupKey,
        normalizeScopeKey(attempt.scopeKey),
      ),
      JSON.stringify({
        ...attempt,
        members: canonicalIds(attempt.members),
        selections: canonicalSelections(attempt.selections),
        reuseShipmentIds: canonicalIds(attempt.reuseShipmentIds),
      }),
    );
  } catch {
    /* quota / private mode — the in-memory attempt remains authoritative for this mount */
  }
}

/** Clears the recorded attempt only after a completed operation (or an explicit reset). */
export function clearOperationalGroupAttempt(
  checkoutId: string,
  operationalGroupKey: string,
  scopeKey: string | null | undefined,
): void {
  if (!checkoutId || !operationalGroupKey || !canUseSessionStorage()) return;
  try {
    sessionStorage.removeItem(
      storageKey(checkoutId, operationalGroupKey, normalizeScopeKey(scopeKey)),
    );
  } catch {
    /* ignore */
  }
}

/**
 * Decides which occurrence this review attempt belongs to.
 *
 * Same reviewed envelope as the recorded attempt → resume/retry THAT occurrence (lost response,
 * 5xx, cancellation). A changed reviewed envelope → a NEW occurrence, because the recorded envelope
 * is immutable server-side. No recorded attempt → a freshly minted occurrence.
 *
 * The decision is persisted before returning, so it survives a remount between decision and POST.
 */
export function resolveOperationalGroupAttempt(input: {
  checkoutId: string;
  operationalGroupKey: string;
  scopeKey: string | null | undefined;
  request: OperationalGroupOccurrenceRequest;
}): OperationalGroupAttemptResolution {
  const { checkoutId, operationalGroupKey } = input;
  const scopeKey = normalizeScopeKey(input.scopeKey);
  const members = canonicalIds(input.request.members);
  const selections = canonicalSelections(input.request.selections);
  const reuseShipmentIds = canonicalIds(input.request.reuseShipmentIds);
  const fingerprint = operationalGroupRequestFingerprint({ members, selections, reuseShipmentIds });

  const recorded = readOperationalGroupAttempt(checkoutId, operationalGroupKey, scopeKey);
  const sameEnvelope = Boolean(recorded)
    && (recorded!.fingerprint.length === 0 || recorded!.fingerprint === fingerprint);
  const changedRequest = Boolean(recorded) && !sameEnvelope;

  const attempt: OperationalGroupAttempt = {
    occurrenceId: sameEnvelope && recorded ? recorded.occurrenceId : mintOperationalGroupOccurrence(),
    checkoutId,
    operationalGroupKey,
    scopeKey,
    fingerprint,
    members,
    selections,
    reuseShipmentIds,
    attempts: sameEnvelope && recorded ? recorded.attempts + 1 : 1,
    lastFailedAtUtc: sameEnvelope && recorded ? recorded.lastFailedAtUtc : null,
    lastErrorCode: sameEnvelope && recorded ? recorded.lastErrorCode : null,
  };
  writeOperationalGroupAttempt(attempt);

  return {
    occurrenceId: attempt.occurrenceId,
    attempt,
    resumingRecordedAttempt: sameEnvelope,
    changedRequest,
  };
}

/**
 * Records the failed outcome of one attempt while KEEPING the envelope for reconcile/retry.
 * Never clears the occurrence.
 */
export function markOperationalGroupAttemptFailed(input: {
  attempt: OperationalGroupAttempt;
  errorCode?: string | null;
}): OperationalGroupAttempt {
  const { attempt } = input;
  const failed: OperationalGroupAttempt = {
    ...attempt,
    lastFailedAtUtc: new Date().toISOString(),
    lastErrorCode: coalesce(input.errorCode) || null,
  };
  writeOperationalGroupAttempt(failed);
  return failed;
}
