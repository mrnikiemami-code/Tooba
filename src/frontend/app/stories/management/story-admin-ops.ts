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

function hasAction(row: AdminStorySnapshot, code: string): boolean {
  return (row.actionCodes ?? []).includes(code);
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

export type StoryAdminOpItemResult = {
  storyId: string;
  title: string;
  outcome: "succeeded" | "failed" | "unknown" | "skipped";
  message: string | null;
  envelope: StoryIntentEnvelope | null;
};

export type StoryAdminBatchResult = {
  items: StoryAdminOpItemResult[];
  succeeded: number;
  failed: number;
  unknown: number;
  skipped: number;
};

/**
 * Sequential per-row execution. Failed/unknown keep their frozen envelope; succeeded clear via executor.
 * Callers retry only unresolved retained intents — never re-mint keys for a finished row.
 */
export async function executeAdminOpBatch(args: {
  plan: StoryAdminOpPlan;
  gate: StoryExecutorGate;
  scope: StoryExecutorScope;
  actorUserId: string;
  reason?: string;
  /** Optional pre-frozen envelopes keyed by storyId (exact retry). */
  frozenByStoryId?: Map<string, StoryIntentEnvelope>;
}): Promise<StoryAdminBatchResult> {
  const items: StoryAdminOpItemResult[] = [];
  for (const skip of args.plan.skipped) {
    items.push({
      storyId: skip.row.storyId,
      title: skip.row.title,
      outcome: "skipped",
      message: skip.reason,
      envelope: null,
    });
  }

  let succeeded = 0;
  let failed = 0;
  let unknown = 0;
  const skipped = args.plan.skipped.length;

  for (const entry of args.plan.eligible) {
    const frozen = args.frozenByStoryId?.get(entry.row.storyId);
    const envelope =
      frozen ??
      buildAdminOpEnvelope({
        kind: args.plan.kind,
        row: entry.row,
        actorUserId: args.actorUserId,
        shopId: args.scope.shopId,
        reason: args.reason,
      });
    const outcome: StoryExecutorResult = await runDurableStoryMutation({
      gate: args.gate,
      envelope,
      scope: args.scope,
    });
    if (outcome.state === "ok") {
      succeeded += 1;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        outcome: "succeeded",
        message: null,
        envelope,
      });
    } else if (outcome.state === "failed") {
      failed += 1;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        outcome: "failed",
        message: outcome.message,
        envelope: outcome.envelope,
      });
    } else if (outcome.state === "blocked") {
      unknown += 1;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        outcome: "unknown",
        message: outcome.reason,
        envelope,
      });
    } else {
      unknown += 1;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        outcome: "unknown",
        message: "unknown",
        envelope,
      });
    }
  }

  return { items, succeeded, failed, unknown, skipped };
}

export function summarizeAdminBatch(result: StoryAdminBatchResult, kindLabel: string): string {
  const parts: string[] = [];
  if (result.succeeded > 0) parts.push(`${result.succeeded} موفق`);
  if (result.failed > 0) parts.push(`${result.failed} ناموفق`);
  if (result.unknown > 0) parts.push(`${result.unknown} نامشخص`);
  if (result.skipped > 0) parts.push(`${result.skipped} ردشده`);
  return `${kindLabel}: ${parts.join("، ") || "بدون تغییر"}`;
}
