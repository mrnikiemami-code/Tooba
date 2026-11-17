/**
 * TB-275 / TB-276: the durable review intent of one return case.
 *
 * An operator action on a return case (receipt, decision, refund, retry, physical settlement) is a durable
 * consequence, so the reviewed envelope is written, READ BACK, and only then sent. TB-276 repairs the connected
 * lifecycle TB-275 left open:
 *
 * - the envelope carries the immutable TARGET (case + owning checkout/route) and the EXACT serialized transport
 *   body, so a later reload/refetch can never rebuild the request from a newer snapshot;
 * - a write is only "durable" after a verified read-back of the exact bytes;
 * - stored data is validated on every read: an unknown action, a foreign case, a missing/unparseable body or an
 *   unsupported version is UNREADABLE pending evidence — never silently coerced into a sendable intent;
 * - compare-and-clear requires the exact case + route + action + occurrence AND the exact body, so a late answer for
 *   an old intent can never erase a newer envelope.
 */

/** TB-276: bump when the stored shape changes incompatibly; an older envelope reads as unreadable evidence. */
export const RETURN_REVIEW_INTENT_VERSION = "v2";

/** TB-275/TB-277: the deliberate review intents of one return case. */
export type ReturnReviewAction =
  | "receipt"
  | "approve"
  | "reject"
  /** TB-277: the FIRST refund of an approved case — a deliberate financial action, never part of "approve". */
  | "start_refund"
  | "resume_refund"
  | "retry_refund"
  | "settle_physical";

/**
 * TB-276: which owning route executes the reviewed action. `returns` is the Returns-owned admin route;
 * `order-operation` is the existing Order operations authority (the reviewed approve/reject transport).
 */
export type ReturnReviewTarget = "returns" | "order-operation";

/** The reviewed intent of ONE action, durable before the request is sent. */
export interface ReturnReviewIntent {
  version: string;
  action: ReturnReviewAction;
  target: ReturnReviewTarget;
  returnRequestId: string;
  /** The frozen owning route identity (the checkout for `order-operation`); never re-read at send time. */
  routeTargetId: string | null;
  occurrenceKey: string;
  /** The EXACT serialized transport body: re-sent byte-for-byte while the outcome is unknown. */
  requestBody: string;
  recordedAt: string;
}

/** The minimal storage seam, so the intent envelope is testable without a browser. */
export interface IntentStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

/** What a case's storage actually holds right now. */
export type ReturnReviewIntentRead =
  | { kind: "none" }
  | { kind: "pending"; intent: ReturnReviewIntent }
  /** Stored evidence exists but cannot be validated: it must stay visible and must not authorize any send. */
  | { kind: "unreadable"; raw: string }
  /** The storage itself cannot be read (private mode / throwing shim): no durable intent can be proven. */
  | { kind: "unavailable" };

const KNOWN_ACTIONS: ReadonlySet<string> = new Set<ReturnReviewAction>([
  "receipt",
  "approve",
  "reject",
  "start_refund",
  "resume_refund",
  "retry_refund",
  "settle_physical",
]);

const KNOWN_TARGETS: ReadonlySet<string> = new Set<ReturnReviewTarget>(["returns", "order-operation"]);

/** The storage key of one case's unresolved review intent. */
export function returnIntentStorageKey(returnRequestId: string): string {
  return `tb275-return-intent:${returnRequestId}`;
}

/** A new reviewed occurrence identity. */
export function createOccurrenceKey(): string {
  const cryptoApi = typeof globalThis === "object" ? globalThis.crypto : undefined;
  if (cryptoApi && typeof cryptoApi.randomUUID === "function") return cryptoApi.randomUUID();
  return `tb276-${Date.now().toString(16)}-${Math.random().toString(16).slice(2, 10)}`;
}

