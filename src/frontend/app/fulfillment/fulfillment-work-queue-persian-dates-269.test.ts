/**
 * TB-269 — the queue's date display contract.
 *
 * The user's screenshot showed a raw `2026-11-15` handoff date beside Jalali operational deadlines. These cases
 * pin the repair: the queue now speaks the same Persian calendar as the operations page, a date-only commitment
 * stays that calendar day under opposite client zones, an accepted instant keeps its carrier-zone day and time,
 * a clock range keeps its start/end order, and nothing unreadable is ever replaced by a guessed date, a browser
 * zone or a raw wire value.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { execFileSync } from "node:child_process";
import path from "node:path";
import { pathToFileURL } from "node:url";
import {
  formatCustomerReceiving,
  formatOperationalDeadline,
  formatPlannedHandoff,
  formatPreparationDeadlines,
} from "./fulfillment-work-queue-dispatch.ts";

const MODULE_URL = pathToFileURL(path.join(import.meta.dirname, "fulfillment-work-queue-dispatch.ts")).href;

/** Renders the handoff cell in a separate process with an explicit client zone (TZ is read at process start). */
function handoffUnderClientZone(zone: string): string {
  const script = [
    `import { formatPlannedHandoff } from ${JSON.stringify(MODULE_URL)};`,
    "process.stdout.write(formatPlannedHandoff({",
    '  plannedHandoffDate: "2026-11-15",',
    "  plannedHandoffWindow: null,",
    '  plannedHandoffTimeZone: "Asia/Tehran",',
    "}));",
  ].join("\n");
  return execFileSync(process.execPath, ["--experimental-strip-types", "--input-type=module", "-e", script], {
    env: { ...process.env, TZ: zone },
    encoding: "utf8",
  }).trim();
}

/** No operator-visible value may carry a raw ISO date/time or a raw IANA zone id any more. */
function assertNoRawWireValue(value: string): void {
  assert.doesNotMatch(value, /\d{4}-\d{2}-\d{2}/, `raw ISO date leaked: ${value}`);
  assert.doesNotMatch(value, /\d{2}:\d{2}:\d{2}/, `raw ISO time leaked: ${value}`);
  assert.doesNotMatch(value, /[A-Za-z]+\/[A-Za-z_]+/, `raw zone id leaked: ${value}`);
}

test("TB-269: an accepted date-only handoff renders the Persian calendar, not a Gregorian date", () => {
  assert.equal(
    formatPlannedHandoff({
      plannedHandoffDate: "2026-11-15",
      plannedHandoffWindow: null,
      plannedHandoffTimeZone: "Asia/Tehran",
    }),
    "۲۴ آبان ۱۴۰۵ · به وقت تهران",
  );
  // The customer commitment is its own promise and carries its own window, no plan zone. The stored window is
  // kept verbatim (its own separator included); only the digits are Persian.
  assert.equal(
    formatCustomerReceiving({ customerReceivingDate: "2026-11-15", customerReceivingWindow: "09:00-10:00" }),
    "۲۴ آبان ۱۴۰۵ · \u2066۰۹:۰۰-۱۰:۰۰\u2069",
  );
  assert.equal(
    formatCustomerReceiving({ customerReceivingDate: "2026-11-15", customerReceivingWindow: null }),
    "۲۴ آبان ۱۴۰۵",
  );
});

test("TB-269: a date-only commitment is the same calendar day under opposite client time zones", () => {
  // UTC+14 and UTC-11 straddle the whole date line: a UTC/local-midnight conversion would shift one of them.
  const east = handoffUnderClientZone("Pacific/Kiritimati");
  const west = handoffUnderClientZone("Pacific/Midway");
  assert.equal(east, "۲۴ آبان ۱۴۰۵ · به وقت تهران");
  assert.equal(west, east);
});

test("TB-269: an accepted instant crossing UTC midnight keeps its carrier day and time", () => {
  // 2026-11-15T21:30Z is already 2026-11-16 01:00 in Asia/Tehran; the carrier day must not read as the UTC day.
  assert.equal(formatOperationalDeadline("2026-11-15T21:30:00Z", "Asia/Tehran"), "۲۵ آبان ۱۴۰۵، ۰۱:۰۰");
  assert.equal(formatOperationalDeadline("2026-11-15T08:30:00Z", "Asia/Tehran"), "۲۴ آبان ۱۴۰۵، ۱۲:۰۰");
});

