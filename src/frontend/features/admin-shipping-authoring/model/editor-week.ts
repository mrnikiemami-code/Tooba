/**
 * Local WEEKLY-SCHEDULE intent of ONE shipping method (shared by every region group).
 *
 * A configured schedule initializes from the real stored timezone + seven ISO days (1 = Monday).
 * An UNCONFIGURED schedule stays explicitly unconfigured until the operator types a timezone and
 * deliberately clicks Configure: only then do seven CLOSED days with blank hours appear, and no
 * browser timezone, default working hours or fabricated persisted configuration is invented.
 * The typed-but-unconfigured timezone is still real local WORK: it is dirty (and blocks Apply with
 * an explicit configure-or-reset issue) while the `weeklySchedule` wrapper stays omitted.
 *
 * Rules mirrored from the shipping domain (no new policy):
 * - exactly seven unique ISO weekdays;
 * - a working day requires an explicit `start < end` pair (overnight is not a legal pair);
 * - a closed day keeps its remembered hours, or none;
 * - the timezone text is trimmed and must be non-empty within the stored length limit;
 * - there is no clear operation for a configured schedule (absent/edit only).
 *
 * Provenance is PER BOUND, not per day: editing the start must never re-interpret the untouched
 * stored end (and the reverse). An untouched bound keeps its exact stored token, including seconds
 * and fractions; a deliberate `HH:mm` edit becomes a lawful `HH:mm:00` `TimeText`. Range checks
 * compare actual time values (`18:00:00` equals `18:00:00.0000000`), never string spelling.
 */
import type { EditorScheduleDay, EditorWeeklySchedule, TimeText } from "./editor-document.ts";
import type { ScheduleDayEdit, WeeklyScheduleEdit } from "./editor-edits.ts";

/** Mirrors `ShippingRatePolicyLimits.TimeZoneIdMaxLength`. */
export const TIME_ZONE_MAX_LENGTH = 100;

/** ISO weekdays, Monday first. */
export const WEEKDAYS = [1, 2, 3, 4, 5, 6, 7] as const;

export interface ScheduleDayDraft {
  readonly weekday: number;
  readonly isWorking: boolean;
  /** Exact untouched `TimeText`, or an `HH:mm` the operator typed through the time input. */
  readonly startTime: string;
  readonly endTime: string;
  /** Per-bound provenance: true only for the bound this operator actually typed. */
  readonly startEdited: boolean;
  readonly endEdited: boolean;
}

export type WeekFormState =
  | { readonly kind: "unconfigured"; readonly pendingTimeZone: string }
  | {
      readonly kind: "configured";
      readonly timeZoneId: string;
      readonly days: readonly ScheduleDayDraft[];
    };

