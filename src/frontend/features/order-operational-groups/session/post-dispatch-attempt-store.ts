/**
 * TB-258-R1 — the retained post-dispatch review envelope.
 *
 * The reviewed request of a post-dispatch exception (non-delivery reason, origin receipt lines with
 * their physical condition, or the exact resend selections + occurrence key) is persisted BEFORE the
 * first POST, scoped to viewer + checkout + fulfillment + source parcel + mode. A lost response, a
 * 5xx/timeout, a typed refusal or a remount therefore reconciles the SAME request instead of creating
 * a second operation, while another viewer session can never inherit it.
 *
 * This follows the existing TB-253 retained-attempt safety pattern: only an authoritative completion
 * clears the envelope; there is deliberately NO "release on error code" helper, because a bare code
 * does not prove that an earlier request left nothing behind. A deliberate new operation requires a
 * completed/resolved attempt first.
 *
 * Nothing here is authorization: the server owns every bound (source parcel, accepted method, usable
 * received capacity, resolution state).
 */

import type {
  PostDispatchExceptionMode,
  PostDispatchExceptionRequest,
} from "../ui/post-dispatch-exception-dialog.tsx";

/** Identity of ONE reviewed surface: viewer session, checkout, fulfillment, parcel and mode. */
export type PostDispatchAttemptScope = {
  viewerScopeKey?: string | null;
  checkoutId: string;
  fulfillmentId: string;
  sourceShipmentId: string;
  mode: PostDispatchExceptionMode;
};

/** Durable client record of one prebound post-dispatch request. */
export type PostDispatchAttempt = {
  viewerScopeKey: string;
  checkoutId: string;
  fulfillmentId: string;
  sourceShipmentId: string;
  mode: PostDispatchExceptionMode;
  /** The COMPLETE reviewed request persisted before the first mutation. */
  request: PostDispatchExceptionRequest;
  attempts: number;
  lastFailedAtUtc: string | null;
};

const VERSION = "v1";
const DEFAULT_SCOPE = "__viewer__";

/**
 * In-memory mirror used ONLY when sessionStorage is unusable (private mode, quota, a throwing shim).
 * It keeps the exact request alive for this mount so a close/reopen inside one page session still
 * reconciles it instead of blindly minting a second operation. It is never consulted while
 * sessionStorage is healthy, so a cleared storage can never resurrect a stale envelope.
 */
const memoryFallback = new Map<string, PostDispatchAttempt>();
let storageBroken = false;

function coalesce(value: string | null | undefined): string {
  return typeof value === "string" ? value.trim() : "";
}

function normalizeScope(scope: PostDispatchAttemptScope): PostDispatchAttemptScope | null {
  const checkoutId = coalesce(scope.checkoutId);
  const fulfillmentId = coalesce(scope.fulfillmentId);
  const sourceShipmentId = coalesce(scope.sourceShipmentId);
  if (!checkoutId || !fulfillmentId || !sourceShipmentId || !scope.mode) return null;
  return {
    viewerScopeKey: coalesce(scope.viewerScopeKey) || DEFAULT_SCOPE,
    checkoutId,
    fulfillmentId,
    sourceShipmentId,
    mode: scope.mode,
  };
}

function storageKey(scope: PostDispatchAttemptScope): string {
  return [
    `tb.post-dispatch-attempt.${VERSION}`,
    scope.checkoutId,
    scope.viewerScopeKey ?? DEFAULT_SCOPE,
    scope.fulfillmentId,
    scope.sourceShipmentId,
    scope.mode,
  ].join(":");
}

