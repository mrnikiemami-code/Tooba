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
  /**
   * TB-271: stage badges. Each one counts the WHOLE active set inside the selected time scope (after
   * search/column filters, before the selected stage), so a badge can never describe one fetched page.
   */
  stageAll: number;
  stagePreparation: number;
  stageReadyToSend: number;
  stageHandoff: number;
  stageDelivery: number;
  stageReturns: number;
  /** TB-271: the archive badge — time-independent and never part of an active total. */
  stageHistory: number;
  /** TB-271: time badges over the selected stage, before the selected time scope. */
  timeToday: number;
  timeFuture: number;
  timeAll: number;
  /** TB-271: the explicit «موعد مشخص نشده» subset of the current (stage + time) selection. */
  undated: number;
  /** TB-271: rows in the current (stage + time) selection — the set the page is cut from. */
  selectedTotal: number;
  /** TB-271: the server's own echo of the applied tokens. */
  selectedStage: string;
  selectedTimeScope: string;
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
  stageAll: 0,
  stagePreparation: 0,
  stageReadyToSend: 0,
  stageHandoff: 0,
  stageDelivery: 0,
  stageReturns: 0,
  stageHistory: 0,
  timeToday: 0,
  timeFuture: 0,
  timeAll: 0,
  undated: 0,
  selectedTotal: 0,
  selectedStage: "all",
  selectedTimeScope: "today",
};

/**
 * TB-271: the five employee job stages plus the unfiltered «همه». One stage per ACTIVE physical work unit — this
 * list replaces the two overlapping quick-filter rows and is a SELECTION over server-classified rows, never an
 * authorization or a second lifecycle.
 */
export type FulfillmentJobStage =
  | "all"
  | "preparation"
  | "ready_to_send"
  | "handoff"
  | "delivery"
  | "returns"
  | "history";

export const FULFILLMENT_JOB_STAGES: Array<{ id: FulfillmentJobStage; labelFa: string }> = [
  { id: "all", labelFa: "همه" },
  { id: "preparation", labelFa: "آماده‌سازی و بسته‌بندی" },
  { id: "ready_to_send", labelFa: "آمادهٔ ارسال" },
  { id: "handoff", labelFa: "تحویل به حامل" },
  { id: "delivery", labelFa: "پیگیری تحویل" },
  { id: "returns", labelFa: "برگشتی‌ها" },
];

/** The quiet archive view: outside the five competing stages, never part of an active total. */
export const FULFILLMENT_HISTORY_STAGE_LABEL = "سابقهٔ ارسال";

/** TB-271: the ONLY primary time scopes, defaulting to today on a clean entry. */
export type FulfillmentTimeScope = "today" | "future" | "all";

export const FULFILLMENT_TIME_SCOPES: Array<{ id: FulfillmentTimeScope; labelFa: string; hintFa: string }> = [
  { id: "today", labelFa: "امروز", hintFa: "کار سررسیدشده، امروز و کار بدون موعد مشخص" },
  { id: "future", labelFa: "آینده", hintFa: "کارهای فعال با زمان مشخص در روزهای آینده" },
  { id: "all", labelFa: "همه", hintFa: "همهٔ کارهای فعال، شامل موارد بدون موعد" },
];

export const FULFILLMENT_DEFAULT_JOB_STAGE: FulfillmentJobStage = "all";
export const FULFILLMENT_DEFAULT_TIME_SCOPE: FulfillmentTimeScope = "today";

const KNOWN_JOB_STAGES: readonly string[] = ["all", "preparation", "ready_to_send", "handoff", "delivery", "returns", "history"];
const KNOWN_TIME_SCOPES: readonly string[] = ["today", "future", "all"];

/** Normalizes a stage token; an unknown token clears to «همه» instead of guessing a stage. */
export function normalizeJobStage(value: string | null | undefined): FulfillmentJobStage {
  const token = (value ?? "").trim().toLowerCase();
  return (KNOWN_JOB_STAGES.includes(token) ? token : FULFILLMENT_DEFAULT_JOB_STAGE) as FulfillmentJobStage;
}

/** Normalizes a time-scope token; an unknown token clears to the clean default «امروز». */
export function normalizeTimeScope(value: string | null | undefined): FulfillmentTimeScope {
  const token = (value ?? "").trim().toLowerCase();
  return (KNOWN_TIME_SCOPES.includes(token) ? token : FULFILLMENT_DEFAULT_TIME_SCOPE) as FulfillmentTimeScope;
}

