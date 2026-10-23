/**
 * Local WEEKLY-SCHEDULE intent of ONE shipping method (shared by every region group).
 *
 * A configured schedule initializes from the real stored timezone + seven ISO days (1 = Monday).
 * An UNCONFIGURED schedule stays explicitly unconfigured until the operator types a timezone and
 * deliberately clicks Configure: only then do seven CLOSED days with blank hours appear, and no
 * browser timezone, default working hours or fabricated persisted configuration is invented.
 *
 * Rules mirrored from the shipping domain (no new policy):
 * - exactly seven unique ISO weekdays;
 * - a working day requires an explicit `start < end` pair (overnight is not a legal pair);
 * - a closed day keeps its remembered hours, or none;
 * - the timezone text is trimmed and must be non-empty within the stored length limit;
 * - there is no clear operation for a configured schedule (absent/edit only).
 *
 * Hours are kept as the exact stored token until the operator actually edits that day; a deliberate
 * `HH:mm` edit becomes a lawful `HH:mm:00` `TimeText` at serialization time only.
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
  /** True once this day's hours were deliberately edited (a `HH:mm` value, not a stored token). */
  readonly hoursEdited: boolean;
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

function dayDraft(day: EditorScheduleDay): ScheduleDayDraft {
  return {
    weekday: day.weekday,
    isWorking: day.isWorking,
    startTime: day.startTime ?? "",
    endTime: day.endTime ?? "",
    hoursEdited: false,
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
      hoursEdited: false,
    })),
  };
}

/** Local reset of a configured draft back to its confirmed stored schedule. */
export function resetWeek(schedule: EditorWeeklySchedule | null): WeekFormState {
  return createWeekForm(schedule);
}

export function withTimeZone(state: WeekFormState, value: string): WeekFormState {
  return state.kind === "configured" ? { ...state, timeZoneId: value } : state;
}

function replaceDay(state: WeekFormState, weekday: number, patch: (day: ScheduleDayDraft) => ScheduleDayDraft): WeekFormState {
  if (state.kind !== "configured") return state;
  return { ...state, days: state.days.map((day) => (day.weekday === weekday ? patch(day) : day)) };
}

/** Working/closed toggle: remembered hours stay exactly as they are. */
export function withDayWorking(state: WeekFormState, weekday: number, isWorking: boolean): WeekFormState {
  return replaceDay(state, weekday, (day) => ({ ...day, isWorking }));
}

/** A deliberate `HH:mm` (or blank) edit of one bound; marks the day's hours as operator-edited. */
export function withDayTime(
  state: WeekFormState,
  weekday: number,
  bound: "startTime" | "endTime",
  value: string,
): WeekFormState {
  return replaceDay(state, weekday, (day) => ({ ...day, [bound]: value, hoursEdited: true }));
}

/** Why the local week cannot be serialized (localized by the screen). */
export type WeekIssueCode =
  | "timeZoneRequired"
  | "timeZoneTooLong"
  | "dayHoursRequired"
  | "dayTimeInvalid"
  | "dayRangeInvalid";

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
  if (state.kind === "unconfigured") return [];
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
    const start = canonicalTime(day.startTime, day.hoursEdited);
    const end = canonicalTime(day.endTime, day.hoursEdited);
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
      if (end <= start) issues.push({ code: "dayRangeInvalid", weekday: day.weekday });
      continue;
    }
    if (!startPresent && !endPresent) continue;
    if (!startPresent || !endPresent || start === null || end === null) {
      issues.push({ code: "dayTimeInvalid", weekday: day.weekday });
      continue;
    }
    if (end <= start) issues.push({ code: "dayRangeInvalid", weekday: day.weekday });
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
      startTime: canonicalTime(day.startTime, day.hoursEdited),
      endTime: canonicalTime(day.endTime, day.hoursEdited),
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
      local.startTime === stored.startTime &&
      local.endTime === stored.endTime
    );
  });
}

/** True when the local week differs from the confirmed schedule (or was deliberately configured). */
export function weekDirty(schedule: EditorWeeklySchedule | null, state: WeekFormState): boolean {
  if (state.kind === "unconfigured") return false;
  if (schedule === null) return true;
  if (schedule.timeZoneId !== state.timeZoneId.trim()) return true;
  return !sameDays(schedule, state);
}

/**
 * The `weeklySchedule` intent for the ONE PUT: `undefined` means PRESERVE (absent wrapper), which
 * covers an untouched configured week and an untouched unconfigured week. An unconfigured draft that
 * was never deliberately configured stays omitted; there is no clear operation.
 */
export function buildWeeklyScheduleEdit(
  schedule: EditorWeeklySchedule | null,
  state: WeekFormState,
): WeeklyScheduleEdit | undefined {
  if (state.kind === "unconfigured") return undefined;
  if (!weekDirty(schedule, state)) return undefined;
  return { timeZoneId: state.timeZoneId.trim(), days: dayEdits(state) };
}
