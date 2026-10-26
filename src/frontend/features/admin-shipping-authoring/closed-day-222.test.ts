/**
 * TB-222 focused facts: a CLOSED day's bounds are untouchable at the HANDLER boundary, not merely greyed.
 *
 * These are the exact model functions the week editor dispatches, so a programmatic or assistive change
 * cannot write hours into a day the operator marked closed, and toggling closed preserves the remembered
 * bounds exactly (the day keeps its ISO weekday and its stored time text).
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  configureWeek,
  weekDirty,
  withDayTime,
  withDayWorking,
  withTimeZone,
  type WeekFormState,
} from "./model/editor-week.ts";

const ZONE = "Asia/Tehran";
const SATURDAY = 6;

function configured(): WeekFormState {
  return withTimeZone(configureWeek({ kind: "unconfigured", pendingTimeZone: ZONE }), ZONE);
}

function day(state: WeekFormState, weekday: number) {
  if (state.kind !== "configured") throw new Error("expected a configured week");
  const found = state.days.find((candidate) => candidate.weekday === weekday);
  if (!found) throw new Error(`weekday ${weekday} is absent`);
  return found;
}

test("222-01 a closed day refuses a start/end edit and returns the SAME state", () => {
  const week = configured();
  // Configured days start closed; bounds are typed only after the day is marked as working.
  assert.equal(withDayTime(week, SATURDAY, "startTime", "09:00"), week);
  assert.equal(withDayTime(week, SATURDAY, "endTime", "17:00"), week);
  assert.equal(day(week, SATURDAY).startTime, "");
});

test("222-02 an unknown weekday is refused instead of creating a day", () => {
  const week = configured();
  assert.equal(withDayTime(week, 9, "endTime", "10:00"), week);
});

test("222-03 closing preserves both remembered bounds and reopening restores them unchanged", () => {
  const week = configured();
  const open = withDayWorking(week, SATURDAY, true);
  const typed = withDayTime(withDayTime(open, SATURDAY, "startTime", "08:30"), SATURDAY, "endTime", "12:45");
  assert.notEqual(typed, open);

  const closed = withDayWorking(typed, SATURDAY, false);
  assert.equal(day(closed, SATURDAY).startTime, "08:30");
  assert.equal(day(closed, SATURDAY).endTime, "12:45");
  assert.equal(day(closed, SATURDAY).weekday, SATURDAY, "the day identity never changes on toggle");

  const reopened = withDayWorking(closed, SATURDAY, true);
  assert.equal(day(reopened, SATURDAY).startTime, "08:30");
  assert.equal(day(reopened, SATURDAY).endTime, "12:45");
  assert.equal(weekDirty(null, reopened), true, "reopened work still counts as real local work");
});