test("TB-269: a clock range keeps Persian digits and its own start/end order, isolated LTR", () => {
  const handoff = formatPlannedHandoff({
    plannedHandoffDate: "2026-11-15",
    plannedHandoffWindow: "08:00–20:00",
    plannedHandoffTimeZone: "Asia/Tehran",
  });
  assert.ok(handoff.includes("\u2066۰۸:۰۰–۲۰:۰۰\u2069"), handoff);
  const start = handoff.indexOf("۰۸:۰۰");
  const end = handoff.indexOf("۲۰:۰۰");
  assert.ok(start > 0 && end > start, `range order reversed: ${handoff}`);
  assertNoRawWireValue(handoff);
});

test("TB-269: the deadline cell reads as three Persian instants plus ONE human zone label", () => {
  const text = formatPreparationDeadlines({
    preparationStartUtc: "2026-11-14T09:30:00Z",
    transferDepartureUtc: "2026-11-14T12:20:00Z",
    handoffDeadlineUtc: "2026-11-14T13:00:00Z",
    plannedHandoffTimeZone: "Asia/Tehran",
  });
  assert.ok(text.startsWith("آماده‌سازی تا ۲۳ آبان ۱۴۰۵، ۱۳:۰۰"), text);
  assert.ok(text.includes(" · خروج تا ۲۳ آبان ۱۴۰۵، ۱۵:۵۰"), text);
  assert.ok(text.endsWith("تحویل به حامل تا ۲۳ آبان ۱۴۰۵، ۱۶:۳۰ · به وقت تهران"), text);
  // The zone is printed once per cell, not beside every instant.
  assert.equal(text.split("به وقت تهران").length - 1, 1, text);
  assertNoRawWireValue(text);
});

test("TB-269: a missing promise stays explicitly unavailable and invents nothing", () => {
  assert.equal(
    formatPlannedHandoff({ plannedHandoffDate: null, plannedHandoffWindow: null, plannedHandoffTimeZone: null }),
    "مشخص نشده",
  );
  assert.equal(formatCustomerReceiving({ customerReceivingDate: null, customerReceivingWindow: null }), "—");
  assert.equal(formatOperationalDeadline(null, "Asia/Tehran"), "—");
  assert.equal(
    formatPreparationDeadlines({
      preparationStartUtc: null,
      transferDepartureUtc: null,
      handoffDeadlineUtc: null,
      plannedHandoffTimeZone: "Asia/Tehran",
    }),
    "—",
  );
});

test("TB-269: unreadable input or a refused zone is named, never replaced by a raw value or a browser zone", () => {
  assert.equal(formatPlannedHandoff({
    plannedHandoffDate: "2026-13-45",
    plannedHandoffWindow: "08:00–20:00",
    plannedHandoffTimeZone: "Asia/Tehran",
  }), "تاریخ نامعتبر");
  assert.equal(formatCustomerReceiving({ customerReceivingDate: "not-a-date", customerReceivingWindow: null }), "تاریخ نامعتبر");
  assert.equal(formatOperationalDeadline("not-an-instant", "Asia/Tehran"), "تاریخ نامعتبر");
  assert.equal(formatOperationalDeadline("2026-11-15T08:30:00Z", "Mars/Olympus"), "منطقهٔ زمانی نامعتبر");
  // A refused zone in the handoff cell must not print the raw id beside a correct date.
  const refusedZone = formatPlannedHandoff({
    plannedHandoffDate: "2026-11-15",
    plannedHandoffWindow: null,
    plannedHandoffTimeZone: "Mars/Olympus",
  });
  assert.equal(refusedZone, "۲۴ آبان ۱۴۰۵ · منطقهٔ زمانی نامعتبر");
  assertNoRawWireValue(refusedZone);
  // A missing plan zone is a missing fact, not permission to render in the browser's own zone.
  assert.equal(
    formatOperationalDeadline("2026-11-15T08:30:00Z", null),
    "منطقهٔ زمانی مشخص نشده",
  );
});
