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

/** TB-263: the semantic kind of the last attempt, so a reopen never re-labels a known fact as uncertain. */
export type PostDispatchAttemptOutcomeKind = "pending" | "refused" | "unknown";

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
  /**
   * TB-263: what the server actually said about the last attempt. `pending` means the decision is durably
   * recorded and still owed, `refused` a deterministic refusal, `unknown` a genuinely uncertain outcome.
   * A legacy record without it is read as `unknown` (the safe reading), never as pending or completed.
   */
  lastOutcomeKind?: PostDispatchAttemptOutcomeKind | null;
  /** Localized reason of that last attempt, so a reload shows the same truthful sentence. */
  lastMessageFa?: string | null;
};

const VERSION = "v1";
const DEFAULT_SCOPE = "__viewer__";

/**
 * TB-258-R1/R3: in-memory mirror used only as a same-mount convenience. It is deliberately NOT durability:
 * it does not survive a document reload, so it can never authorize a NEW mutation. Only a successful
 * sessionStorage write is reload-durable — see `PostDispatchAttemptWrite.durable`.
 */
const memoryFallback = new Map<string, PostDispatchAttempt>();

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

/**
 * TB-258-R3: probe on EVERY call, never latch. A transient storage failure must not permanently disable
 * durability for the rest of the page session — a restored storage can and must serve the next attempt.
 */
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

/** A new occurrence key for a genuinely new review. */
export function mintPostDispatchOccurrence(): string {
  return mintId();
}

function readOutcomeKind(value: unknown): PostDispatchAttemptOutcomeKind | null {
  return value === "pending" || value === "refused" || value === "unknown" ? value : null;
}

function readStored(key: string): PostDispatchAttempt | null {
  const api = sessionStorageApi();
  if (!api) return memoryFallback.get(key) ?? null;
  try {
    const raw = api.getItem(key);
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
      lastOutcomeKind: readOutcomeKind(parsed.lastOutcomeKind),
      lastMessageFa: coalesce(parsed.lastMessageFa) || null,
    };
  } catch {
    // A throwing/quota-broken storage is not a record. The in-memory mirror is the only same-mount source.
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
  "resolve_damaged_goods",
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

/**
 * TB-258-R3: the outcome of persisting a reviewed request. `durable` is true ONLY when the COMPLETE
 * envelope is in storage that survives a full document reload (sessionStorage, verified by read-back).
 * A memory-only write is never durability, so a NEW mutation must be refused (see the dialog's send
 * boundary) instead of risking a second allocation after the reload loses the map.
 */
export type PostDispatchAttemptWrite = {
  attempt: PostDispatchAttempt;
  durable: boolean;
};

/**
 * Persists the COMPLETE reviewed request BEFORE the first POST.
 *
 * Returns `null` only when the scope itself is incomplete. Otherwise the caller MUST inspect `durable`
 * and refuse a new mutation when it is false — that is the send boundary, not styling.
 */
export function writePostDispatchAttempt(input: {
  scope: PostDispatchAttemptScope;
  request: PostDispatchExceptionRequest;
  attempts?: number;
  lastFailedAtUtc?: string | null;
  lastOutcomeKind?: PostDispatchAttemptOutcomeKind | null;
  lastMessageFa?: string | null;
}): PostDispatchAttemptWrite | null {
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
    lastOutcomeKind: readOutcomeKind(input.lastOutcomeKind),
    lastMessageFa: coalesce(input.lastMessageFa) || null,
  };
  const key = storageKey(normalized);
  const api = sessionStorageApi();
  if (api) {
    try {
      const serialized = JSON.stringify(attempt);
      api.setItem(key, serialized);
      // Read-back: a storage that silently drops the write is NOT durable either.
      if (api.getItem(key) === serialized) {
        memoryFallback.delete(key);
        return { attempt, durable: true };
      }
    } catch {
      // Storage refused (quota / private mode / throwing shim): fall through to the memory-only mirror.
    }
  }
  memoryFallback.set(key, attempt);
  return { attempt, durable: false };
}

/**
 * Clears the envelope ONLY after an authoritative completion of this exact review.
 *
 * TB-258-R3: a write failure, an unknown outcome or an error code never clears it, and a durable record
 * that could not be rewritten is left in place rather than replaced by a memory-only shadow.
 */
export function clearPostDispatchAttempt(scope: PostDispatchAttemptScope): void {
  const normalized = normalizeScope(scope);
  if (!normalized) return;
  const key = storageKey(normalized);
  memoryFallback.delete(key);
  const api = sessionStorageApi();
  if (!api) return;
  try {
    api.removeItem(key);
  } catch {
    // A throwing storage cannot hold a durable record to clear; nothing else to do.
  }
}

/** Records one failed/uncertain outcome while KEEPING the exact envelope and key. */
export function markPostDispatchAttemptFailed(
  scope: PostDispatchAttemptScope,
  attempt: PostDispatchAttempt,
  outcome?: { kind: PostDispatchAttemptOutcomeKind; messageFa?: string | null },
): PostDispatchAttempt {
  return writePostDispatchAttempt({
    scope,
    request: attempt.request,
    attempts: attempt.attempts + 1,
    lastFailedAtUtc: new Date().toISOString(),
    lastOutcomeKind: outcome?.kind ?? "unknown",
    lastMessageFa: outcome?.messageFa ?? null,
  })?.attempt ?? attempt;
}