function canUseSessionStorage(): boolean {
  if (storageBroken) return false;
  try {
    return typeof sessionStorage !== "undefined" && typeof sessionStorage.getItem === "function";
  } catch {
    storageBroken = true;
    return false;
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

/** A new occurrence key for a genuinely new review. */
export function mintPostDispatchOccurrence(): string {
  return mintId();
}

function readStored(key: string): PostDispatchAttempt | null {
  if (!canUseSessionStorage()) return memoryFallback.get(key) ?? null;
  try {
    const raw = sessionStorage.getItem(key);
    if (typeof raw !== "string" || raw.trim().length === 0) return null;
    const parsed = JSON.parse(raw) as Partial<PostDispatchAttempt> | null;
    if (!parsed || typeof parsed !== "object" || !parsed.request || !parsed.mode) return null;
    return {
      viewerScopeKey: coalesce(parsed.viewerScopeKey) || DEFAULT_SCOPE,
      checkoutId: coalesce(parsed.checkoutId),
      fulfillmentId: coalesce(parsed.fulfillmentId),
      sourceShipmentId: coalesce(parsed.sourceShipmentId),
      mode: parsed.mode,
      request: parsed.request,
      attempts: Number.isFinite(parsed.attempts) ? Math.max(0, Number(parsed.attempts)) : 0,
      lastFailedAtUtc: coalesce(parsed.lastFailedAtUtc) || null,
    };
  } catch {
    storageBroken = true;
    return memoryFallback.get(key) ?? null;
  }
}

/** The recorded envelope for exactly this viewer/checkout/fulfillment/parcel/mode, if any. */
export function readPostDispatchAttempt(scope: PostDispatchAttemptScope): PostDispatchAttempt | null {
  const normalized = normalizeScope(scope);
  if (!normalized) return null;
  const recorded = readStored(storageKey(normalized));
  if (!recorded) return null;
  // A record from another scope can never be inherited, even if a storage key was crafted by hand.
  if (recorded.checkoutId !== normalized.checkoutId
    || recorded.fulfillmentId !== normalized.fulfillmentId
    || recorded.sourceShipmentId !== normalized.sourceShipmentId
    || recorded.viewerScopeKey !== normalized.viewerScopeKey
    || recorded.mode !== normalized.mode) {
    return null;
  }
  return recorded;
}

/** Every post-dispatch mode, in lifecycle order. The mode is the last component of the storage key. */
export const POST_DISPATCH_MODES: ReadonlyArray<PostDispatchExceptionMode> = [
  "record_non_delivery",
  "start_return",
  "record_return_receipt",
  "resend_shipment",
];

/**
 * TB-258-R2: every envelope still retained for ONE parcel — across modes — for this viewer scope.
 *
 * The parcel card uses this to keep ONE recoverable entry visible after a refresh or after the fresh
 * projection stopped offering the action: the reviewed request is still owed a reconciliation even
 * when no ordinary action button exists any more. Newest first, so the card shows the live obligation.
 */
export function listPostDispatchAttemptsForParcel(input: {
  viewerScopeKey?: string | null;
  checkoutId: string;
  fulfillmentId: string;
  sourceShipmentId: string;
}): PostDispatchAttempt[] {
  const attempts: PostDispatchAttempt[] = [];
  for (const mode of POST_DISPATCH_MODES) {
    const recorded = readPostDispatchAttempt({
      viewerScopeKey: input.viewerScopeKey ?? null,
      checkoutId: input.checkoutId,
      fulfillmentId: input.fulfillmentId,
      sourceShipmentId: input.sourceShipmentId,
      mode,
    });
    if (recorded) attempts.push(recorded);
  }

  return attempts.sort((left, right) => {
    const leftAt = left.lastFailedAtUtc ?? "";
    const rightAt = right.lastFailedAtUtc ?? "";
    if (leftAt === rightAt) return right.attempts - left.attempts;
    return rightAt.localeCompare(leftAt);
  });
}

/** Persists the COMPLETE reviewed request BEFORE the first POST. */
export function writePostDispatchAttempt(input: {
  scope: PostDispatchAttemptScope;
  request: PostDispatchExceptionRequest;
  attempts?: number;
  lastFailedAtUtc?: string | null;
}): PostDispatchAttempt | null {
  const normalized = normalizeScope(input.scope);
  if (!normalized) return null;
  const attempt: PostDispatchAttempt = {
    viewerScopeKey: normalized.viewerScopeKey ?? DEFAULT_SCOPE,
    checkoutId: normalized.checkoutId,
    fulfillmentId: normalized.fulfillmentId,
    sourceShipmentId: normalized.sourceShipmentId,
    mode: normalized.mode,
    request: input.request,
    attempts: Math.max(1, input.attempts ?? 1),
    lastFailedAtUtc: input.lastFailedAtUtc ?? null,
  };
  const key = storageKey(normalized);
  if (canUseSessionStorage()) {
    try {
      sessionStorage.setItem(key, JSON.stringify(attempt));
      memoryFallback.delete(key);
      return attempt;
    } catch {
      // Storage refused (quota / private mode): the in-memory mirror keeps this mount authoritative.
      storageBroken = true;
    }
  }
  memoryFallback.set(key, attempt);
  return attempt;
}

/** Clears the envelope ONLY after an authoritative completion of this exact review. */
export function clearPostDispatchAttempt(scope: PostDispatchAttemptScope): void {
  const normalized = normalizeScope(scope);
  if (!normalized) return;
  const key = storageKey(normalized);
  memoryFallback.delete(key);
  if (!canUseSessionStorage()) return;
  try {
    sessionStorage.removeItem(key);
  } catch {
    storageBroken = true;
  }
}

/** Records one failed/uncertain outcome while KEEPING the exact envelope and key. */
export function markPostDispatchAttemptFailed(
  scope: PostDispatchAttemptScope,
  attempt: PostDispatchAttempt,
): PostDispatchAttempt {
  return writePostDispatchAttempt({
    scope,
    request: attempt.request,
    attempts: attempt.attempts + 1,
    lastFailedAtUtc: new Date().toISOString(),
  }) ?? attempt;
}