function coalesce(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

export interface ReturnReviewIntentInput {
  action: ReturnReviewAction;
  target: ReturnReviewTarget;
  returnRequestId: string;
  /** `order-operation` intents require the frozen checkout id; Returns-owned routes require `null`. */
  routeTargetId?: string | null;
  body: Record<string, unknown>;
  occurrenceKey?: string;
}

/**
 * Builds the durable envelope of one reviewed action. Returns `null` when the reviewed context is incomplete, so an
 * unaddressable intent is refused BEFORE anything is stored or sent (never a sendable envelope without its target).
 */
export function buildReturnReviewIntent(input: ReturnReviewIntentInput): ReturnReviewIntent | null {
  const returnRequestId = coalesce(input.returnRequestId);
  const occurrenceKey = coalesce(input.occurrenceKey) || createOccurrenceKey();
  const action = coalesce(input.action);
  const target = coalesce(input.target);
  const routeTargetId = coalesce(input.routeTargetId);
  if (!returnRequestId || !occurrenceKey) return null;
  if (!KNOWN_ACTIONS.has(action) || !KNOWN_TARGETS.has(target)) return null;
  if (!input.body || typeof input.body !== "object" || Array.isArray(input.body)) return null;
  if (target === "order-operation" ? !routeTargetId : routeTargetId !== "") return null;
  const requestBody = JSON.stringify(input.body);
  if (!requestBody || requestBody === "{}") return null;
  return {
    version: RETURN_REVIEW_INTENT_VERSION,
    action: action as ReturnReviewAction,
    target: target as ReturnReviewTarget,
    returnRequestId,
    routeTargetId: target === "order-operation" ? routeTargetId : null,
    occurrenceKey,
    requestBody,
    recordedAt: new Date().toISOString(),
  };
}

/** The exact bytes that were reviewed; the only body ever sent for this occurrence. */
export function serializeReturnReviewIntent(intent: ReturnReviewIntent): string {
  return JSON.stringify(intent);
}

/** Parses the frozen transport body of an intent; `null` means the stored bytes are not a usable body. */
export function parseReturnReviewBody(intent: ReturnReviewIntent): Record<string, unknown> | null {
  try {
    const parsed = JSON.parse(intent.requestBody) as unknown;
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) return null;
    return parsed as Record<string, unknown>;
  } catch {
    return null;
  }
}

function decodeIntent(raw: string, returnRequestId: string): ReturnReviewIntent | null {
  let parsed: Partial<ReturnReviewIntent> | null;
  try {
    parsed = JSON.parse(raw) as Partial<ReturnReviewIntent> | null;
  } catch {
    return null;
  }
  if (!parsed || typeof parsed !== "object") return null;
  if (coalesce(parsed.version) !== RETURN_REVIEW_INTENT_VERSION) return null;
  if (coalesce(parsed.returnRequestId) !== returnRequestId) return null;
  const action = coalesce(parsed.action);
  const target = coalesce(parsed.target);
  const occurrenceKey = coalesce(parsed.occurrenceKey);
  const requestBody = typeof parsed.requestBody === "string" ? parsed.requestBody : "";
  if (!KNOWN_ACTIONS.has(action) || !KNOWN_TARGETS.has(target) || !occurrenceKey || !requestBody) return null;
  const routeTargetId = coalesce(parsed.routeTargetId);
  if (target === "order-operation" ? !routeTargetId : routeTargetId !== "") return null;
  // The frozen bytes must still be the exact body object they claim to be; anything else is corrupt evidence.
  let decodedBody: unknown;
  try {
    decodedBody = JSON.parse(requestBody) as unknown;
  } catch {
    return null;
  }
  if (!decodedBody || typeof decodedBody !== "object" || Array.isArray(decodedBody)) return null;
  if (JSON.stringify(decodedBody) !== requestBody) return null;
  return {
    version: RETURN_REVIEW_INTENT_VERSION,
    action: action as ReturnReviewAction,
    target: target as ReturnReviewTarget,
    returnRequestId,
    routeTargetId: target === "order-operation" ? routeTargetId : null,
    occurrenceKey,
    requestBody,
    recordedAt: coalesce(parsed.recordedAt),
  };
}

/**
 * Reads the stored envelope of one case. A throwing getter is reported as `unavailable` (no durable proof exists);
 * stored-but-invalid bytes are reported as `unreadable` so no caller can quietly treat them as "nothing stored".
 */
export function readReturnReviewIntent(
  storage: IntentStorage | null,
  returnRequestId: string,
): ReturnReviewIntentRead {
  if (!storage) return { kind: "unavailable" };
  let raw: string | null;
  try {
    raw = storage.getItem(returnIntentStorageKey(returnRequestId));
  } catch {
    return { kind: "unavailable" };
  }
  if (typeof raw !== "string" || raw.trim().length === 0) return { kind: "none" };
  const intent = decodeIntent(raw, returnRequestId);
  return intent ? { kind: "pending", intent } : { kind: "unreadable", raw };
}

/**
 * Persists the reviewed envelope BEFORE the request is sent and proves it by reading the exact bytes back.
 * Returns `false` when the storage silently discarded the write, so the caller can refuse to send blind.
 */
export function storeReturnReviewIntent(
  storage: IntentStorage | null,
  intent: ReturnReviewIntent,
): boolean {
  if (!storage) return false;
  const key = returnIntentStorageKey(intent.returnRequestId);
  const serialized = serializeReturnReviewIntent(intent);
  try {
    storage.setItem(key, serialized);
    return storage.getItem(key) === serialized;
  } catch {
    return false;
  }
}

