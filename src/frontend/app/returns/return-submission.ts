/**
 * TB-278/TB-279-R1 — the customer's ONE durable multi-seller return submission.
 *
 * A customer may lawfully return SEVERAL lines that belong to DIFFERENT seller orders (and lines that need different
 * reasons) in one deliberate act. The owning Returns directory still creates ONE case per seller order, so this module
 * freezes that act as a SET of child submissions — each child is one `(sellerOrderId, orderLineId, reason)` with its own
 * stable idempotency key and its own EXACT request bytes — and writes the whole set to storage BEFORE the first send.
 *
 * Rules this module owns (the surface must not re-invent them):
 *  - the act belongs to ONE authenticated viewer: the key is `viewer + checkout`, so a shared browser session can never
 *    read, retry or clear another customer's possibly-sent act, and an act recorded without a viewer is unreadable;
 *  - a stored envelope is only trusted when its OWN bytes verify: every child's exact body is re-derived from its
 *    identity (seller order + line + reason + quantity + key + destination) and compared, so tampered evidence can never
 *    be replayed;
 *  - the bytes of a child are produced once and replayed verbatim, so a duplicate click, a lost response, a reload or a
 *    second device attempt resolves to the SAME stored case instead of minting a new one;
 *  - a child that came back with a typed refusal proves no effect ONLY when the refusal is dictated by the immutable
 *    body/identity it sent; every other refusal (authorization, state, window, capacity, conflict, expired) leaves the
 *    child `unknown`, because an earlier send of those same bytes may already have written;
 *  - a recorded child is terminal and never re-sent, so a partially successful act keeps its successes and retries only
 *    the unresolved children;
 *  - nothing is cleared implicitly: only the EXACT current act can be cleared (full envelope compare, not just a key),
 *    and only when every child is resolved.
 */
import {
  isAlignedToUnit,
  unitsToExactText,
  unitsToWireNumber,
} from "./return-quantity.ts";

export const RETURN_SUBMISSION_VERSION = 2;
export const RETURN_SUBMISSION_STORAGE_PREFIX = "tb279:customer-return-submission:";
/**
 * TB-279-R2 (F): the PREVIOUS-version prefix. A v1 act carries NO authenticated viewer, so it can never be attributed to
 * the customer in front of the surface; its bytes are preserved untouched and treated as unresolved evidence.
 */
export const RETURN_SUBMISSION_LEGACY_PREFIX = "tb278:customer-return-submission:";

export type ReturnSubmissionChildState = "pending" | "unknown" | "recorded" | "refused";

export type RefundDestinationToken = "OriginalPayment" | "Wallet";

/** One frozen child: one case the customer deliberately asked for. */
export interface ReturnSubmissionChild {
  /** Stable identity of this child inside the act: seller order + line + reason token. */
  key: string;
  sellerOrderId: string;
  orderLineId: string;
  productLabel: string | null;
  unitLabel: string | null;
  unitDecimalPlaces: number | null;
  /** Exact `numeric(18,6)` millionths the customer entered. */
  quantityUnits: number;
  /** Exact text as the customer entered it (audit + display). */
  quantityText: string;
  reason: string;
  idempotencyKey: string;
  /** The EXACT request bytes, produced once; replayed verbatim on every retry. */
  body: string;
  state: ReturnSubmissionChildState;
  caseId: string | null;
  caseNumber: string | null;
  refusalCode: string | null;
}

export interface ReturnSubmission {
  version: number;
  submissionKey: string;
  checkoutId: string;
  /** The authenticated customer this act belongs to — never optional, never guessed. */
  viewerId: string;
  destination: RefundDestinationToken;
  recordedAt: string;
  children: ReturnSubmissionChild[];
}

export interface SubmissionSelection {
  sellerOrderId: string;
  orderLineId: string;
  productLabel: string | null;
  unitLabel: string | null;
  unitDecimalPlaces: number | null;
  quantityUnits: number;
  quantityText: string;
  reason: string;
}

/** The exact scope an act lives in: one viewer's act for one checkout. */
export interface SubmissionScope {
  checkoutId: string;
  viewerId: string;
}

