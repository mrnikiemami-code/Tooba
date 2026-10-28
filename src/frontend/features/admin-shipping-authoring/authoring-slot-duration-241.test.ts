/**
 * TB-241 — receiving slots in the ADMIN authoring surface.
 *
 * The operator configures ONE explicit length per method. The local form must:
 * - start from the real stored value (absent/null = one full-day window, never 60 minutes);
 * - treat a changed length as real dirty work and serialize it as a real number;
 * - keep an untouched schedule (including legacy null) as "preserve" (no wrapper at all);
 * - refuse an out-of-range programmatic value instead of normalising it;
 * - decode only a real 1..1440 integer or explicit null from the server payload.
 */
import assert from "node:assert/strict";
import test from "node:test";

import type { EditorWeeklySchedule } from "./model/editor-document.ts";
import {
  buildWeeklyScheduleEdit,
  configureWeek,
  createWeekForm,
  weekDirty,
  weekIssues,
  withPendingTimeZone,
  withSlotDuration,
} from "./model/editor-week.ts";

function schedule(slotDurationMinutes?: number | null): EditorWeeklySchedule {
  return {
    timeZoneId: "Asia/Tehran",
    days: [1, 2, 3, 4, 5, 6, 7].map((weekday) => ({
      weekday,
      isWorking: weekday <= 5,
      startTime: weekday <= 5 ? "09:00:00" : null,
      endTime: weekday <= 5 ? "17:00:00" : null,
    })),
    ...(slotDurationMinutes === undefined ? {} : { slotDurationMinutes }),
  };
}

test("a stored slot length is the initial state; a legacy schedule stays explicitly null", () => {
  const hourly = createWeekForm(schedule(60));
  assert.equal(hourly.kind, "configured");
  assert.equal(hourly.kind === "configured" ? hourly.slotDurationMinutes : "n/a", 60);

  const legacy = createWeekForm(schedule());
  assert.equal(legacy.kind === "configured" ? legacy.slotDurationMinutes : "n/a", null);
});

test("an untouched legacy week is not rewritten just because the control exists", () => {
  const stored = schedule();
  assert.equal(weekDirty(stored, createWeekForm(stored)), false);
  assert.equal(buildWeeklyScheduleEdit(stored, createWeekForm(stored)), undefined);
});

test("changing only the slot length is real work and serializes the exact number", () => {
  const stored = schedule();
  const state = withSlotDuration(createWeekForm(stored), 90);
  assert.equal(weekDirty(stored, state), true);
  const edit = buildWeeklyScheduleEdit(stored, state);
  assert.ok(edit);
  assert.equal(edit!.slotDurationMinutes, 90);
  assert.equal(edit!.days.length, 7);
  assert.equal(edit!.timeZoneId, "Asia/Tehran");
});

test("removing the slot length is an explicit null, not an omission", () => {
  const stored = schedule(60);
  const state = withSlotDuration(createWeekForm(stored), null);
  assert.equal(weekDirty(stored, state), true);
  const edit = buildWeeklyScheduleEdit(stored, state);
  assert.ok(edit);
  assert.equal(edit!.slotDurationMinutes, null);
});

test("an out-of-range length is refused instead of normalised", () => {
  const state = createWeekForm(schedule());
  for (const bad of [0, -5, 1441, 1.5, Number.NaN]) {
    assert.equal(withSlotDuration(state, bad), state, String(bad));
  }
  assert.equal(withSlotDuration(state, 1).kind === "configured" ? withSlotDuration(state, 1) !== state : "n/a", true);
  assert.equal(withSlotDuration(state, 1440) !== state, true);
});

test("a newly configured week starts with one full-day window, never an invented hour", () => {
  const configured = configureWeek(withPendingTimeZone(createWeekForm(null), "Asia/Tehran"));
  assert.equal(configured.kind, "configured");
  assert.equal(configured.kind === "configured" ? configured.slotDurationMinutes : "n/a", null);
  assert.deepEqual(weekIssues(configured).filter((issue) => issue.code === "slotDurationInvalid"), []);
});
