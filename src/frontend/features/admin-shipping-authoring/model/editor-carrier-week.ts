/**
 * TB-256: local CARRIER-ACCEPTANCE calendar of ONE shipping method.
 *
 * This calendar is INDEPENDENT of the customer delivery week: it has its own time zone, its own seven
 * days and its own open/close bounds, and it carries no receiving-slot length. Nothing is inherited
 * implicitly — an unconfigured calendar stays explicitly unconfigured until the operator either types
 * a time zone and configures it, or deliberately copies the customer hours ONCE.
 *
 * Rules mirrored from the weekly schedule (no new policy): exactly seven unique ISO weekdays, a
 * working day requires an explicit `open < close` pair, a closed day keeps its remembered hours or
 * none, the time zone text is trimmed and length-checked, and there is no clear operation.
 *
 * Provenance stays PER BOUND: an untouched stored token is never rewritten, while a deliberate
 * `HH:mm` edit becomes a lawful `HH:mm:00` `TimeText`.
 */
import type { EditorCarrierSchedule, EditorScheduleDay, TimeText } from "./editor-document.ts";
import type { CarrierScheduleEdit, ScheduleDayEdit } from "./editor-edits.ts";
import {
  TIME_ZONE_MAX_LENGTH,
  WEEKDAYS,
  canonicalScheduleTimes,
  timeValue,
  weekIssues,
  type ScheduleDayDraft,
  type WeekFormState,
} from "./editor-week.ts";

/** One carrier day draft: `opensAt`/`closesAt` are the stored `TimeText` or an `HH:mm` edit. */
export interface CarrierDayDraft {
  readonly weekday: number;
  readonly isWorking: boolean;
  readonly opensAt: string;
  readonly closesAt: string;
  /** Per-bound provenance: true only for the bound this operator actually typed. */
  readonly opensEdited: boolean;
  readonly closesEdited: boolean;
}

export type CarrierWeekFormState =
  | { readonly kind: "unconfigured"; readonly pendingTimeZone: string }
  | {
      readonly kind: "configured";
      readonly timeZoneId: string;
      readonly days: readonly CarrierDayDraft[];
    };