/** The minimal storage shape (browser `sessionStorage` or a test double). */
export interface SubmissionStorage {
  readonly length: number;
  key(index: number): string | null;
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

export type SubmissionRead =
  | { kind: "none" }
  | { kind: "unavailable" }
  | { kind: "viewer_required" }
  | { kind: "unreadable" }
  /** TB-279-R2: an older, viewer-less act exists for this checkout. It is preserved and can never be adopted/cleared. */
  | { kind: "legacy_unresolved" }
  | { kind: "found"; submission: ReturnSubmission };

/**
 * Refusal codes that are dictated by the immutable body/identity the request carried, so they can never coexist with a
 * write of those exact bytes. Everything else stays unresolved — a later refusal does not prove an earlier send did not
 * write.
 */
export const SUBMISSION_NO_EFFECT_REFUSALS: ReadonlySet<string> = new Set([
  "return.quantity_invalid",
  "return.quantity_precision",
  "return.quantity_unit_invalid",
  "return.duplicate_line",
  "return.idempotency_required",
  "return.line_missing",
]);

function isDestination(value: unknown): value is RefundDestinationToken {
  return value === "OriginalPayment" || value === "Wallet";
}

/** TB-279-R1: the key is viewer-scoped; an empty viewer has NO key at all (no shared fallback). */
export function submissionStorageKey(checkoutId: string, viewerId: string): string {
  const viewer = viewerId.trim();
  return `${RETURN_SUBMISSION_STORAGE_PREFIX}${viewer}:${checkoutId}`;
}

/** The stable identity of one child: the owning directory's own case grain. */
export function submissionChildKey(sellerOrderId: string, orderLineId: string, reason: string): string {
  return `${sellerOrderId}|${orderLineId}|${reason.trim().toLowerCase()}`;
}

/**
 * The EXACT customer return body of one child. Key order is fixed so the same child always serializes to the same
 * bytes; nothing volatile (time, random, labels) is included.
 */
export function buildChildBody(child: {
  sellerOrderId: string;
  idempotencyKey: string;
  reason: string;
  destination: string;
  orderLineId: string;
  quantityUnits: number;
}): string {
  return JSON.stringify({
    sellerOrderId: child.sellerOrderId,
    idempotencyKey: child.idempotencyKey,
    reason: child.reason,
    destination: child.destination,
    refundDestination: child.destination,
    items: [
      {
        orderLineId: child.orderLineId,
        quantity: unitsToWireNumber(child.quantityUnits),
      },
    ],
  });
}

export function defaultSubmissionKeyFactory(): string {
  const uuid = globalThis.crypto?.randomUUID;
  if (typeof uuid === "function") return uuid.call(globalThis.crypto);
  return `tb279-${Date.now()}-${Math.random().toString(36).slice(2, 12)}`;
}

function trimOrNull(value: unknown): string | null {
  return typeof value === "string" && value.trim() !== "" ? value.trim() : null;
}

/**
 * Freezes the customer's deliberate selection into one durable act. Every child carries its own idempotency key and its
 * exact bytes; the act itself gets one submission key so only THIS act can ever be resumed or cleared.
 * Returns `null` when the act has no authenticated viewer or no lawful child — the surface must not send then.
 */
export function buildReturnSubmission(input: {
  checkoutId: string;
  destination: RefundDestinationToken;
  selections: SubmissionSelection[];
  viewerId: string | null | undefined;
  recordedAt?: string;
  submissionKey?: string;
  newChildKey?: () => string;
}): ReturnSubmission | null {
  const viewerId = trimOrNull(input.viewerId);
  if (!viewerId || !input.checkoutId || input.selections.length === 0) return null;
  const newKey = input.newChildKey ?? defaultSubmissionKeyFactory;
  return {
    version: RETURN_SUBMISSION_VERSION,
    submissionKey: input.submissionKey ?? newKey(),
    checkoutId: input.checkoutId,
    viewerId,
    destination: input.destination,
    recordedAt: input.recordedAt ?? new Date().toISOString(),
    children: input.selections.map((selection) => {
      const reason = selection.reason.trim();
      const idempotencyKey = `tb279-${newKey()}`;
      return {
        key: submissionChildKey(selection.sellerOrderId, selection.orderLineId, reason),
        sellerOrderId: selection.sellerOrderId,
        orderLineId: selection.orderLineId,
        productLabel: selection.productLabel,
        unitLabel: selection.unitLabel,
        unitDecimalPlaces: selection.unitDecimalPlaces,
        quantityUnits: selection.quantityUnits,
        quantityText: selection.quantityText,
        reason,
        idempotencyKey,
        body: buildChildBody({
          sellerOrderId: selection.sellerOrderId,
          idempotencyKey,
          reason,
          destination: input.destination,
          orderLineId: selection.orderLineId,
          quantityUnits: selection.quantityUnits,
        }),
        state: "pending",
        caseId: null,
        caseNumber: null,
        refusalCode: null,
      } satisfies ReturnSubmissionChild;
    }),
  };
}

export function serializeReturnSubmission(submission: ReturnSubmission): string {
  return JSON.stringify(submission);
}

function isChildState(value: unknown): value is ReturnSubmissionChildState {
  return value === "pending" || value === "unknown" || value === "recorded" || value === "refused";
}

/**
 * Parses one stored child and RE-DERIVES its exact body from its own identity. A body that does not match (tampered
 * quantity, foreign line, different destination, mismatched child key) is refused as unreadable evidence, never replayed.
 */
function parseChild(value: unknown, destination: RefundDestinationToken): ReturnSubmissionChild | null {
  if (!value || typeof value !== "object") return null;
  const child = value as Record<string, unknown>;
  if (typeof child.key !== "string" || child.key.length === 0) return null;
  if (typeof child.sellerOrderId !== "string" || child.sellerOrderId.length === 0) return null;
  if (typeof child.orderLineId !== "string" || child.orderLineId.length === 0) return null;
  if (typeof child.idempotencyKey !== "string" || child.idempotencyKey.length === 0) return null;
  if (typeof child.body !== "string" || child.body.length === 0) return null;
  if (typeof child.quantityUnits !== "number" || !Number.isInteger(child.quantityUnits) || child.quantityUnits <= 0) {
    return null;
  }
  if (!isChildState(child.state)) return null;
  const reason = typeof child.reason === "string" ? child.reason : "";
  if (reason.trim() === "") return null;
  // The child key is the owning directory's grain: seller order + line + reason.
  if (child.key !== submissionChildKey(child.sellerOrderId, child.orderLineId, reason)) return null;
  // The stored bytes must be EXACTLY the bytes this identity produces.
  const expectedBody = buildChildBody({
    sellerOrderId: child.sellerOrderId,
    idempotencyKey: child.idempotencyKey,
    reason,
    destination,
    orderLineId: child.orderLineId,
    quantityUnits: child.quantityUnits,
  });
  if (child.body !== expectedBody) return null;
  const caseId = trimOrNull(child.caseId);
  const refusalCode = trimOrNull(child.refusalCode);
  if (child.state === "recorded" && caseId === null) return null;
  if (child.state === "refused" && (refusalCode === null || !SUBMISSION_NO_EFFECT_REFUSALS.has(refusalCode))) {
    return null;
  }
  if (child.state !== "refused" && refusalCode !== null && SUBMISSION_NO_EFFECT_REFUSALS.has(refusalCode)) {
    return null;
  }
  return {
    key: child.key,
    sellerOrderId: child.sellerOrderId,
    orderLineId: child.orderLineId,
    productLabel: typeof child.productLabel === "string" ? child.productLabel : null,
    unitLabel: typeof child.unitLabel === "string" ? child.unitLabel : null,
    unitDecimalPlaces: typeof child.unitDecimalPlaces === "number" ? child.unitDecimalPlaces : null,
    quantityUnits: child.quantityUnits,
    quantityText: typeof child.quantityText === "string" ? child.quantityText : unitsToExactText(child.quantityUnits),
    reason,
    idempotencyKey: child.idempotencyKey,
    body: child.body,
    state: child.state,
    caseId,
    caseNumber: trimOrNull(child.caseNumber),
    refusalCode,
  };
}

export function parseReturnSubmission(raw: string | null): ReturnSubmission | null {
  if (!raw) return null;
  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }
  if (!parsed || typeof parsed !== "object") return null;
  const record = parsed as Record<string, unknown>;
  if (record.version !== RETURN_SUBMISSION_VERSION) return null;
  if (typeof record.submissionKey !== "string" || record.submissionKey.length === 0) return null;
  if (typeof record.checkoutId !== "string" || record.checkoutId.length === 0) return null;
  const viewerId = trimOrNull(record.viewerId);
  // TB-279-R1: an act with no authenticated viewer can never be adopted or replayed.
  if (viewerId === null) return null;
  if (!isDestination(record.destination)) return null;
  if (!Array.isArray(record.children) || record.children.length === 0) return null;
  const children = record.children.map((child) => parseChild(child, record.destination as RefundDestinationToken));
  if (children.some((child) => child === null)) return null;
  const typed = children as ReturnSubmissionChild[];
  if (new Set(typed.map((child) => child.key)).size !== typed.length) return null;
  return {
    version: RETURN_SUBMISSION_VERSION,
    submissionKey: record.submissionKey,
    checkoutId: record.checkoutId,
    viewerId,
    destination: record.destination as RefundDestinationToken,
    recordedAt: typeof record.recordedAt === "string" ? record.recordedAt : "",
    children: typed,
  };
}

