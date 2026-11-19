/**
 * Admin Story list/detail/bulk operations — one eligibility + frozen envelope builder shared by
 * row menu, detail, and AppDataGrid bulk toolbar. Server actionCodes remain the authority.
 */
import type { AdminStorySnapshot } from "../story-api";
import type { StoryIntentEnvelope } from "./story-intent";
import { newStoryOccurrenceKey as mintOccurrence } from "./story-intent";
import type { StoryExecutorResult } from "./story-executor";
import { runDurableStoryMutation, type StoryExecutorGate, type StoryExecutorScope } from "./story-executor";

export type StoryAdminOpKind = "approve" | "reject" | "delete";

export type StoryAdminOpEligibility = {
  row: AdminStorySnapshot;
  eligible: boolean;
  reason: string | null;
};

export type StoryAdminOpPlan = {
  kind: StoryAdminOpKind;
  selected: AdminStorySnapshot[];
  eligible: StoryAdminOpEligibility[];
  skipped: StoryAdminOpEligibility[];
};

export type StoryAdminOpOutcome = "succeeded" | "failed" | "unknown" | "skipped" | "not_started";

export type StoryAdminOpItemResult = {
  storyId: string;
  title: string;
  kind: StoryAdminOpKind;
  outcome: StoryAdminOpOutcome;
  message: string | null;
  envelope: StoryIntentEnvelope | null;
};

export type StoryAdminBatchResult = {
  items: StoryAdminOpItemResult[];
  succeeded: number;
  failed: number;
  unknown: number;
  skipped: number;
  notStarted: number;
  stoppedReason: string | null;
};

function hasAction(row: AdminStorySnapshot, code: string): boolean {
  return (row.actionCodes ?? []).includes(code);
}

function scopesEqual(a: StoryExecutorScope, b: StoryExecutorScope): boolean {
  return a.role === b.role && a.actorUserId === b.actorUserId && (a.shopId ?? null) === (b.shopId ?? null);
}

/** Persian skip reasons — merchant-facing, not technical jargon. */
export function skipReasonForOp(kind: StoryAdminOpKind, row: AdminStorySnapshot): string {
  if (kind === "approve") {
    if (hasAction(row, "approve")) return "";
    if (row.origin === "Admin") return "استوری ویترین اصلی از مسیر تأیید فروشنده عبور نمی‌کند.";
    if (row.reviewStatus === "Approved") return "این استوری قبلاً تأیید شده است.";
    if (row.reviewStatus !== "Submitted") return "فقط استوری‌های ارسال‌شده برای تأیید قابل تأیید هستند.";
    return "این استوری در وضعیت فعلی قابل تأیید نیست.";
  }
  if (kind === "reject") {
    if (hasAction(row, "reject")) return "";
    if (row.origin === "Admin") return "استوری ویترین اصلی رد نمی‌شود.";
    if (row.reviewStatus === "Rejected") return "این استوری قبلاً رد شده است.";
    if (row.reviewStatus !== "Submitted") return "فقط استوری‌های در انتظار تأیید قابل رد هستند.";
    return "این استوری در وضعیت فعلی قابل رد نیست.";
  }
  if (hasAction(row, "delete")) return "";
  return "حذف برای این استوری مجاز نیست.";
}

export function classifyAdminOp(
  kind: StoryAdminOpKind,
  selected: AdminStorySnapshot[],
): StoryAdminOpPlan {
  const eligible: StoryAdminOpEligibility[] = [];
  const skipped: StoryAdminOpEligibility[] = [];
  for (const row of selected) {
    const code = kind === "approve" ? "approve" : kind === "reject" ? "reject" : "delete";
    if (hasAction(row, code)) {
      eligible.push({ row, eligible: true, reason: null });
    } else {
      skipped.push({ row, eligible: false, reason: skipReasonForOp(kind, row) });
    }
  }
  return { kind, selected, eligible, skipped };
}

