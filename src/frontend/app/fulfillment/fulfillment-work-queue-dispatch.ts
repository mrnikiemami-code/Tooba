/**
 * TB-260 — dispatch-timing client contract of the Admin fulfillment work queue.
 *
 * The server classifies every queue row with ONE pure authority and ships the bucket, the handoff window
 * state, the accepted plan and the exact operational-group locator with the row. This module only formats
 * those facts for the operator and carries the server-owned full-set counts: it never computes a deadline,
 * a "today" or a bucket from the browser clock, and it never looks up current shipping settings.
 */
import type { FulfillmentListRow, FulfillmentQueueFilter } from "./fulfillment-api.ts";

/** Honest counts of the whole filtered queue set (never of the current page). */
export interface FulfillmentQueueCounts {
  totalActive: number;
  due: number;
  today: number;
  overdue: number;
  upcoming: number;
  unspecified: number;
  inTransit: number;
  /** TB-264: rows whose physical preparation must already have started. */
  preparationDue: number;
}

export const FULFILLMENT_EMPTY_QUEUE_COUNTS: FulfillmentQueueCounts = {
  totalActive: 0,
  due: 0,
  today: 0,
  overdue: 0,
  upcoming: 0,
  unspecified: 0,
  inTransit: 0,
  preparationDue: 0,
};

function field(value: unknown, camel: string, pascal: string): unknown {
  const record = value && typeof value === "object" ? (value as Record<string, unknown>) : null;
  return record ? record[camel] ?? record[pascal] : undefined;
}

/**
 * Maps the server's `dispatchCounts` block. Absent or unreadable counts become zeros — a page tally is never
 * substituted, because that would silently under-report the queue.
 */
export function mapFulfillmentQueueCounts(value: unknown): FulfillmentQueueCounts {
  const counts = value && typeof value === "object" ? (value as Record<string, unknown>) : null;
  if (!counts) return FULFILLMENT_EMPTY_QUEUE_COUNTS;
  const amount = (camel: string, pascal: string): number => {
    const parsed = Number(counts[camel] ?? counts[pascal]);
    return Number.isFinite(parsed) ? parsed : 0;
  };
  return {
    totalActive: amount("totalActive", "TotalActive"),
    due: amount("due", "Due"),
    today: amount("today", "Today"),
    overdue: amount("overdue", "Overdue"),
    upcoming: amount("upcoming", "Upcoming"),
    unspecified: amount("unspecified", "Unspecified"),
    inTransit: amount("inTransit", "InTransit"),
    preparationDue: amount("preparationDue", "PreparationDue"),
  };
}

/**
 * Dispatch-timing quick filters, in the operator's daily order of work: what is already late, then today,
 * then everything still ahead, then the rows whose accepted evidence carries no plan at all.
 */
export const FULFILLMENT_DISPATCH_FILTERS: Array<{ id: FulfillmentQueueFilter; labelFa: string; labelEn: string }> = [
  { id: "dispatch_preparation_due", labelFa: "شروع آماده‌سازی رسیده", labelEn: "Preparation due" },
  { id: "dispatch_overdue", labelFa: "سررسیدشده", labelEn: "Overdue" },
  { id: "dispatch_today", labelFa: "ارسال امروز", labelEn: "Due today" },
  { id: "dispatch_due", labelFa: "عقب‌افتاده", labelEn: "Backlog" },
  { id: "dispatch_upcoming", labelFa: "آینده", labelEn: "Upcoming" },
  { id: "dispatch_unspecified", labelFa: "موعد ارسال مشخص نشده", labelEn: "No dispatch date" },
];

export const FULFILLMENT_DISPATCH_BUCKET_LABELS: Record<string, { fa: string; en: string }> = {
  overdue: { fa: "سررسیدشده", en: "Overdue" },
  today: { fa: "ارسال امروز", en: "Due today" },
  backlog: { fa: "عقب‌افتاده", en: "Backlog" },
  upcoming: { fa: "آینده", en: "Upcoming" },
  unspecified: { fa: "موعد ارسال مشخص نشده", en: "No dispatch date" },
  in_transit: { fa: "در مسیر", en: "In transit" },
  not_queued: { fa: "در صف نیست", en: "Not queued" },
};

export const FULFILLMENT_HANDOFF_WINDOW_LABELS: Record<string, { fa: string; en: string }> = {
  open: { fa: "بازهٔ تحویل باز است", en: "Window open" },
  closed: { fa: "بازهٔ تحویل گذشته است", en: "Window closed" },
  today_not_open: { fa: "هنوز به بازه نرسیده", en: "Window not open yet" },
  future: { fa: "در آینده", en: "Future" },
  none: { fa: "بدون بازه", en: "No window" },
};

/**
 * The server ships wire-stable PascalCase names (`Today`, `TodayNotOpen`, `InTransit`, `NotQueued`,
 * `None` …). They are normalised to the lower-case keys used here; an already lower-case or snake_case
 * token (older payload, tests) passes through unchanged. Never localise by guessing from Persian text.
 */