/**
 * TB-279-R2 (F): finds every PREVIOUS-version act of this checkout. A v1 envelope has no viewer, so it cannot be
 * attributed to the current customer and must never be adopted, compared or cleared — only preserved and reported.
 * Both v1 key shapes are matched: `${prefix}${checkoutId}` and `${prefix}${viewer}:${checkoutId}`.
 */
export function findLegacySubmissionKeys(storage: SubmissionStorage, checkoutId: string): string[] {
  const keys: string[] = [];
  const unscoped = `${RETURN_SUBMISSION_LEGACY_PREFIX}${checkoutId}`;
  const suffix = `:${checkoutId}`;
  for (let index = 0; index < storage.length; index += 1) {
    const key = storage.key(index);
    if (!key || !key.startsWith(RETURN_SUBMISSION_LEGACY_PREFIX)) continue;
    if (key === unscoped || key.endsWith(suffix)) keys.push(key);
  }
  return keys;
}

/** Reads THIS viewer's frozen act for THIS checkout. `unreadable`/`viewer_required` are explicit states, never "none". */
export function readReturnSubmission(
  storage: SubmissionStorage | null,
  checkoutId: string,
  viewerId?: string | null,
): SubmissionRead {
  if (!storage) return { kind: "unavailable" };
  const viewer = trimOrNull(viewerId);
  if (viewer === null) return { kind: "viewer_required" };
  let raw: string | null;
  try {
    raw = storage.getItem(submissionStorageKey(checkoutId, viewer));
  } catch {
    return { kind: "unavailable" };
  }
  if (!raw) {
    // TB-279-R2 (F): a legacy act for this checkout is unresolved evidence, not "nothing here".
    try {
      if (findLegacySubmissionKeys(storage, checkoutId).length > 0) return { kind: "legacy_unresolved" };
    } catch {
      return { kind: "unavailable" };
    }
    return { kind: "none" };
  }
  const submission = parseReturnSubmission(raw);
  if (!submission) return { kind: "unreadable" };
  // The envelope must belong to EXACTLY this scope; anything else is foreign or tampered evidence.
  if (submission.checkoutId !== checkoutId || submission.viewerId !== viewer) return { kind: "unreadable" };
  return { kind: "found", submission };
}

