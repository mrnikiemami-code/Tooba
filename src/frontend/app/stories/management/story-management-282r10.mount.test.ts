/**
 * TB-282-R10 — publication window + status copy + schedule clock (unit; no AppDataGrid CSS import).
 */
import assert from "node:assert/strict";
import { test } from "node:test";
import { faStoryCopy } from "./story-management-copy";
import { formatStoryScheduleClock, parseOptionalPublicationWindow } from "./story-schedule-time";

test("B: optional publication window Jalali/Tehran roundtrip and end validation", () => {
  const ok = parseOptionalPublicationWindow({
    startDayIso: "2026-11-05",
    startTime: "16:00",
    endDayIso: "2026-12-09",
    endTime: "18:00",
  });
  assert.equal(ok.ok, true);
  if (ok.ok) {
    assert.ok(ok.startAt);
    assert.ok(ok.endAt);
    assert.ok(Date.parse(ok.endAt!) > Date.parse(ok.startAt!));
    const clock = formatStoryScheduleClock(ok.startAt!, "fa");
    assert.ok(clock.includes("/"));
    assert.ok(/\d{2}:\d{2}/.test(clock));
  }
  const bad = parseOptionalPublicationWindow({
    startDayIso: "2026-11-05",
    startTime: "16:00",
    endDayIso: "2026-11-05",
    endTime: "15:00",
  });
  assert.equal(bad.ok, false);
  const empty = parseOptionalPublicationWindow({
    startDayIso: "",
    startTime: "",
    endDayIso: "",
    endTime: "",
  });
  assert.equal(empty.ok, true);
  if (empty.ok) {
    assert.equal(empty.startAt, null);
    assert.equal(empty.endAt, null);
  }
});

test("D: one Persian awaiting term shared by filter/status/submit", () => {
  assert.equal(faStoryCopy.filterPending, "در انتظار تأیید");
  assert.equal(faStoryCopy.displayAwaiting, "در انتظار تأیید");
  assert.equal(faStoryCopy.submit, "ارسال برای تأیید");
  assert.equal(faStoryCopy.rowSubmit, "ارسال برای تأیید");
  assert.equal(faStoryCopy.durationLabel.includes("عکس"), true);
  assert.ok(faStoryCopy.publicationWindowHeading);
  assert.ok(faStoryCopy.deleteStory);
  assert.ok(faStoryCopy.mediaRequiredHint);
});
