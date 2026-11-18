/**
 * TB-278 — the customer's ONE durable multi-seller return submission.
 *
 * A customer may lawfully return SEVERAL lines that belong to DIFFERENT seller orders (and lines that need different
 * reasons) in one deliberate act. The owning Returns directory still creates ONE case per seller order, so this module
 * freezes that act as a SET of child submissions — each child is one `(sellerOrderId, orderLineId, reason)` with its own
 * stable idempotency key and its own EXACT request bytes — and writes the whole set to storage BEFORE the first send.
 *
 * Rules this module owns (the surface must not re-invent them):
 *  - the bytes of a child are produced once and replayed verbatim, so a duplicate click, a lost response, a reload or a
 *    second device attempt resolves to the SAME stored case instead of minting a new one;
 *  - a child that came back with a typed refusal proves no effect ONLY when the refusal is dictated by the immutable
 *    body/identity it sent; every other refusal (authorization, state, window, capacity, conflict, expired) leaves the
 *    child `unknown`, because an earlier send of those same bytes may already have written;
 *  - a recorded child is terminal and never re-sent, so a partially successful act keeps its successes and retries only
 *    the unresolved children;
 *  - nothing is cleared implicitly: only the exact current submission key can be cleared, and only when every child is
 *    resolved.
 */
import {
  isAlignedToUnit,
  unitsToExactText,
  unitsToWireNumber,
} from "./return-quantity.ts";

export const RETURN_SUBMISSION_VERSION = 1;
export const RETURN_SUBMISSION_STORAGE_PREFIX = "tb278:customer-return-submission:";

export type ReturnSubmissionChildState = "pending" | "unknown" | "recorded" | "refused";

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
  /**
   * TB-279: the customer this act belongs to. `undefined` means the surface did not know the viewer, so the act keeps the
   * legacy shared key: it is never a guessed identity, and another viewer never silently adopts it.
   */
  viewerId?: string | null;
  destination: string;
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
  | { kind: "unreadable" }
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

/**
 * TB-279: the durable act is scoped to the VIEWING customer, not only to the checkout.
 * <para>
 * A shared browser session can be used by more than one customer; a viewer-scoped key means one customer can never read,
 * retry or clear another customer's possibly-sent act. When the viewer is unknown the act keeps the legacy shared key
 * (never a guessed identity), and the surface still refuses to send on unreadable evidence.
 * </para>
 */
export function submissionStorageKey(checkoutId: string, viewerId?: string | null): string {
  const viewer = viewerId?.trim() ?? "";
  return viewer === ""
    ? `${RETURN_SUBMISSION_STORAGE_PREFIX}${checkoutId}`
    : `${RETURN_SUBMISSION_STORAGE_PREFIX}${viewer}:${checkoutId}`;
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
  return `tb278-${Date.now()}-${Math.random().toString(36).slice(2, 12)}`;
}

/**
 * Freezes the customer's deliberate selection into one durable act. Every child carries its own idempotency key and its
 * exact bytes; the act itself gets one submission key so only THIS act can ever be resumed or cleared.
 */
