import assert from "node:assert/strict";
import test from "node:test";
import {
  combineTehranDayAndTime,
  parseStoryScheduleDraft,
  STORY_SCHEDULE_TIME_ZONE,
} from "./story-schedule-time.ts";

test("Jalali-picked Gregorian day + Tehran clock converts independent of browser zone label", () => {
  assert.equal(STORY_SCHEDULE_TIME_ZONE, "Asia/Tehran");
  const iso = combineTehranDayAndTime("2025-11-19", "10:30");
  assert.ok(iso);
  // 10:30 Tehran in standard +03:30 => 07:00Z
  assert.equal(iso, "2025-11-19T07:00:00.000Z");
});

test("absent end means no end; invalid start keeps draft and refuses send", () => {
  const ok = parseStoryScheduleDraft({
    startDayIso: "2025-11-19",
    startTime: "09:00",
    endDayIso: "",
    endTime: "",
  });
  assert.equal(ok.ok, true);
  if (ok.ok) assert.equal(ok.endAt, null);

  const bad = parseStoryScheduleDraft({
    startDayIso: "",
    startTime: "09:00",
    endDayIso: "",
    endTime: "",
  });
  assert.equal(bad.ok, false);
  if (!bad.ok) assert.equal(bad.field, "startAt");
});