export function normalizeDispatchToken(value: string | null | undefined): string {
  if (!value) return "";
  return value
    .trim()
    .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
    .toLowerCase();
}

/** Persian label of the timing bucket; an unknown server id is shown verbatim rather than guessed. */
export function formatDispatchBucketLabel(bucket: string | null | undefined): string {
  if (!bucket) return FULFILLMENT_DISPATCH_BUCKET_LABELS.unspecified.fa;
  return FULFILLMENT_DISPATCH_BUCKET_LABELS[normalizeDispatchToken(bucket)]?.fa ?? bucket;
}

export function formatHandoffWindowLabel(state: string | null | undefined): string {
  if (!state) return FULFILLMENT_HANDOFF_WINDOW_LABELS.none.fa;
  return FULFILLMENT_HANDOFF_WINDOW_LABELS[normalizeDispatchToken(state)]?.fa ?? state;
}

/** «موعد ارسال» is built only from the accepted plan; an absent plan is shown, never invented. */
export function formatPlannedHandoff(
  row: Pick<FulfillmentListRow, "plannedHandoffDate" | "plannedHandoffWindow" | "plannedHandoffTimeZone">,
): string {
  if (!row.plannedHandoffDate) return "مشخص نشده";
  const window = row.plannedHandoffWindow ? ` · ${row.plannedHandoffWindow}` : "";
  const zone = row.plannedHandoffTimeZone ? ` (${row.plannedHandoffTimeZone})` : "";
  return `${row.plannedHandoffDate}${window}${zone}`;
}

/** Customer delivery commitment — deliberately displayed apart from the dispatch plan. */
export function formatCustomerReceiving(
  row: Pick<FulfillmentListRow, "customerReceivingDate" | "customerReceivingWindow">,
): string {
  if (!row.customerReceivingDate) return "—";
  return row.customerReceivingWindow
    ? `${row.customerReceivingDate} · ${row.customerReceivingWindow}`
    : row.customerReceivingDate;
}

/** Reads the module-owned `dispatchCounts` block out of a raw GridQuery payload. */
export function readDispatchCountsFromPayload(payload: unknown): FulfillmentQueueCounts {
  return mapFulfillmentQueueCounts(field(payload, "dispatchCounts", "DispatchCounts"));
}

/**
 * TB-264: server-classified physical-preparation urgency. `None` means the accepted plan carries no
 * preparation deadline (legacy policy v1), so the operator is told nothing rather than an invented one.
 */
export const FULFILLMENT_PREPARATION_STATE_LABELS: Record<string, { fa: string; en: string }> = {
  none: { fa: "بدون موعد آماده‌سازی", en: "No preparation deadline" },
  not_applicable: { fa: "آماده‌سازی انجام شده", en: "Preparation done" },
  upcoming: { fa: "آماده‌سازی در آینده", en: "Preparation upcoming" },
  today: { fa: "شروع آماده‌سازی امروز", en: "Preparation starts today" },
  overdue: { fa: "شروع آماده‌سازی رسیده", en: "Preparation due now" },
  handoff_missed: { fa: "موعد تحویل به حامل گذشته", en: "Handoff deadline passed" },
};

export function formatPreparationState(state: string | null | undefined): string {
  if (!state) return FULFILLMENT_PREPARATION_STATE_LABELS.none.fa;
  return FULFILLMENT_PREPARATION_STATE_LABELS[normalizeDispatchToken(state)]?.fa ?? state;
}

/**
 * TB-264: one operational deadline in the accepted carrier zone. The instant is produced by the server
 * from the accepted policy; this function only renders it and never shifts it.
 */
export function formatOperationalDeadline(utc: string | null | undefined, timeZone?: string | null): string {
  if (!utc) return "—";
  const parsed = new Date(utc);
  if (Number.isNaN(parsed.getTime())) return utc;
  try {
    return new Intl.DateTimeFormat("fa-IR", {
      timeZone: timeZone || "UTC",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    }).format(parsed);
  } catch {
    return utc;
  }
}

/** «آماده‌سازی تا … · خروج تا … · تحویل به حامل تا …» — absent deadlines stay explicitly «—». */
export function formatPreparationDeadlines(
  row: Pick<
    FulfillmentListRow,
    "preparationStartUtc" | "transferDepartureUtc" | "handoffDeadlineUtc" | "plannedHandoffTimeZone"
  >,
): string {
  if (!row.preparationStartUtc && !row.transferDepartureUtc && !row.handoffDeadlineUtc) {
    return "—";
  }
  const zone = row.plannedHandoffTimeZone;
  return [
    `آماده‌سازی تا ${formatOperationalDeadline(row.preparationStartUtc, zone)}`,
    `خروج تا ${formatOperationalDeadline(row.transferDepartureUtc, zone)}`,
    `تحویل به حامل تا ${formatOperationalDeadline(row.handoffDeadlineUtc, zone)}`,
  ].join(" · ");
}
