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
  /** Stable server/local code for a definite refusal — used for the plain-Persian reason. */
  code?: string | null;
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
    // Server actionCodes no longer offer approve when the owning command would refuse the content.
    if (!row.contentReady) {
      return "رسانهٔ آماده ندارد؛ اسلاید باید تصویر یا ویدئوی بارگذاری‌شده داشته باشد.";
    }
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
  if (outcome.code === "story.response.unverified" || outcome.code === "story.transport.rejected") return true;
  const msg = (outcome.message ?? "").toLowerCase();
  return (
    msg.includes("unverified") ||
    msg.includes("host-unreachable") ||
    msg.includes("uncertain") ||
    msg.includes("ارتباط قطع")
  );
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
      code: null,
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
        code: null,
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
          code: outcome.code ?? null,
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
          code: outcome.code ?? null,
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
          code: `story.blocked.${outcome.reason}`,
          envelope,
        });
        markRemainingNotStarted(index + 1, stopReasonLabel(outcome.reason));
        stop = true;
        break;
      }
      // Pre-send blocks (in-flight/storage/envelope) never reached the wire: a definite non-effect.
      // They must not be reported as an uncertain outcome just because nothing was sent.
      failed += 1;
      items.push({
        storyId: entry.row.storyId,
        title: entry.row.title,
        kind,
        outcome: "failed",
        message: blockedReasonFa(outcome.reason),
        code: `story.blocked.${outcome.reason}`,
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
        code: "story.outcome.unknown",
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

export type SkippedReasonGroup = { reason: string; count: number };

/** Group skipped rows by plain reason — never enumerate titles (bounded for large selections). */
export function groupSkippedReasons(skipped: StoryAdminOpEligibility[]): SkippedReasonGroup[] {
  const counts = new Map<string, number>();
  for (const entry of skipped) {
    const reason = (entry.reason ?? "").trim() || "این استوری برای این اقدام مناسب نیست.";
    counts.set(reason, (counts.get(reason) ?? 0) + 1);
  }
  return [...counts.entries()].map(([reason, count]) => ({ reason, count }));
}

/**
 * One plain-Persian toast when nothing in the selection can run.
 * Prefers owner-facing plurals for common platform / awaiting-review cases.
 */
export function explainZeroEligibleToast(plan: StoryAdminOpPlan): string {
  const groups = groupSkippedReasons(plan.skipped);
  if (groups.length === 0) {
    return plan.kind === "approve"
      ? "استوری‌های ویترین اصلی به تأیید نیاز ندارند."
      : plan.kind === "reject"
        ? "فقط استوری‌های در انتظار تأیید قابل رد هستند."
        : "هیچ‌کدام از استوری‌های انتخاب‌شده قابل حذف نیستند.";
  }
  if (groups.length === 1) {
    const only = groups[0]!;
    const allPlatform = plan.skipped.every((s) => s.row.origin === "Admin");
    if (plan.kind === "approve" && allPlatform) {
      return "استوری‌های ویترین اصلی به تأیید نیاز ندارند.";
    }
    if (plan.kind === "reject") {
      const awaitingOnly = plan.skipped.every(
        (s) => s.row.origin === "Admin" || s.row.reviewStatus !== "Submitted" || !hasAction(s.row, "reject"),
      );
      if (awaitingOnly) return "فقط استوری‌های در انتظار تأیید قابل رد هستند.";
    }
    if (only.count > 1) return only.reason;
    return only.reason;
  }
  return groups.map((g) => (g.count > 1 ? `${g.reason} (${g.count})` : g.reason)).join("؛ ");
}

export type AdminBatchToastSeverity = "success" | "error" | "info" | "warning";

export type AdminBatchToast = { message: string; severity: AdminBatchToastSeverity };

/** Persian numerals keep the chat-style counts readable in an RTL operator surface. */
function faNum(value: number): string {
  return value.toLocaleString("fa-IR");
}

function verbPastFa(kind: StoryAdminOpKind): string {
  if (kind === "approve") return "تأیید شد";
  if (kind === "reject") return "رد شد";
  return "حذف شد";
}

/** Definite refusal wording — «تأیید نشد: …» is a result, not an uncertain outcome. */
function verbRefusedFa(kind: StoryAdminOpKind): string {
  if (kind === "approve") return "تأیید نشد";
  if (kind === "reject") return "رد نشد";
  return "حذف نشد";
}

function uncertainLeadFa(kind: StoryAdminOpKind): string {
  if (kind === "approve") return "نتیجهٔ تأیید";
  if (kind === "reject") return "نتیجهٔ رد";
  return "نتیجهٔ حذف";
}

/**
 * Server/local refusal codes → one short merchant sentence. The UI never shows raw codes or
 * technical jargon, and never invents a cause the server did not send.
 */
const STORY_REFUSAL_FA: Record<string, string> = {
  "story.content.notReady":
    "اسلاید قابل‌پخش ندارد؛ باید تصویر یا ویدئوی بارگذاری‌شده داشته باشد و دوباره ارسال شود.",
  "story.media.forbidden": "رسانهٔ اسلاید معتبر یا متعلق به این فروشگاه نیست؛ اسلاید را با فایل بارگذاری‌شده جایگزین کنید.",
  "story.media.unavailable": "سرویس رسانه در دسترس نیست؛ کمی بعد دوباره تلاش کنید.",
  "story.version.stale": "استوری بعد از بازکردن صفحه تغییر کرده است؛ فهرست را تازه کنید و دوباره تلاش کنید.",
  "story.version.required": "نسخهٔ استوری مشخص نیست؛ فهرست را تازه کنید و دوباره تلاش کنید.",
  "story.missing": "استوری پیدا نشد؛ ممکن است قبلاً حذف شده باشد.",
  "story.occurrence.conflict": "این اقدام با یک اقدام قبلی هم‌خوان نیست؛ فهرست را تازه کنید.",
  "story.mutation.rejected": "وضعیت فعلی استوری اجازهٔ این اقدام را نمی‌دهد؛ فهرست را تازه کنید.",
  "story.shop.authority.unavailable": "اطلاعات فروشگاه در دسترس نیست؛ کمی بعد دوباره تلاش کنید.",
  "story.cta.rejected": "مقصد محصول این استوری معتبر نیست؛ ابتدا مقصد اسلاید را اصلاح کنید.",
  "story.blocked.storage":
    "ثبت امن این اقدام ممکن نشد؛ یک اقدام ناتمام از قبل مانده است؛ ابتدا آن را از «اقدام‌های ناتمام» تمام کنید.",
  "story.blocked.envelope": "این اقدام با مسیر امن استوری هم‌خوان نبود؛ صفحه را تازه کنید.",
  "story.blocked.in_flight": "یک اقدام دیگر در حال اجراست؛ چند لحظه بعد دوباره تلاش کنید.",
  "story.outcome.unknown": "نتیجه مشخص نشد؛ اگر تغییری ندیدید دوباره تلاش کنید.",
};

export function storyRefusalReasonFa(code: string | null | undefined, message: string | null): string {
  const known = code?.trim() ? STORY_REFUSAL_FA[code.trim()] : undefined;
  if (known) return known;
  const text = (message ?? "").trim();
  if (text && !/^[a-z0-9._:-]+$/i.test(text)) return text;
  return "دلیل مشخصی از سرور نرسید؛ فهرست را تازه کنید و دوباره تلاش کنید.";
}

/** Pre-send block reasons (nothing was dispatched) — definite, never "unknown". */
export function blockedReasonFa(reason: string): string {
  const known = STORY_REFUSAL_FA[`story.blocked.${reason}`];
  if (known) return known;
  return "این اقدام ارسال نشد؛ صفحه را تازه کنید و دوباره تلاش کنید.";
}

function groupFailureReasons(failed: StoryAdminOpItemResult[]): SkippedReasonGroup[] {
  const counts = new Map<string, number>();
  for (const item of failed) {
    const reason = storyRefusalReasonFa(item.code, item.message);
    counts.set(reason, (counts.get(reason) ?? 0) + 1);
  }
  return [...counts.entries()].map(([reason, count]) => ({ reason, count }));
}

/**
 * ONE completion toast — totals + bounded reasons; skipped ≠ moderation «ردشده»; unknown stays uncertain.
 * A single row always names its exact refusal reason instead of a bare «۱ مورد انجام نشد».
 */
export function formatAdminBatchToast(result: StoryAdminBatchResult, kind: StoryAdminOpKind): AdminBatchToast {
  if (result.items.length === 1) {
    const only = result.items[0]!;
    if (only.outcome === "succeeded") {
      return { message: `${faNum(1)} استوری ${verbPastFa(kind)}`, severity: "success" };
    }
    if (only.outcome === "failed") {
      return {
        message: `${verbRefusedFa(kind)}: ${storyRefusalReasonFa(only.code, only.message)}`,
        severity: "error",
      };
    }
    if (only.outcome === "unknown") {
      return {
        message: `${uncertainLeadFa(kind)} نامشخص است؛ اگر تغییری ندیدید دوباره تلاش کنید.`,
        severity: "warning",
      };
    }
    return {
      message: `${verbRefusedFa(kind)}: ${(only.message ?? "").trim() || "این اقدام قابل اجرا نیست."}`,
      severity: "error",
    };
  }

  const parts: string[] = [];
  if (result.succeeded > 0) {
    parts.push(`${faNum(result.succeeded)} استوری ${verbPastFa(kind)}`);
  }
  const notDone = result.failed + result.skipped;
  if (notDone > 0) {
    parts.push(`${faNum(notDone)} مورد ${kind === "delete" ? "حذف نشد" : "انجام نشد"}`);
  }
  const failedItems = result.items.filter((item) => item.outcome === "failed");
  if (failedItems.length > 0) {
    const groups = groupFailureReasons(failedItems).slice(0, 3);
    const detail = groups
      .map((group) => (group.count > 1 ? `${group.reason} (${group.count})` : group.reason))
      .join("؛ ");
    if (detail) parts.push(detail);
  }
  if (result.unknown > 0) {
    parts.push(`${faNum(result.unknown)} مورد نتیجه‌اش نامشخص است و قابل ادامه است`);
  }
  if (result.notStarted > 0) {
    parts.push(`${faNum(result.notStarted)} مورد اجرا نشد`);
  }
  if (result.stoppedReason) {
    parts.push(result.stoppedReason);
  }
  const message = parts.join("؛ ") || "تغییری اعمال نشد.";
  let severity: AdminBatchToastSeverity = "info";
  if (result.unknown > 0) severity = "warning";
  else if (result.failed > 0) severity = result.succeeded > 0 ? "warning" : "error";
  else if (result.succeeded > 0) severity = result.skipped > 0 ? "warning" : "success";
  else if (result.notStarted > 0) severity = "warning";
  else severity = "error";
  return { message, severity };
}

/** @deprecated Prefer formatAdminBatchToast — kept for any residual callers. */
export function summarizeAdminBatch(result: StoryAdminBatchResult, kindLabel: string): string {
  void kindLabel;
  const kind: StoryAdminOpKind =
    result.items.find((i) => i.kind)?.kind ?? "approve";
  return formatAdminBatchToast(result, kind).message;
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
      return "انجام نشد";
    case "not_started":
      return "اجرا نشد";
    default:
      return outcome;
  }
}