/**
 * TB-271: the documented client mirror of the server's legacy `queueFilter` mapping. An old deep link is
 * translated ONCE into a meaningful (stage, scope) selection and never sent as a second hidden filter.
 */
export function legacyQueueSelection(token: string | null | undefined): {
  stage: FulfillmentJobStage;
  scope: FulfillmentTimeScope;
} {
  const value = (token ?? "").trim().toLowerCase();
  switch (value) {
    case "ready_to_process":
    case "ready_to_pack":
      return { stage: "preparation", scope: "all" };
    case "ready_to_ship":
    case "missing_tracking":
      return { stage: "ready_to_send", scope: "all" };
    case "in_transit":
      return { stage: "delivery", scope: "all" };
    case "delivered":
    case "problem":
      return { stage: "history", scope: "all" };
    default:
      return { stage: FULFILLMENT_DEFAULT_JOB_STAGE, scope: FULFILLMENT_DEFAULT_TIME_SCOPE };
  }
}

/** The badge number of one stage inside the selected time scope; `null` counts render as unknown, not zero. */
export function stageCount(counts: FulfillmentQueueCounts, stage: FulfillmentJobStage): number {
  switch (stage) {
    case "preparation":
      return counts.stagePreparation;
    case "ready_to_send":
      return counts.stageReadyToSend;
    case "handoff":
      return counts.stageHandoff;
    case "delivery":
      return counts.stageDelivery;
    case "returns":
      return counts.stageReturns;
    case "history":
      return counts.stageHistory;
    default:
      return counts.stageAll;
  }
}

/** The badge number of one time scope over the selected stage. */
export function timeCount(counts: FulfillmentQueueCounts, scope: FulfillmentTimeScope): number {
  switch (scope) {
    case "future":
      return counts.timeFuture;
    case "all":
      return counts.timeAll;
    default:
      return counts.timeToday;
  }
}

export const FULFILLMENT_JOB_STAGE_LABELS: Record<string, string> = {
  preparation: "آماده‌سازی و بسته‌بندی",
  ready_to_send: "آمادهٔ ارسال",
  handoff: "تحویل به حامل",
  delivery: "پیگیری تحویل",
  returns: "برگشتی‌ها",
  history: FULFILLMENT_HISTORY_STAGE_LABEL,
};

export const FULFILLMENT_JOB_TIME_STATE_LABELS: Record<string, string> = {
  past_due: "گذشته",
  due_today: "امروز",
  future: "آینده",
  undated: "",
};

function field(value: unknown, camel: string, pascal: string): unknown {
  const record = value && typeof value === "object" ? (value as Record<string, unknown>) : null;
  return record ? record[camel] ?? record[pascal] : undefined;
}

/**
 * Maps the server's `dispatchCounts` block. Absent or unreadable counts become zeros — a page tally is never
 * substituted, because that would silently under-report the queue. The screen separates "unknown" (a payload
 * without the block) from these zeros through <see cref="readDispatchCountsFromPayload"/>.
 */