/** Writes AND reads the act back; a failed set/read/compare means the surface must not send. */
export function storeReturnSubmission(storage: SubmissionStorage | null, submission: ReturnSubmission): boolean {
  if (!storage) return false;
  if (trimOrNull(submission.viewerId) === null) return false;
  const value = serializeReturnSubmission(submission);
  const key = submissionStorageKey(submission.checkoutId, submission.viewerId);
  try {
    storage.setItem(key, value);
    return storage.getItem(key) === value;
  } catch {
    return false;
  }
}

/**
 * Applies ONE child's answer to the CURRENT exact act only.
 *
 * - `recorded` is terminal (a case exists — never re-sent),
 * - `refused` only when the typed code proves the body/identity was rejected (never possibly written),
 * - everything else is `unknown` and keeps its exact bytes for the next attempt.
 *
 * Returns the updated act, or `null` when storage is unavailable, the scope is unknown, or the stored act is not this
 * submission key (a newer act is never touched by a late answer).
 *
 * TB-279-R2 (F): when the caller passes `expected` it must match the STORED envelope byte-for-byte and the child it is
 * answering must still carry the exact frozen body/idempotency key. A late answer for an act that was replaced,
 * re-scoped or tampered with is dropped instead of being applied to different bytes.
 */
export function recordChildOutcome(
  storage: SubmissionStorage | null,
  scope: SubmissionScope,
  submissionKey: string,
  childKey: string,
  outcome:
    | { kind: "recorded"; caseId: string; caseNumber: string | null }
    | { kind: "refused"; refusalCode: string }
    | { kind: "unknown"; refusalCode: string | null },
  expected?: { envelope: string; childBody: string; idempotencyKey: string },
): ReturnSubmission | null {
  const read = readReturnSubmission(storage, scope.checkoutId, scope.viewerId);
  if (read.kind !== "found") return null;
  const current = read.submission;
  if (current.submissionKey !== submissionKey) return null;
  const target = current.children.find((child) => child.key === childKey);
  if (!target) return null;
  if (expected) {
    if (serializeReturnSubmission(current) !== expected.envelope) return null;
    if (target.body !== expected.childBody) return null;
    if (target.idempotencyKey !== expected.idempotencyKey) return null;
  }
  const children = current.children.map((child) => {
    if (child.key !== childKey) return child;
    if (child.state === "recorded") return child;
    if (outcome.kind === "recorded") {
      return { ...child, state: "recorded" as const, caseId: outcome.caseId, caseNumber: outcome.caseNumber, refusalCode: null };
    }
    if (outcome.kind === "refused") {
      return { ...child, state: "refused" as const, refusalCode: outcome.refusalCode };
    }
    return { ...child, state: "unknown" as const, refusalCode: outcome.refusalCode };
  });
  const updated: ReturnSubmission = { ...current, children };
  return storeReturnSubmission(storage, updated) ? updated : null;
}