export function buildAdminOpEnvelope(args: {
  kind: StoryAdminOpKind;
  row: AdminStorySnapshot;
  actorUserId: string;
  shopId: string | null;
  reason?: string;
  /** Reuse frozen key on retry — never mint a new key for the same act. */
  occurrenceKey?: string;
}): StoryIntentEnvelope {
  const key = args.occurrenceKey?.trim() || mintOccurrence();
  const storyId = args.row.storyId;
  if (args.kind === "approve") {
    const body = { expectedVersion: args.row.versionToken, occurrenceKey: key };
    return {
      version: 2,
      action: "approve",
      occurrenceKey: key,
      method: "POST",
      path: `/v1/admin/stories/${storyId}/approve`,
      routeTargetId: storyId,
      expectedVersion: args.row.versionToken,
      bodyText: JSON.stringify(body),
      uploadMeta: null,
      addBodyText: null,
      uploadedAssetId: null,
      children: [],
      role: "admin",
      shopId: args.row.sellerPartyId ?? args.shopId,
      actorUserId: args.actorUserId,
      recordedAt: new Date().toISOString(),
    };
  }
  if (args.kind === "reject") {
    const reason = (args.reason ?? "").trim();
    const body = { reason, expectedVersion: args.row.versionToken, occurrenceKey: key };
    return {
      version: 2,
      action: "reject",
      occurrenceKey: key,
      method: "POST",
      path: `/v1/admin/stories/${storyId}/reject`,
      routeTargetId: storyId,
      expectedVersion: args.row.versionToken,
      bodyText: JSON.stringify(body),
      uploadMeta: null,
      addBodyText: null,
      uploadedAssetId: null,
      children: [],
      role: "admin",
      shopId: args.row.sellerPartyId ?? args.shopId,
      actorUserId: args.actorUserId,
      recordedAt: new Date().toISOString(),
    };
  }
  const body = { expectedVersion: args.row.versionToken, occurrenceKey: key };
  return {
    version: 2,
    action: "delete",
    occurrenceKey: key,
    method: "DELETE",
    path: `/v1/admin/stories/${storyId}`,
    routeTargetId: storyId,
    expectedVersion: args.row.versionToken,
    bodyText: JSON.stringify(body),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "admin",
    shopId: args.row.sellerPartyId ?? args.shopId,
    actorUserId: args.actorUserId,
    recordedAt: new Date().toISOString(),
  };
}

function isUncertainFailure(outcome: Extract<StoryExecutorResult, { state: "failed" }>): boolean {
  if (outcome.status === 0) return true;
  if (outcome.code === "story.response.unverified") return true;
  const msg = (outcome.message ?? "").toLowerCase();
  return msg.includes("unverified") || msg.includes("host-unreachable") || msg.includes("uncertain");
}

function stopReasonLabel(
  reason: "stale_epoch" | "scope" | "auth" | "cancelled" | "blocked",
): string {
  if (reason === "stale_epoch") return "جلسه یا زمینه عوض شد؛ بقیه اجرا نشد.";
  if (reason === "scope") return "عامل یا فروشگاه عوض شد؛ بقیه اجرا نشد.";
  if (reason === "auth") return "دسترسی قطع شد؛ بقیه اجرا نشد.";
  if (reason === "cancelled") return "اقدام لغو شد؛ بقیه اجرا نشد.";
  return "اجرا متوقف شد؛ بقیه اجرا نشد.";
}

/**
 * Sequential per-row execution with frozen actor/scope/epoch.
 * Revalidates live scope + gate epoch before every subsequent dispatch.
 * Untouched remaining rows are not_started (never persisted as sent).
 */