export function mapFulfillmentQueueCounts(value: unknown): FulfillmentQueueCounts {
  const counts = value && typeof value === "object" ? (value as Record<string, unknown>) : null;
  if (!counts) return FULFILLMENT_EMPTY_QUEUE_COUNTS;
  const amount = (camel: string, pascal: string): number => {
    const parsed = Number(counts[camel] ?? counts[pascal]);
    return Number.isFinite(parsed) ? parsed : 0;
  };
  const token = (camel: string, pascal: string, fallback: string): string => {
    const raw = counts[camel] ?? counts[pascal];
    return typeof raw === "string" && raw.trim() ? raw.trim() : fallback;
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
    stageAll: amount("stageAll", "StageAll"),
    stagePreparation: amount("stagePreparation", "StagePreparation"),
    stageReadyToSend: amount("stageReadyToSend", "StageReadyToSend"),
    stageHandoff: amount("stageHandoff", "StageHandoff"),
    stageDelivery: amount("stageDelivery", "StageDelivery"),
    stageReturns: amount("stageReturns", "StageReturns"),
    stageHistory: amount("stageHistory", "StageHistory"),
    timeToday: amount("timeToday", "TimeToday"),
    timeFuture: amount("timeFuture", "TimeFuture"),
    timeAll: amount("timeAll", "TimeAll"),
    undated: amount("undated", "Undated"),
    selectedTotal: amount("selectedTotal", "SelectedTotal"),
    selectedStage: token("selectedStage", "SelectedStage", "all"),
    selectedTimeScope: token("selectedTimeScope", "SelectedTimeScope", "today"),
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

/**
 * Reads the module-owned `dispatchCounts` block out of a raw GridQuery payload. A payload WITHOUT the block is
 * `undefined` (unknown), never six plausible zeros — an unknown result must not look like a successful empty queue.
 */
export function readDispatchCountsFromPayload(payload: unknown): FulfillmentQueueCounts | undefined {
  const block = field(payload, "dispatchCounts", "DispatchCounts");
  if (!block || typeof block !== "object") return undefined;
  return mapFulfillmentQueueCounts(block);
}

/** The operator label of one stage token; an unknown token is returned verbatim rather than guessed. */
export function formatJobStageLabel(row: {
  jobStage?: string | null;
  jobStageLabelFa?: string | null;
}): string {
  const label = (row.jobStageLabelFa ?? "").trim();
  if (label) return label;
  const token = (row.jobStage ?? "").trim().toLowerCase();
  return FULFILLMENT_JOB_STAGE_LABELS[token] ?? (token ? token : "—");
}

/**
 * TB-271: the row's relevant OPERATIONAL time, with its precise meaning. Each stage answers a different stored
 * instant (preparation start ≠ departure ≠ carrier close), delivery/return follow-up stores no SLA at all, and an
 * active row whose accepted evidence carries no plan says so instead of borrowing another row's deadline.
 */
export function formatJobTime(row: {
  jobTimeMeaning?: string | null;
  jobTimeLabelFa?: string | null;
  jobTimeUtc?: string | null;
  jobTimeState?: string | null;
  plannedHandoffTimeZone?: string | null;
}): string {
  const label = (row.jobTimeLabelFa ?? "").trim();
  const raw = (row.jobTimeUtc ?? "").trim();
  const meaning = (row.jobTimeMeaning ?? "").trim().toLowerCase();
  if (!label) return "—";
  // Undated by FACT (no plan, or an ongoing follow-up with no stored SLA): the sentence IS the answer.
  if (!raw || meaning === "missing_plan" || meaning === "ongoing_followup" || meaning === "none") return label;
  const zone = readZone(row.plannedHandoffTimeZone);
  const instant = zone ? operationalInstantLabel(raw, zone) : null;
  if (!instant) {
    // A refused/absent zone is named truthfully; the browser zone is never substituted.
    const zoneProblem = (row.plannedHandoffTimeZone ?? "").trim() ? FULFILLMENT_DISPLAY_INVALID_ZONE : FULFILLMENT_DISPLAY_MISSING_ZONE;
    return `${label} · ${zoneProblem}`;
  }
  const zoneLabel = operationalTimeZoneLabel(zone);
  const stateLabel = FULFILLMENT_JOB_TIME_STATE_LABELS[(row.jobTimeState ?? "").trim().toLowerCase()] ?? "";
  const parts = [label, instant];
  if (zoneLabel) parts.push(zoneLabel);
  if (stateLabel) parts.push(stateLabel);
  return parts.join(" · ");
}

/**
 * TB-271: a HUMAN parcel locator inside its whole physical group («مرسولهٔ ۲ از ۳»), so two parcels of one order
 * stay distinguishable without a raw GUID. A row that is not an ordinated parcel keeps its own scope label.
 */
export function formatWorkUnitOrdinal(row: {
  workUnitKind?: string | null;
  workUnitOrdinal?: number | null;
  workUnitOrdinalTotal?: number | null;
}): string | null {
  if ((row.workUnitKind ?? "") !== "parcel") return null;
  const ordinal = Number(row.workUnitOrdinal ?? 0);
  const total = Number(row.workUnitOrdinalTotal ?? 0);
  if (!Number.isFinite(ordinal) || ordinal <= 0 || !Number.isFinite(total) || total <= 0) return null;
  return `مرسولهٔ ${ordinal.toLocaleString("fa-IR")} از ${total.toLocaleString("fa-IR")}`;
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
