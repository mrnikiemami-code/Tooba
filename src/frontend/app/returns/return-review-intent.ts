/**
 * TB-275: the durable review intent of one return case.
 *
 * <para>
 * The operator's deliberate action is written as ONE envelope before the request is sent, so a double click, a lost
 * response or a reload can never produce a second physical receipt, a second refund attempt or a second decision. The
 * occurrence key is the reviewed identity of that click and is what the server replays.
 * </para>
 */

/** TB-275: the deliberate review intents of one return case. */
export type ReturnReviewAction =
  | "receipt"
  | "approve"
  | "reject"
  | "resume_refund"
  | "retry_refund"
  | "settle_physical";

/** TB-275: the reviewed intent of ONE action, durable before the request is sent. */
export interface ReturnReviewIntent {
  action: ReturnReviewAction;
  occurrenceKey: string;
  body: Record<string, unknown>;
  recordedAt: string;
}

/** The minimal storage seam, so the intent envelope is testable without a browser. */
export interface IntentStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

/** The storage key of one case's unresolved review intent. */
export function returnIntentStorageKey(returnRequestId: string): string {
  return `tb275-return-intent:${returnRequestId}`;
}

/** A new reviewed occurrence identity. */
export function createOccurrenceKey(): string {
  const cryptoApi = typeof globalThis === "object" ? globalThis.crypto : undefined;
  if (cryptoApi && typeof cryptoApi.randomUUID === "function") return cryptoApi.randomUUID();
  return `tb275-${Date.now().toString(16)}-${Math.random().toString(16).slice(2, 10)}`;
}

/** Builds the durable envelope of one reviewed action. */
export function buildReviewIntent(
  action: ReturnReviewAction,
  body: Record<string, unknown>,
  occurrenceKey: string = createOccurrenceKey(),
): ReturnReviewIntent {
  return { action, occurrenceKey, body, recordedAt: new Date().toISOString() };
}

/** Reads the stored envelope of one case (name/shape validated; anything else is not an intent). */
export function readReviewIntent(storage: IntentStorage, returnRequestId: string): ReturnReviewIntent | null {
  const raw = storage.getItem(returnIntentStorageKey(returnRequestId));
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Partial<ReturnReviewIntent> | null;
    if (!parsed || typeof parsed.occurrenceKey !== "string" || !parsed.occurrenceKey) return null;
    if (typeof parsed.action !== "string" || !parsed.action) return null;
    return {
      action: parsed.action as ReturnReviewAction,
      occurrenceKey: parsed.occurrenceKey,
      body: (parsed.body as Record<string, unknown>) ?? {},
      recordedAt: typeof parsed.recordedAt === "string" ? parsed.recordedAt : "",
    };
  } catch {
    return null;
  }
}

/** Persists the reviewed envelope BEFORE the request is sent. */
export function storeReviewIntent(
  storage: IntentStorage,
  returnRequestId: string,
  intent: ReturnReviewIntent,
): void {
  storage.setItem(returnIntentStorageKey(returnRequestId), JSON.stringify(intent));
}

/**
 * Compare-and-clear: only the exact reviewed occurrence is removed, so a late answer for an OLD intent can never erase
 * a NEWER envelope the operator just recorded.
 */
export function clearReviewIntent(
  storage: IntentStorage,
  returnRequestId: string,
  occurrenceKey: string,
): boolean {
  const stored = readReviewIntent(storage, returnRequestId);
  if (!stored || stored.occurrenceKey !== occurrenceKey) return false;
  storage.removeItem(returnIntentStorageKey(returnRequestId));
  return true;
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