/** True when the two envelopes name the same reviewed command (target, action, occurrence and exact bytes). */
export function sameReturnReviewIntent(left: ReturnReviewIntent, right: ReturnReviewIntent): boolean {
  return left.returnRequestId === right.returnRequestId
    && left.target === right.target
    && left.action === right.action
    && (left.routeTargetId ?? "") === (right.routeTargetId ?? "")
    && left.occurrenceKey === right.occurrenceKey
    && left.requestBody === right.requestBody;
}

/**
 * Compare-and-clear ONE exact reviewed command: case, route target, action, occurrence AND exact body must still be
 * the stored bytes. A late answer for an older intent therefore never erases a newer envelope.
 */
export function clearReturnReviewIntentIfCurrent(
  storage: IntentStorage | null,
  intent: ReturnReviewIntent,
): boolean {
  if (!storage) return false;
  const key = returnIntentStorageKey(intent.returnRequestId);
  try {
    const raw = storage.getItem(key);
    if (typeof raw !== "string" || raw !== serializeReturnReviewIntent(intent)) return false;
    storage.removeItem(key);
    return storage.getItem(key) === null;
  } catch {
    return false;
  }
}

/**
 * TB-276: discards ONLY bytes that no reader can validate (corrupt/unsupported/foreign). Valid evidence is never
 * removed here — that requires the authoritative response of the exact command it describes.
 */
export function discardUnreadableReturnReviewIntent(
  storage: IntentStorage | null,
  returnRequestId: string,
): boolean {
  if (!storage) return false;
  const key = returnIntentStorageKey(returnRequestId);
  try {
    const raw = storage.getItem(key);
    if (typeof raw !== "string" || raw.trim().length === 0) return false;
    if (decodeIntent(raw, returnRequestId)) return false;
    storage.removeItem(key);
    return storage.getItem(key) === null;
  } catch {
    return false;
  }
}

/**
 * TB-276 / TB-277-R1: the reviewed commands whose owning code path proves refusal happened BEFORE any durable write of
 * that intent. This is a deliberately small, auditable TOKEN list — never "every 4xx", never message matching:
 *
 * TB-277-R1 REMOVED every code that only proves "this RETRY was refused". A later authorization denial, a missing
 * actor, a permission loss or a generic conflict/state answer says nothing about an EARLIER send of the same frozen
 * bytes: that send may have been authorized and may have written, so the retained envelope must stay unresolved. The
 * remaining tokens are refusals determined SOLELY by the frozen reviewed body (or by an immutable fact that could never
 * have been true when the body was sent), so they can never describe a command that wrote and then answered this way.
 *
 * Deliberately NOT listed:
 * - a transport failure carries no code at all;
 * - `return.rejected` is the client fallback for a payload without a code;
 * - `return.recovery.gateway_unavailable`: the pending attempt is retained and must be resumed by the SAME intent;
 * - `admin.authorization.denied`, `admin.actor.missing`, `order.operation.denied`, `validation.failed` at the
 *   authorization boundary, and every conflict/state code (`return.stale`, `return.already_*`,
 *   `return.idempotency_conflict`, `return.decision.reason_conflict`, `return.receipt.conflict`,
 *   `return.receipt.state_invalid`, `return.recovery.*`) — they describe THIS attempt, not an earlier one.
 */
export const PROVEN_NO_EFFECT_REFUSAL_CODES: ReadonlySet<string> = new Set([
  // The reviewed BODY itself cannot describe a lawful quantity of goods; no earlier send of these bytes could write.
  "return.quantity_invalid",
  "return.quantity_precision",
  "return.duplicate_line",
  "return.idempotency_required",
  "return.rejection_reason_required",
  "return.receipt.occurrence_required",
  "return.receipt.line_invalid",
  "return.receipt.unit_quantity_invalid",
  // The line it names cannot belong to this case (an immutable identity), so nothing could ever have been stored.
  "return.line_missing",
  "return.receipt.origin_invalid",
]);

/** True when a typed code proves the reviewed command did not write anything. */
export function isProvenNoEffectRefusal(code: string | null | undefined): boolean {
  const token = coalesce(code);
  return token.length > 0 && PROVEN_NO_EFFECT_REFUSAL_CODES.has(token);
}

/** An in-memory storage for tests and for the non-browser render path. */
export function createMemoryIntentStorage(seed?: Record<string, string>): IntentStorage {
  const values = new Map<string, string>(Object.entries(seed ?? {}));
  return {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => {
      values.set(key, value);
    },
    removeItem: (key) => {
      values.delete(key);
    },
  };
}
