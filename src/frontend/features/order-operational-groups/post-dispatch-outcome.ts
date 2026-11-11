/**
 * TB-263 — the ONE semantic classification of a submitted post-dispatch review.
 *
 * The screenshot defect was semantic, not cosmetic: every `false` submit result was treated as an
 * *unknown* response, so a server-acknowledged durable decision (no healthy stock yet) and an exact
 * localized refusal were both presented as "the response was lost", while the durable envelope stayed
 * stamped as an uncertain attempt. The three outcomes below are different facts and must stay different:
 *
 *  - `completed`  the server authoritatively finished it: close the review, one success toast, refresh.
 *  - `pending`    the server DURABLY recorded the decision but cannot finish now (e.g. no healthy stock).
 *                 The same decision continues later; it is never a lost response and never re-minted.
 *  - `refused`    the server answered deterministically with a reason. `blocking` refusals (authority,
 *                 ownership, conflict with recorded state) end this review and need acknowledgement;
 *                 non-blocking ones are corrected inline in the same review.
 *  - `unknown`    transport loss / 5xx / no answer at all: only this case is genuinely uncertain, so only
 *                 this case keeps the "same request only" uncertainty treatment.
 *
 * Pure and transport-free: the app layer maps its own result shape into `PostDispatchResultFacts`.
 */

export type PostDispatchSubmitOutcomeKind = "completed" | "pending" | "refused" | "unknown";

export type PostDispatchSubmitOutcome = {
  kind: PostDispatchSubmitOutcomeKind;
  /** Already localized operator copy. Never a raw code, enum or technical diagnostic. */
  messageFa: string;
  /** Stable server code when the server answered deterministically. */
  code: string | null;
  /** A refusal that ends this review (authority/ownership/state conflict) instead of one fixed inline. */
  blocking: boolean;
};

/** What the app layer knows about one submit attempt, independent of any HTTP client type. */
export type PostDispatchResultFacts = {
  ok: boolean;
  denied: boolean;
  status: number;
  code: string | null;
  /** Localized operator copy produced by the existing admin error mapper. */
  messageFa: string;
};

export const POST_DISPATCH_OUTCOME_MESSAGE_FA: Record<PostDispatchSubmitOutcomeKind, string> = {
  completed: "انجام شد.",
  pending: "تصمیم ثبت شد ولی هنوز کامل نشده است.",
  refused: "این درخواست پذیرفته نشد.",
  unknown: "نتیجهٔ این درخواست مشخص نشد.",
};

/**
 * The decision is durably recorded but its replacement cannot be created yet. This is a KNOWN pending
 * business state, never an unknown response: the recorded decision, source, quantity and occurrence stay
 * exactly as reviewed and continue when healthy stock arrives.
 */
export function isPendingPostDispatchCode(code: string | null | undefined): boolean {
  return code === "fulfillment.damage.replacement_unavailable";
}

/**
 * Refusals keep the SAME review open with one exact inline reason, so nothing is abandoned by an error
 * code. A review is closed and acknowledged in the blocking MessageBox only when it genuinely cannot
 * continue: the actor is denied, or the fresh projection no longer offers the action at all (the panel
 * marks that case `blocking`).
 */
export function completedPostDispatchOutcome(): PostDispatchSubmitOutcome {
  return { kind: "completed", messageFa: POST_DISPATCH_OUTCOME_MESSAGE_FA.completed, code: null, blocking: false };
}

export function unknownPostDispatchOutcome(messageFa?: string | null): PostDispatchSubmitOutcome {
  return {
    kind: "unknown",
    messageFa: messageFa?.trim() || POST_DISPATCH_OUTCOME_MESSAGE_FA.unknown,
    code: null,
    blocking: false,
  };
}

/**
 * Classify one server answer. Only a genuine transport loss (no status), a 5xx, or an unreadable answer is
 * uncertain; a 4xx with a stable code is a deterministic server fact.
 */
export function classifyPostDispatchResult(facts: PostDispatchResultFacts): PostDispatchSubmitOutcome {
  if (facts.ok) return completedPostDispatchOutcome();

  const code = facts.code?.trim() || null;
  const messageFa = facts.messageFa?.trim() || POST_DISPATCH_OUTCOME_MESSAGE_FA.unknown;

  if (facts.denied) {
    return { kind: "refused", messageFa, code, blocking: true };
  }
  // No status at all means the request never got a server answer; a 5xx means the outcome may or may not
  // have been committed. Both are genuinely uncertain and must keep the complete durable envelope.
  if (facts.status <= 0 || facts.status >= 500) {
    return { kind: "unknown", messageFa, code, blocking: false };
  }
  if (code && isPendingPostDispatchCode(code)) {
    return { kind: "pending", messageFa, code, blocking: false };
  }
  // Any other deterministic 4xx is stated once inside the SAME open review: the exact reason, the exact
  // reviewed values and the retained envelope. It is never treated as a lost response and never abandoned.
  return { kind: "refused", messageFa, code, blocking: false };
}

/**
 * Backwards compatibility for the pre-TB-263 boolean callback shape: only `true` is authoritative
 * completion, and anything else is treated as genuinely uncertain (never as a pending decision).
 */
export function normalizePostDispatchOutcome(
  value: PostDispatchSubmitOutcome | boolean | void | null,
): PostDispatchSubmitOutcome {
  if (value === true) return completedPostDispatchOutcome();
  if (value && typeof value === "object") return value;
  return unknownPostDispatchOutcome();
}
