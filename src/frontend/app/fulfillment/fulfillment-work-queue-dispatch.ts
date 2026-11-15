/**
 * TB-260 — dispatch-timing client contract of the Admin fulfillment work queue.
 *
 * The server classifies every queue row with ONE pure authority and ships the bucket, the handoff window
 * state, the accepted plan and the exact operational-group locator with the row. This module only formats
 * those facts for the operator and carries the server-owned full-set counts: it never computes a deadline,
 * a "today" or a bucket from the browser clock, and it never looks up current shipping settings.
 */
import type { FulfillmentListRow, FulfillmentQueueFilter } from "./fulfillment-api.ts";
import {
  operationalDayLabel,
  operationalInstantLabel,
  operationalTimeZoneLabel,
  operationalTimeWindowLabel,
} from "../../lib/operational-format.ts";

/**
 * TB-269: the queue renders the SAME calendar the operations page renders. The user's screenshot showed a raw
 * `2026-11-15` handoff date beside Jalali deadlines, so these formatters now use the shared operational display
 * helpers instead of printing wire values. Display only: no instant, day or zone is derived or recomputed here.
 */
const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;

/** A date-only acceptance fact is that CALENDAR DAY. It is never converted through a client/UTC midnight. */
function acceptedDayLabel(
  raw: string | null | undefined,
  zone?: string | null,
): string | null {
  const value = (raw ?? "").trim();
  if (!value) return null;
  // Defensive only: the wire shape is a date. A full instant (never shipped here) keeps its accepted-zone label
  // rather than being shifted through the browser zone.
  if (!DATE_ONLY.test(value)) return operationalInstantLabel(value, readZone(zone));
  return operationalDayLabel(value);
}

/** A zone the runtime can actually format in; `null` means missing (`""`) or refused (unknown id). */
function readZone(zone: string | null | undefined): string | null {
  const raw = (zone ?? "").trim();
  if (!raw) return null;
  try {
    new Intl.DateTimeFormat("en-US", { timeZone: raw });
    return raw;
  } catch {
    return null;
  }
}

/** LTR isolation for a clock range inside an RTL line: the range keeps its direction and its start/end order. */
function isolatedTimeRange(window: string): string {
  return `\u2066${operationalTimeWindowLabel(window)}\u2069`;
}

/** The three honest display markers; none of them is a guessed date and none leaks a raw value. */
export const FULFILLMENT_DISPLAY_UNAVAILABLE = "مشخص نشده";
export const FULFILLMENT_DISPLAY_INVALID_DATE = "تاریخ نامعتبر";
export const FULFILLMENT_DISPLAY_INVALID_ZONE = "منطقهٔ زمانی نامعتبر";
export const FULFILLMENT_DISPLAY_MISSING_ZONE = "منطقهٔ زمانی مشخص نشده";

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
  if (!row.plannedHandoffDate) return FULFILLMENT_DISPLAY_UNAVAILABLE;
  const day = acceptedDayLabel(row.plannedHandoffDate, row.plannedHandoffTimeZone);
  if (!day) return FULFILLMENT_DISPLAY_INVALID_DATE;
  const parts = [day];
  const window = (row.plannedHandoffWindow ?? "").trim();
  if (window) parts.push(isolatedTimeRange(window));
  const declaredZone = (row.plannedHandoffTimeZone ?? "").trim();
  if (declaredZone) {
    const zone = readZone(declaredZone);
    if (!zone) return [...parts, FULFILLMENT_DISPLAY_INVALID_ZONE].join(" · ");
    const zoneLabel = operationalTimeZoneLabel(zone);
    if (zoneLabel) parts.push(zoneLabel);
  }
  return parts.join(" · ");
}

/** Customer delivery commitment — deliberately displayed apart from the dispatch plan. */
export function formatCustomerReceiving(
  row: Pick<FulfillmentListRow, "customerReceivingDate" | "customerReceivingWindow">,
): string {
  if (!row.customerReceivingDate) return "—";
  const day = acceptedDayLabel(row.customerReceivingDate);
  if (!day) return FULFILLMENT_DISPLAY_INVALID_DATE;
  const window = (row.customerReceivingWindow ?? "").trim();
  return window ? `${day} · ${isolatedTimeRange(window)}` : day;
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
 * TB-264 / TB-269: one operational deadline in the accepted carrier zone. The instant is produced by the server
 * from the accepted policy; this function only renders it in the human Persian calendar of that zone and never
 * shifts it. An unreadable instant or a refused zone is named truthfully instead of leaking a raw value.
 */
export function formatOperationalDeadline(utc: string | null | undefined, timeZone?: string | null): string {
  const raw = (utc ?? "").trim();
  if (!raw) return "—";
  if (Number.isNaN(new Date(raw).getTime())) return FULFILLMENT_DISPLAY_INVALID_DATE;
  const declaredZone = (timeZone ?? "").trim();
  if (!declaredZone) return FULFILLMENT_DISPLAY_MISSING_ZONE;
  const zone = readZone(declaredZone);
  if (!zone) return FULFILLMENT_DISPLAY_INVALID_ZONE;
  return operationalInstantLabel(raw, zone) ?? FULFILLMENT_DISPLAY_INVALID_DATE;
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
  const parts = [
    `آماده‌سازی تا ${formatOperationalDeadline(row.preparationStartUtc, zone)}`,
    `خروج تا ${formatOperationalDeadline(row.transferDepartureUtc, zone)}`,
    `تحویل به حامل تا ${formatOperationalDeadline(row.handoffDeadlineUtc, zone)}`,
  ];
  // The zone is one human label per cell («به وقت تهران»), never the raw id repeated beside every instant.
  const zoneLabel = operationalTimeZoneLabel(readZone(zone));
  return zoneLabel ? `${parts.join(" · ")} · ${zoneLabel}` : parts.join(" · ");
}