export function buildReturnSubmission(input: {
  checkoutId: string;
  destination: string;
  selections: SubmissionSelection[];
  recordedAt?: string;
  submissionKey?: string;
  viewerId?: string | null;
  newChildKey?: () => string;
}): ReturnSubmission {
  const newKey = input.newChildKey ?? defaultSubmissionKeyFactory;
  return {
    version: RETURN_SUBMISSION_VERSION,
    submissionKey: input.submissionKey ?? newKey(),
    checkoutId: input.checkoutId,
    viewerId: input.viewerId ?? null,
    destination: input.destination,
    recordedAt: input.recordedAt ?? new Date().toISOString(),
    children: input.selections.map((selection) => {
      const reason = selection.reason.trim();
      const idempotencyKey = `tb278-${newKey()}`;
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

function parseChild(value: unknown): ReturnSubmissionChild | null {
  if (!value || typeof value !== "object") return null;
  const child = value as Record<string, unknown>;
  if (typeof child.key !== "string" || child.key.length === 0) return null;
  if (typeof child.sellerOrderId !== "string" || child.sellerOrderId.length === 0) return null;
  if (typeof child.orderLineId !== "string" || child.orderLineId.length === 0) return null;
  if (typeof child.idempotencyKey !== "string" || child.idempotencyKey.length === 0) return null;
  if (typeof child.body !== "string" || child.body.length === 0) return null;
  if (typeof child.quantityUnits !== "number" || !Number.isInteger(child.quantityUnits)) return null;
  if (!isChildState(child.state)) return null;
  return {
    key: child.key,
    sellerOrderId: child.sellerOrderId,
    orderLineId: child.orderLineId,
    productLabel: typeof child.productLabel === "string" ? child.productLabel : null,
    unitLabel: typeof child.unitLabel === "string" ? child.unitLabel : null,
    unitDecimalPlaces: typeof child.unitDecimalPlaces === "number" ? child.unitDecimalPlaces : null,
    quantityUnits: child.quantityUnits,
    quantityText: typeof child.quantityText === "string" ? child.quantityText : unitsToExactText(child.quantityUnits),
    reason: typeof child.reason === "string" ? child.reason : "",
    idempotencyKey: child.idempotencyKey,
    body: child.body,
    state: child.state,
    caseId: typeof child.caseId === "string" ? child.caseId : null,
    caseNumber: typeof child.caseNumber === "string" ? child.caseNumber : null,
    refusalCode: typeof child.refusalCode === "string" ? child.refusalCode : null,
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
  if (!Array.isArray(record.children) || record.children.length === 0) return null;
  const children = record.children.map(parseChild);
  if (children.some((child) => child === null)) return null;
  return {
    version: RETURN_SUBMISSION_VERSION,
    submissionKey: record.submissionKey,
    checkoutId: record.checkoutId,
    // TB-279: an act recorded without a viewer keeps `null` (legacy shared key) and is never adopted by a named viewer.
    viewerId: typeof record.viewerId === "string" && record.viewerId.length > 0 ? record.viewerId : null,
    destination: typeof record.destination === "string" ? record.destination : "OriginalPayment",
    recordedAt: typeof record.recordedAt === "string" ? record.recordedAt : "",
    children: children as ReturnSubmissionChild[],
  };
}

/** Reads this checkout's frozen act. `unreadable` is an explicit state, never silently "nothing pending". */
export function readReturnSubmission( storage: SubmissionStorage | null, checkoutId: string, viewerId?: string | null, ): SubmissionRead {
  if (!storage) return { kind: "unavailable" };
  let raw: string | null;
  try {
    raw = storage.getItem(submissionStorageKey(checkoutId, viewerId));
  } catch {
    return { kind: "unavailable" };
  }
  if (!raw) return { kind: "none" };
  const submission = parseReturnSubmission(raw);
  if (!submission) return { kind: "unreadable" };
  if (submission.checkoutId !== checkoutId) return { kind: "unreadable" };
  // TB-279: an act scoped to another viewer is never adopted by this one; it stays untouched evidence for that viewer.
  const scopedViewer = submission.viewerId ?? null;
  const requestedViewer = viewerId?.trim() ?? "";
  if (requestedViewer !== "" && scopedViewer !== null && scopedViewer !== requestedViewer) {
    return { kind: "none" };
  }
  return { kind: "found", submission };
}

/** Writes AND reads the act back; a failed set/read/compare means the surface must not send. */
export function storeReturnSubmission(
  storage: SubmissionStorage | null,
  submission: ReturnSubmission,
  viewerId?: string | null,
): boolean {
  if (!storage) return false;
  const value = serializeReturnSubmission(submission);
  // TB-279: the act's OWN viewer scope is authoritative, so a child-outcome write can never drift into another key.
  const scope = viewerId ?? submission.viewerId ?? null;
  try {
    storage.setItem(submissionStorageKey(submission.checkoutId, scope), value);
    return storage.getItem(submissionStorageKey(submission.checkoutId, scope)) === value;
  } catch {
    return false;
  }
}

/**
 * Applies ONE child's answer to the CURRENT act only.
 *
 * - `recorded` is terminal (a case exists — never re-sent),
 * - `refused` only when the typed code proves the body/identity was rejected (never possibly written),
 * - everything else is `unknown` and keeps its exact bytes for the next attempt.
 *
 * Returns the updated act, or `null` when storage is unavailable or the stored act is not this submission key.
 */
export function recordChildOutcome(
  storage: SubmissionStorage | null,
  submissionKey: string,
  childKey: string,
  outcome:
    | { kind: "recorded"; caseId: string | null; caseNumber: string | null }
    | { kind: "refused"; refusalCode: string }
    | { kind: "unknown"; refusalCode: string | null },
): ReturnSubmission | null {
  const current = readReturnSubmissionByKey(storage, submissionKey);
  if (!current) return null;
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

function readReturnSubmissionByKey(
  storage: SubmissionStorage | null,
  submissionKey: string,
): ReturnSubmission | null {
  if (!storage) return null;
  try {
    for (let index = 0; index < storage.length; index += 1) {
      const key = storage.key(index);
      if (!key || !key.startsWith(RETURN_SUBMISSION_STORAGE_PREFIX)) continue;
      const submission = parseReturnSubmission(storage.getItem(key));
      if (submission && submission.submissionKey === submissionKey) return submission;
    }
  } catch {
    return null;
  }
  return null;
}

/** Clears only the EXACT act; a newer act (or another checkout's act) is never touched. */
export function clearReturnSubmissionIfCurrent(
  storage: SubmissionStorage | null,
  submission: ReturnSubmission,
  viewerId?: string | null,
): boolean {
  if (!storage) return false;
  const key = submissionStorageKey(submission.checkoutId, viewerId ?? submission.viewerId ?? null);
  try {
    const raw = storage.getItem(key);
    if (raw === null) return true;
    const stored = parseReturnSubmission(raw);
    if (!stored || stored.submissionKey !== submission.submissionKey) return false;
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