function dayDraft(day: EditorScheduleDay): CarrierDayDraft {
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
export function createCarrierWeekForm(schedule: EditorCarrierSchedule | null): CarrierWeekFormState {
  if (schedule === null) return { kind: "unconfigured", pendingTimeZone: "" };
  return {
    kind: "configured",
    timeZoneId: schedule.timeZoneId,
    days: schedule.days.map(dayDraft),
  };
}

/** Operator typed a time zone for an UNCONFIGURED calendar (still not configured). */
export function withCarrierPendingTimeZone(state: CarrierWeekFormState, value: string): CarrierWeekFormState {
  return state.kind === "unconfigured" ? { ...state, pendingTimeZone: value } : state;
}

/** Deliberate Configure: seven CLOSED days, blank hours, the operator-supplied time zone only. */
export function configureCarrierWeek(state: CarrierWeekFormState): CarrierWeekFormState {
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

/** Local reset of the carrier draft back to the confirmed stored calendar. */
export function resetCarrierWeek(schedule: EditorCarrierSchedule | null): CarrierWeekFormState {
  return createCarrierWeekForm(schedule);
}

export function withCarrierTimeZone(state: CarrierWeekFormState, value: string): CarrierWeekFormState {
  return state.kind === "configured" ? { ...state, timeZoneId: value } : state;
}

function replaceDay(
  state: CarrierWeekFormState,
  weekday: number,
  patch: (day: CarrierDayDraft) => CarrierDayDraft,
): CarrierWeekFormState {
  if (state.kind !== "configured") return state;
  return { ...state, days: state.days.map((day) => (day.weekday === weekday ? patch(day) : day)) };
}

/** Working/closed toggle: BOTH remembered bound texts stay exactly as they are. */
export function withCarrierDayWorking(
  state: CarrierWeekFormState,
  weekday: number,
  isWorking: boolean,
): CarrierWeekFormState {
  return replaceDay(state, weekday, (day) => ({ ...day, isWorking }));
}

/**
 * Bound edit of ONE day. The handler boundary refuses a CLOSED day (and an unknown day), so a
 * programmatic change can never modify the hours of a day marked closed; the disabled inputs are
 * presentation, not the protection. A refused edit returns the SAME state.
 */
export function withCarrierDayTime(
  state: CarrierWeekFormState,
  weekday: number,
  bound: "opensAt" | "closesAt",
  value: string,
): CarrierWeekFormState {
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
 * calendar as ordinary local values. Afterwards the two calendars are fully independent — editing one
 * never touches the other, and this action is never re-applied automatically.
 *
 * Refused (same state) unless the customer week is configured AND issue-free, so an uninspectable or
 * unserializable week can never be laundered into carrier acceptance.
 */
export function copyCarrierWeekFromCustomer(
  state: CarrierWeekFormState,
  customerWeek: WeekFormState,
): CarrierWeekFormState {
  if (customerWeek.kind !== "configured") return state;
  if (weekIssues(customerWeek).length > 0) return state;
  const days: CarrierDayDraft[] = WEEKDAYS.map((weekday) => {
    const day = customerWeek.days.find((candidate): candidate is ScheduleDayDraft => candidate.weekday === weekday);
    if (!day) return { weekday, isWorking: false, opensAt: "", closesAt: "", opensEdited: false, closesEdited: false };
    const times = canonicalScheduleTimes(day);
    return {
      weekday,
      isWorking: day.isWorking,
      // The exact lawful token is kept verbatim (provenance untouched), never re-spelled here.
      opensAt: times.start ?? "",
      closesAt: times.end ?? "",
      opensEdited: false,
      closesEdited: false,
    };
  });
  return { kind: "configured", timeZoneId: customerWeek.timeZoneId, days };
}

/** Why the carrier calendar cannot be serialized (localized by the screen). */
export type CarrierIssueCode =
  | "timeZoneRequired"
  | "timeZoneTooLong"
  | "dayHoursRequired"
  | "dayTimeInvalid"
  | "dayRangeInvalid"
  | "pendingTimeZoneUnapplied";

export interface CarrierIssue {
  readonly code: CarrierIssueCode;
  readonly weekday: number | null;
}

/** All blocking carrier issues, or `[]` when the state is serializable. */
export function carrierWeekIssues(state: CarrierWeekFormState): readonly CarrierIssue[] {
  if (state.kind === "unconfigured") {
    // Typed-but-unconfigured is real local work with no lawful wire shape: Apply stays blocked.
    return state.pendingTimeZone.trim().length > 0 ? [{ code: "pendingTimeZoneUnapplied", weekday: null }] : [];
  }
  const issues: CarrierIssue[] = [];
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

function dayEdits(state: Extract<CarrierWeekFormState, { kind: "configured" }>): readonly ScheduleDayEdit[] {
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
    return {
      weekday,
      isWorking: day.isWorking,
      startTime: times.start,
      endTime: times.end,
    };
  });
}

function sameTime(left: TimeText | null, right: TimeText | null): boolean {
  if (left === null || right === null) return left === right;
  return timeValue(left) === timeValue(right);
}

/** True when the local carrier calendar carries real work (including a typed pending time zone). */
export function carrierWeekDirty(schedule: EditorCarrierSchedule | null, state: CarrierWeekFormState): boolean {
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
 * The `carrierSchedule` intent for the ONE PUT: `undefined` means PRESERVE. Only a deliberate
 * Configure of an unconfigured calendar or a real change to a configured one is serialized.
 */
export function buildCarrierScheduleEdit(
  schedule: EditorCarrierSchedule | null,
  state: CarrierWeekFormState,
): CarrierScheduleEdit | undefined {
  if (state.kind === "unconfigured") return undefined;
  if (!carrierWeekDirty(schedule, state)) return undefined;
  return { timeZoneId: state.timeZoneId.trim(), days: dayEdits(state) };
}