/**
 * Clears only the EXACT act: the stored envelope must be byte-identical to the one whose every child is resolved. A
 * newer act, another checkout's act, another viewer's act or a tampered envelope is never removed.
 */
export function clearReturnSubmissionIfCurrent(
  storage: SubmissionStorage | null,
  submission: ReturnSubmission,
  viewerId?: string | null,
): boolean {
  if (!storage) return false;
  const viewer = trimOrNull(viewerId);
  if (viewer === null || viewer !== submission.viewerId) return false;
  const key = submissionStorageKey(submission.checkoutId, submission.viewerId);
  try {
    const raw = storage.getItem(key);
    if (raw === null) return true;
    if (raw !== serializeReturnSubmission(submission)) return false;
    storage.removeItem(key);
    return storage.getItem(key) === null;
  } catch {
    return false;
  }
}

export function unresolvedChildren(submission: ReturnSubmission): ReturnSubmissionChild[] {
  return submission.children.filter((child) => child.state === "pending" || child.state === "unknown");
}

export function recordedChildren(submission: ReturnSubmission): ReturnSubmissionChild[] {
  return submission.children.filter((child) => child.state === "recorded");
}

/**
 * TB-278: does this checkout carry an act that still needs the customer? `unreadable` counts as needing attention —
 * a surface must never read a broken envelope as "nothing pending".
 */
export function hasUnresolvedCustomerReturnSubmission(
  storage: SubmissionStorage | null,
  checkoutId: string,
  viewerId?: string | null,
): boolean {
  const read = readReturnSubmission(storage, checkoutId, viewerId);
  if (read.kind === "unreadable") return true;
  // TB-279-R2 (F): a preserved older act is unresolved until a human reconciles it.
  if (read.kind === "legacy_unresolved") return true;
  if (read.kind !== "found") return false;
  return !summarizeReturnSubmission(read.submission).resolved;
}

export interface SubmissionSummary {
  total: number;
  recorded: number;
  unknown: number;
  pending: number;
  refused: number;
  resolved: boolean;
  hasPossiblyWritten: boolean;
}

export function summarizeReturnSubmission(submission: ReturnSubmission): SubmissionSummary {
  const summary: SubmissionSummary = {
    total: submission.children.length,
    recorded: 0,
    unknown: 0,
    pending: 0,
    refused: 0,
    resolved: false,
    hasPossiblyWritten: false,
  };
  for (const child of submission.children) {
    if (child.state === "recorded") summary.recorded += 1;
    else if (child.state === "unknown") summary.unknown += 1;
    else if (child.state === "refused") summary.refused += 1;
    else summary.pending += 1;
  }
  summary.resolved = summary.unknown === 0 && summary.pending === 0;
  summary.hasPossiblyWritten = summary.unknown > 0;
  return summary;
}

/** True when this child may NOT be attempted again (it either exists or the act moved on). */
export function isChildSettled(child: ReturnSubmissionChild): boolean {
  return child.state === "recorded" || child.state === "refused";
}

/**
 * Splits the customer's selection into the owning directory's case grain, refusing invalid entries locally before any
 * request: duplicate `(seller order, line, reason)`, non-positive/over-precise quantities and quantities that cannot be
 * expressed in the line's OWN unit are refused with the same typed wording the server uses.
 */
export function validateSubmissionSelection(
  selection: SubmissionSelection,
  remainingUnits: number,
): { ok: true } | { ok: false; code: string } {
  if (!selection.orderLineId) return { ok: false, code: "return.line_missing" };
  if (!Number.isInteger(selection.quantityUnits) || selection.quantityUnits <= 0) {
    return { ok: false, code: "return.quantity_invalid" };
  }
  if (selection.quantityUnits > remainingUnits) return { ok: false, code: "return.quantity_exceeded" };
  if (selection.unitDecimalPlaces !== null && !isAlignedToUnit(selection.quantityUnits, selection.unitDecimalPlaces)) {
    return { ok: false, code: "return.quantity_unit_invalid" };
  }
  if (selection.unitDecimalPlaces === null && selection.quantityUnits % 1_000_000 !== 0) {
    return { ok: false, code: "return.quantity_unit_invalid" };
  }
  return { ok: true };
}