const STORED_TIME = /^(?:[01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9](?:\.[0-9]{1,7})?$/;
const INPUT_TIME = /^(?:[01][0-9]|2[0-3]):[0-5][0-9]$/;

/**
 * Comparable absolute value of a `TimeText`: 100-nanosecond ticks within the day.
 * `18:00:00` and `18:00:00.0000000` are the same value. `null` for malformed text.
 */
export function timeValue(text: string): number | null {
  const match = /^((?:[01][0-9]|2[0-3])):([0-5][0-9]):([0-5][0-9])(?:\.([0-9]{1,7}))?$/.exec(text);
  if (!match) return null;
  const seconds = Number(match[1]) * 3600 + Number(match[2]) * 60 + Number(match[3]);
  const fraction = Number((match[4] ?? "").padEnd(7, "0"));
  return seconds * 10_000_000 + fraction;
}

/** True when two lawful time texts denote the same instant (spelling is irrelevant). */
function sameTimeValue(left: string | null, right: string | null): boolean {
  if (left === null || right === null) return left === right;
  const leftValue = timeValue(left);
  const rightValue = timeValue(right);
  return leftValue !== null && rightValue !== null ? leftValue === rightValue : left === right;
}

function dayDraft(day: EditorScheduleDay): ScheduleDayDraft {
  return {
    weekday: day.weekday,
    isWorking: day.isWorking,
    startTime: day.startTime ?? "",
    endTime: day.endTime ?? "",
    startEdited: false,
    endEdited: false,
  };
}

/** Initial state: the real schedule, or an explicit unconfigured state with an empty timezone. */
export function createWeekForm(schedule: EditorWeeklySchedule | null): WeekFormState {
  if (schedule === null) return { kind: "unconfigured", pendingTimeZone: "" };
  return {
    kind: "configured",
    timeZoneId: schedule.timeZoneId,
    days: schedule.days.map(dayDraft),
  };
}

/** Operator typed a timezone for an UNCONFIGURED schedule (still not configured). */
export function withPendingTimeZone(state: WeekFormState, value: string): WeekFormState {
  return state.kind === "unconfigured" ? { ...state, pendingTimeZone: value } : state;
}

/** Deliberate Configure: seven CLOSED days, blank hours, the operator-supplied timezone only. */
export function configureWeek(state: WeekFormState): WeekFormState {
  if (state.kind === "configured") return state;
  return {
    kind: "configured",
    timeZoneId: state.pendingTimeZone,
    days: WEEKDAYS.map((weekday) => ({
      weekday,
      isWorking: false,
      startTime: "",
      endTime: "",
      startEdited: false,
      endEdited: false,
    })),
  };
}

/** Local reset of a configured draft back to its confirmed stored schedule (and clears pending text). */
export function resetWeek(schedule: EditorWeeklySchedule | null): WeekFormState {
  return createWeekForm(schedule);
}

export function withTimeZone(state: WeekFormState, value: string): WeekFormState {
  return state.kind === "configured" ? { ...state, timeZoneId: value } : state;
}

function replaceDay(
  state: WeekFormState,
  weekday: number,
  patch: (day: ScheduleDayDraft) => ScheduleDayDraft,
): WeekFormState {
  if (state.kind !== "configured") return state;
  return { ...state, days: state.days.map((day) => (day.weekday === weekday ? patch(day) : day)) };
}

/** Working/closed toggle: BOTH remembered hour texts stay exactly as they are. */
export function withDayWorking(state: WeekFormState, weekday: number, isWorking: boolean): WeekFormState {
  return replaceDay(state, weekday, (day) => ({ ...day, isWorking }));
}

/** A deliberate `HH:mm` (or blank) edit of ONE bound; the other bound keeps its provenance. */
export function withDayTime(
  state: WeekFormState,
  weekday: number,
  bound: "startTime" | "endTime",
  value: string,
): WeekFormState {
  return replaceDay(state, weekday, (day) =>
    bound === "startTime"
      ? { ...day, startTime: value, startEdited: true }
      : { ...day, endTime: value, endEdited: true },
  );
}

/** Why the local week cannot be serialized (localized by the screen). */
export type WeekIssueCode =
  | "timeZoneRequired"
  | "timeZoneTooLong"
  | "dayHoursRequired"
  | "dayTimeInvalid"
  | "dayRangeInvalid"
  | "pendingTimeZoneUnapplied";

export interface WeekIssue {
  readonly code: WeekIssueCode;
  readonly weekday: number | null;
}

/** One lawful `TimeText` out of a stored token or an `HH:mm` edit; `null` when invalid/blank. */
function canonicalTime(value: string, edited: boolean): TimeText | null {
  if (value === "") return null;
  if (edited) return INPUT_TIME.test(value) ? `${value}:00` : null;
  return STORED_TIME.test(value) ? value : null;
}

/** All blocking week issues, or `[]` when the state is serializable. */
export function weekIssues(state: WeekFormState): readonly WeekIssue[] {
  if (state.kind === "unconfigured") {
    // Typed-but-unconfigured timezone is real local work with no lawful wire representation:
    // Apply is blocked until the operator configures it or resets the section.
    return state.pendingTimeZone.trim().length > 0 ? [{ code: "pendingTimeZoneUnapplied", weekday: null }] : [];
  }
  const issues: WeekIssue[] = [];
  const timeZone = state.timeZoneId.trim();
  if (timeZone.length === 0) issues.push({ code: "timeZoneRequired", weekday: null });
  else if (timeZone.length > TIME_ZONE_MAX_LENGTH) issues.push({ code: "timeZoneTooLong", weekday: null });

  const seen = new Set<number>();
  for (const day of state.days) {
    if (!seen.add(day.weekday)) {
      issues.push({ code: "dayRangeInvalid", weekday: day.weekday });
      continue;
    }
    const start = canonicalTime(day.startTime, day.startEdited);
    const end = canonicalTime(day.endTime, day.endEdited);
    const startPresent = day.startTime !== "";
    const endPresent = day.endTime !== "";
    if (day.isWorking) {
      if (!startPresent || !endPresent) {
        issues.push({ code: "dayHoursRequired", weekday: day.weekday });
        continue;
      }
      if (start === null || end === null) {
        issues.push({ code: "dayTimeInvalid", weekday: day.weekday });
        continue;
      }
      // Compare REAL values (fractions included): a zero-length interval is still invalid.
      const startValue = timeValue(start)!;
      const endValue = timeValue(end)!;
      if (endValue <= startValue) issues.push({ code: "dayRangeInvalid", weekday: day.weekday });
      continue;
    }
    if (!startPresent && !endPresent) continue;
    if (!startPresent || !endPresent || start === null || end === null) {
      issues.push({ code: "dayTimeInvalid", weekday: day.weekday });
      continue;
    }
    if (timeValue(end)! <= timeValue(start)!) issues.push({ code: "dayRangeInvalid", weekday: day.weekday });
  }
  if (seen.size !== WEEKDAYS.length) issues.push({ code: "dayRangeInvalid", weekday: null });
  return issues;
}

/** Serialized days of an issue-free configured draft (all seven, canonical times). */
function dayEdits(state: Extract<WeekFormState, { kind: "configured" }>): readonly ScheduleDayEdit[] {
  return WEEKDAYS.map((weekday) => {
    const day = state.days.find((candidate) => candidate.weekday === weekday)!;
    return {
      weekday,
      isWorking: day.isWorking,
      startTime: canonicalTime(day.startTime, day.startEdited),
      endTime: canonicalTime(day.endTime, day.endEdited),
    };
  });
}

function sameDays(schedule: EditorWeeklySchedule, state: Extract<WeekFormState, { kind: "configured" }>): boolean {
  if (schedule.days.length !== state.days.length) return false;
  const edits = dayEdits(state);
  return schedule.days.every((stored) => {
    const local = edits.find((day) => day.weekday === stored.weekday);
    return (
      local !== undefined &&
      local.isWorking === stored.isWorking &&
      sameTimeValue(local.startTime, stored.startTime) &&
      sameTimeValue(local.endTime, stored.endTime)
    );
  });
}

/**
 * True when the local week is real work: a changed configured week, a deliberately configured week,
 * OR a typed-but-unconfigured timezone (raw local text, never a configured week).
 */
export function weekDirty(schedule: EditorWeeklySchedule | null, state: WeekFormState): boolean {
  if (state.kind === "unconfigured") return state.pendingTimeZone.trim().length > 0;
  if (schedule === null) return true;
  if (schedule.timeZoneId !== state.timeZoneId.trim()) return true;
  return !sameDays(schedule, state);
}

/**
 * The `weeklySchedule` intent for the ONE PUT: `undefined` means PRESERVE (absent wrapper), which
 * covers an untouched configured week, an untouched unconfigured week and a typed-but-unconfigured
 * timezone. Only a deliberate Configure (or a real change to a configured week) is serialized.
 */
export function buildWeeklyScheduleEdit(
  schedule: EditorWeeklySchedule | null,
  state: WeekFormState,
): WeeklyScheduleEdit | undefined {
  if (state.kind === "unconfigured") return undefined;
  if (!weekDirty(schedule, state)) return undefined;
  return { timeZoneId: state.timeZoneId.trim(), days: dayEdits(state) };
}
