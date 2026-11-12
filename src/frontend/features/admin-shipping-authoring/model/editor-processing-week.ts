/**
 * TB-264-R1: local WAREHOUSE-PROCESSING calendar of ONE shipping method.
 *
 * This is the third independent calendar of the method. It never inherits the customer delivery week
 * or the carrier acceptance week: preparation is counted inside these hours only, so an operator who
 * wants 24/7 work must say so explicitly instead of relying on an undocumented default.
 *
 * Rules mirror the other weekly editors (no new policy): exactly seven unique ISO weekdays, a working
 * day requires an explicit `open < close` pair, a closed day keeps remembered hours or none, the time
 * zone text is trimmed and length-checked, and there is no clear operation.
 */
import type { EditorProcessingSchedule, EditorScheduleDay, TimeText } from "./editor-document.ts";
import type { ProcessingScheduleEdit, ScheduleDayEdit } from "./editor-edits.ts";
import {
  TIME_ZONE_MAX_LENGTH,
  WEEKDAYS,
  canonicalScheduleTimes,
  timeValue,
  weekIssues,
  type ScheduleDayDraft,
  type WeekFormState,
} from "./editor-week.ts";

/** One processing day draft: `opensAt`/`closesAt` are the stored `TimeText` or an `HH:mm` edit. */
export interface ProcessingDayDraft {
  readonly weekday: number;
  readonly isWorking: boolean;
  readonly opensAt: string;
  readonly closesAt: string;
  /** Per-bound provenance: true only for the bound this operator actually typed. */
  readonly opensEdited: boolean;
  readonly closesEdited: boolean;
}

export type ProcessingWeekFormState =
  | { readonly kind: "unconfigured"; readonly pendingTimeZone: string }
  | {
      readonly kind: "configured";
      readonly timeZoneId: string;
      readonly days: readonly ProcessingDayDraft[];
    };

function dayDraft(day: EditorScheduleDay): ProcessingDayDraft {
  return {
    weekday: day.weekday,
    isWorking: day.isWorking,
    opensAt: day.startTime ?? "",
    closesAt: day.endTime ?? "",
    opensEdited: false,
    closesEdited: false,
  };
}

/** Initial state: the real stored calendar, or an explicit unconfigured state with no time zone. */
export function createProcessingWeekForm(schedule: EditorProcessingSchedule | null): ProcessingWeekFormState {
  if (schedule === null) return { kind: "unconfigured", pendingTimeZone: "" };
  return { kind: "configured", timeZoneId: schedule.timeZoneId, days: schedule.days.map(dayDraft) };
}

/** Operator typed a time zone for an UNCONFIGURED calendar (still not configured). */
export function withProcessingPendingTimeZone(
  state: ProcessingWeekFormState,
  value: string,
): ProcessingWeekFormState {
  return state.kind === "unconfigured" ? { ...state, pendingTimeZone: value } : state;
}

/** Deliberate Configure: seven CLOSED days, blank hours, the operator-supplied time zone only. */
export function configureProcessingWeek(state: ProcessingWeekFormState): ProcessingWeekFormState {
  if (state.kind === "configured") return state;
  return {
    kind: "configured",
    timeZoneId: state.pendingTimeZone,
    days: WEEKDAYS.map((weekday) => ({
      weekday,
      isWorking: false,
      opensAt: "",
      closesAt: "",
      opensEdited: false,
      closesEdited: false,
    })),
  };
}

/** Local reset of the processing draft back to the confirmed stored calendar. */
export function resetProcessingWeek(schedule: EditorProcessingSchedule | null): ProcessingWeekFormState {
  return createProcessingWeekForm(schedule);
}

export function withProcessingTimeZone(state: ProcessingWeekFormState, value: string): ProcessingWeekFormState {
  return state.kind === "configured" ? { ...state, timeZoneId: value } : state;
}

function replaceDay(
  state: ProcessingWeekFormState,
  weekday: number,
  patch: (day: ProcessingDayDraft) => ProcessingDayDraft,
): ProcessingWeekFormState {
  if (state.kind !== "configured") return state;
  return { ...state, days: state.days.map((day) => (day.weekday === weekday ? patch(day) : day)) };
}

/** Working/closed toggle: BOTH remembered bound texts stay exactly as they are. */
export function withProcessingDayWorking(
  state: ProcessingWeekFormState,
  weekday: number,
  isWorking: boolean,
): ProcessingWeekFormState {
  return replaceDay(state, weekday, (day) => ({ ...day, isWorking }));
}

/**
 * Bound edit of ONE day. The handler boundary refuses a CLOSED day (and an unknown day), so a
 * programmatic change can never modify the hours of a day marked closed.
 */
export function withProcessingDayTime(
  state: ProcessingWeekFormState,
  weekday: number,
  bound: "opensAt" | "closesAt",
  value: string,
): ProcessingWeekFormState {
  if (state.kind !== "configured") return state;
  const target = state.days.find((day) => day.weekday === weekday);
  if (!target || !target.isWorking) return state;
  return replaceDay(state, weekday, (day) =>
    bound === "opensAt"
      ? { ...day, opensAt: value, opensEdited: true }
      : { ...day, closesAt: value, closesEdited: true },
  );
}

/**
 * ONE-TIME starting point: copies the customer delivery hours (time zone + seven days) into this
 * calendar as ordinary local values. Afterwards the three calendars are fully independent — editing
 * one never touches another, and this action is never re-applied automatically.
 *
 * Refused (same state) unless the customer week is configured AND issue-free.
 */