export async function executeAdminOpBatch(args: {
  plan: StoryAdminOpPlan;
  gate: StoryExecutorGate;
  scope: StoryExecutorScope;
  actorUserId: string;
  reason?: string;
  frozenByStoryId?: Map<string, StoryIntentEnvelope>;
  /** Read current actor/shop/role before each later request. */
  readLiveScope: () => StoryExecutorScope;
  isCancelled?: () => boolean;
}): Promise<StoryAdminBatchResult> {
  const frozenScope: StoryExecutorScope = {
    role: args.scope.role,
    shopId: args.scope.shopId,
    actorUserId: args.actorUserId,
  };
  const frozenEpoch = args.gate.epochRef.current;
  const kind = args.plan.kind;

  const items: StoryAdminOpItemResult[] = [];
  for (const skip of args.plan.skipped) {
    items.push({
      storyId: skip.row.storyId,
      title: skip.row.title,
      kind,
      outcome: "skipped",
      message: skip.reason,
      envelope: null,
    });
  }

  let succeeded = 0;
  let failed = 0;
  let unknown = 0;
  let notStarted = 0;
  const skipped = args.plan.skipped.length;
  let stoppedReason: string | null = null;
  let stop = false;

  const markRemainingNotStarted = (fromIndex: number, reason: string) => {
    stoppedReason = reason;
    for (let i = fromIndex; i < args.plan.eligible.length; i += 1) {
      const entry = args.plan.eligible[i]!;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        kind,
        outcome: "not_started",
        message: "اجرا نشد",
        envelope: null,
      });
      notStarted += 1;
    }
  };

  for (let index = 0; index < args.plan.eligible.length; index += 1) {
    if (stop) break;

    if (args.isCancelled?.()) {
      markRemainingNotStarted(index, stopReasonLabel("cancelled"));
      stop = true;
      break;
    }

    if (args.gate.epochRef.current !== frozenEpoch) {
      markRemainingNotStarted(index, stopReasonLabel("stale_epoch"));
      stop = true;
      break;
    }

    const live = args.readLiveScope();
    if (!scopesEqual(live, frozenScope) || live.actorUserId !== frozenScope.actorUserId) {
      markRemainingNotStarted(index, stopReasonLabel("scope"));
      stop = true;
      break;
    }

    const entry = args.plan.eligible[index]!;
    const frozen = args.frozenByStoryId?.get(entry.row.storyId);
    const envelope =
      frozen ??
      buildAdminOpEnvelope({
        kind,
        row: entry.row,
        actorUserId: frozenScope.actorUserId,
        shopId: frozenScope.shopId,
        reason: args.reason,
      });

    const outcome: StoryExecutorResult = await runDurableStoryMutation({
      gate: args.gate,
      envelope,
      scope: frozenScope,
    });

    // Scope/epoch may have flipped while the request was in flight — keep this attempt's
    // truthful outcome, but never dispatch later rows under the old actor.
    const scopeAfter = args.readLiveScope();
    const epochAfter = args.gate.epochRef.current;
    const invalidatedAfter =
      epochAfter !== frozenEpoch || !scopesEqual(scopeAfter, frozenScope) || Boolean(args.isCancelled?.());

    if (outcome.state === "ok") {
      succeeded += 1;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        kind,
        outcome: "succeeded",
        message: null,
        envelope,
      });
    } else if (outcome.state === "failed") {
      if (isUncertainFailure(outcome)) {
        unknown += 1;
        items.push({
          storyId: entry.row.storyId,
          title: entry.row.title,
          kind,
          outcome: "unknown",
          message: outcome.message || "نتیجه نامشخص است؛ اقدام قابل ادامه است.",
          envelope: outcome.envelope,
        });
      } else {
        failed += 1;
        items.push({
          storyId: entry.row.storyId,
          title: entry.row.title,
          kind,
          outcome: "failed",
          message: outcome.message,
          envelope: outcome.envelope,
        });
      }
    } else if (outcome.state === "blocked") {
      if (outcome.reason === "stale_epoch" || outcome.reason === "scope" || outcome.reason === "auth") {
        unknown += 1;
        items.push({
          storyId: entry.row.storyId,
          title: entry.row.title,
          kind,
          outcome: "unknown",
          message: stopReasonLabel(outcome.reason),
          envelope,
        });
        markRemainingNotStarted(index + 1, stopReasonLabel(outcome.reason));
        stop = true;
        break;
      }
      unknown += 1;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        kind,
        outcome: "unknown",
        message: outcome.reason,
        envelope,
      });
    } else {
      unknown += 1;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        kind,
        outcome: "unknown",
        message: "unknown",
        envelope,
      });
    }

    if (invalidatedAfter && index + 1 < args.plan.eligible.length) {
      markRemainingNotStarted(
        index + 1,
        args.isCancelled?.()
          ? stopReasonLabel("cancelled")
          : epochAfter !== frozenEpoch
            ? stopReasonLabel("stale_epoch")
            : stopReasonLabel("scope"),
      );
      stop = true;
      break;
    }
  }

  return { items, succeeded, failed, unknown, skipped, notStarted, stoppedReason };
}

export function summarizeAdminBatch(result: StoryAdminBatchResult, kindLabel: string): string {
  const parts: string[] = [];
  if (result.succeeded > 0) parts.push(`${result.succeeded} موفق`);
  if (result.failed > 0) parts.push(`${result.failed} ناموفق`);
  if (result.unknown > 0) parts.push(`${result.unknown} نامشخص`);
  if (result.skipped > 0) parts.push(`${result.skipped} ردشده`);
  if (result.notStarted > 0) parts.push(`${result.notStarted} اجرا نشد`);
  return `${kindLabel}: ${parts.join("، ") || "بدون تغییر"}`;
}

export function outcomeLabelFa(outcome: StoryAdminOpOutcome): string {
  switch (outcome) {
    case "succeeded":
      return "موفق";
    case "failed":
      return "ناموفق";
    case "unknown":
      return "نامشخص";
    case "skipped":
      return "ردشده";
    case "not_started":
      return "اجرا نشد";
    default:
      return outcome;
  }
}