export function copyProcessingWeekFromCustomer(
  state: ProcessingWeekFormState,
  customerWeek: WeekFormState,
): ProcessingWeekFormState {
  if (customerWeek.kind !== "configured") return state;
  if (weekIssues(customerWeek).length > 0) return state;
  const days: ProcessingDayDraft[] = WEEKDAYS.map((weekday) => {
    const day = customerWeek.days.find((candidate): candidate is ScheduleDayDraft => candidate.weekday === weekday);
    if (!day) return { weekday, isWorking: false, opensAt: "", closesAt: "", opensEdited: false, closesEdited: false };
    const times = canonicalScheduleTimes(day);
    return {
      weekday,
      isWorking: day.isWorking,
      opensAt: times.start ?? "",
      closesAt: times.end ?? "",
      opensEdited: false,
      closesEdited: false,
    };
  });
  return { kind: "configured", timeZoneId: customerWeek.timeZoneId, days };
}

/** Why the processing calendar cannot be serialized (localized by the screen). */
export type ProcessingIssueCode =
  | "timeZoneRequired"
  | "timeZoneTooLong"
  | "dayHoursRequired"
  | "dayTimeInvalid"
  | "dayRangeInvalid"
  | "pendingTimeZoneUnapplied"
  /** TB-264-R1: `WorkingWeek` preparation declared with no processing calendar at all. */
  | "workingWeekRequiresProcessingWeek";

export interface ProcessingIssue {
  readonly code: ProcessingIssueCode;
  readonly weekday: number | null;
}

/** All blocking processing issues, or `[]` when the state is serializable. */
export function processingWeekIssues(state: ProcessingWeekFormState): readonly ProcessingIssue[] {
  if (state.kind === "unconfigured") {
    return state.pendingTimeZone.trim().length > 0 ? [{ code: "pendingTimeZoneUnapplied", weekday: null }] : [];
  }
  const issues: ProcessingIssue[] = [];
  const timeZone = state.timeZoneId.trim();
  if (timeZone.length === 0) issues.push({ code: "timeZoneRequired", weekday: null });
  else if (timeZone.length > TIME_ZONE_MAX_LENGTH) issues.push({ code: "timeZoneTooLong", weekday: null });

  const seen = new Set<number>();
  for (const day of state.days) {
    if (!seen.add(day.weekday)) {
      issues.push({ code: "dayRangeInvalid", weekday: day.weekday });
      continue;
    }
    const open = canonicalScheduleTimes({
      weekday: day.weekday,
      isWorking: day.isWorking,
      startTime: day.opensAt,
      endTime: day.closesAt,
      startEdited: day.opensEdited,
      endEdited: day.closesEdited,
    });
    const openPresent = day.opensAt !== "";
    const closePresent = day.closesAt !== "";
    if (day.isWorking) {
      if (!openPresent || !closePresent) {
        issues.push({ code: "dayHoursRequired", weekday: day.weekday });
        continue;
      }
      if (open.start === null || open.end === null) {
        issues.push({ code: "dayTimeInvalid", weekday: day.weekday });
        continue;
      }
      if (timeValue(open.end)! <= timeValue(open.start)!) {
        issues.push({ code: "dayRangeInvalid", weekday: day.weekday });
      }
      continue;
    }
    if (!openPresent && !closePresent) continue;
    if (!openPresent || !closePresent || open.start === null || open.end === null) {
      issues.push({ code: "dayTimeInvalid", weekday: day.weekday });
      continue;
    }
    if (timeValue(open.end)! <= timeValue(open.start)!) {
      issues.push({ code: "dayRangeInvalid", weekday: day.weekday });
    }
  }
  if (seen.size !== WEEKDAYS.length) issues.push({ code: "dayRangeInvalid", weekday: null });
  return issues;
}

function dayEdits(state: Extract<ProcessingWeekFormState, { kind: "configured" }>): readonly ScheduleDayEdit[] {
  return WEEKDAYS.map((weekday): ScheduleDayEdit => {
    const day = state.days.find((candidate) => candidate.weekday === weekday)!;
    const times = canonicalScheduleTimes({
      weekday: day.weekday,
      isWorking: day.isWorking,
      startTime: day.opensAt,
      endTime: day.closesAt,
      startEdited: day.opensEdited,
      endEdited: day.closesEdited,
    });
    return { weekday, isWorking: day.isWorking, startTime: times.start, endTime: times.end };
  });
}

function sameTime(left: TimeText | null, right: TimeText | null): boolean {
  if (left === null || right === null) return left === right;
  return timeValue(left) === timeValue(right);
}

/** True when the local processing calendar carries real work (including a typed pending time zone). */
export function processingWeekDirty(
  schedule: EditorProcessingSchedule | null,
  state: ProcessingWeekFormState,
): boolean {
  if (state.kind === "unconfigured") return state.pendingTimeZone.trim().length > 0;
  if (schedule === null) return true;
  if (schedule.timeZoneId !== state.timeZoneId.trim()) return true;
  if (schedule.days.length !== state.days.length) return true;
  const edits = dayEdits(state);
  return !schedule.days.every((stored) => {
    const local = edits.find((day) => day.weekday === stored.weekday);
    return (
      local !== undefined &&
      local.isWorking === stored.isWorking &&
      sameTime(local.startTime, stored.startTime) &&
      sameTime(local.endTime, stored.endTime)
    );
  });
}

/**
 * The `processingSchedule` intent for the ONE PUT: `undefined` means PRESERVE. Only a deliberate
 * Configure of an unconfigured calendar or a real change to a configured one is serialized.
 */
export function buildProcessingScheduleEdit(
  schedule: EditorProcessingSchedule | null,
  state: ProcessingWeekFormState,
): ProcessingScheduleEdit | undefined {
  if (state.kind === "unconfigured") return undefined;
  if (!processingWeekDirty(schedule, state)) return undefined;
  return { timeZoneId: state.timeZoneId.trim(), days: dayEdits(state) };
}
