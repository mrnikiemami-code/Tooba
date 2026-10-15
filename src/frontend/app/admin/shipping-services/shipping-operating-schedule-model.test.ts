/**
 * TB-SHIPPING-FULL-OPERATIONS-072 — focused guards for the weekly operating-schedule editor model:
 * ISO weekday identity (یکشنبه = ۷), unambiguous 24-hour parsing, the `HH:mm:ss` wire shape, the
 * `[start,end)` overlap rule that accepts adjacency, capacity disclosure and the stable API-code
 * mapping. No server is touched.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  CAPACITY_CONFIG_ONLY_FA,
  DEFAULT_TIME_ZONE_ID,
  MAX_WINDOW_CAPACITY,
  NO_SCHEDULE_DISCLOSURE_FA,
  SERVICE_LEVEL_SCHEDULE_FA,
  SHIPPING_SCHEDULE_ISSUE_CODES,
  buildScheduleWriteRequest,
  emptyWindowRow,
  formatWindowFa,
  isIsoWeekday,
  mapScheduleErrorFa,
  parseLocalTime,
  scheduleOptionKey,
  scheduleOptionLabelFa,
  scheduleStateFa,
  sortWindowRows,
  summarizeSchedules,
  toEditableTime,
  toWireTime,
  validateScheduleOption,
  validateShippingScheduleForm,
  weekdayLabelFa,
  windowRowsFromSchedule,
  type ShippingWindowFormRow,
} from "./shipping-operating-schedule-model.ts";
import type { ShippingOperatingScheduleAdminItem } from "./shipping-operating-schedule-api.ts";

let seq = 0;
function row(
  weekday: number,
  startTime: string,
  endTime: string,
  capacity = "",
): ShippingWindowFormRow {
  seq += 1;
  return { key: `t${seq}`, weekday: weekday as ShippingWindowFormRow["weekday"], startTime, endTime, capacity };
}

const codes = (issues: { code: string }[]) => issues.map((issue) => issue.code);

test("ISO weekday identity: Sunday is 7 and every Persian label maps to its ISO number", () => {
  assert.equal(weekdayLabelFa(7), "یکشنبه");
  assert.equal(weekdayLabelFa(6), "شنبه");
  assert.equal(weekdayLabelFa(1), "دوشنبه");
  assert.equal(weekdayLabelFa(5), "جمعه");
  // یک روز ناشناخته با خود عدد نمایش داده می‌شود، نه با برچسب ساختگی.
  assert.equal(weekdayLabelFa(9), "روز 9");
  assert.equal(isIsoWeekday(7), true);
  assert.equal(isIsoWeekday(0), false);
  assert.equal(isIsoWeekday(8), false);
  assert.equal(isIsoWeekday(1.5), false);
});

test("24-hour parsing is strict and never guesses a time from a malformed value", () => {
  assert.equal(parseLocalTime("13:00"), 13 * 60);
  assert.equal(parseLocalTime(" 23:59 "), 23 * 60 + 59);
  assert.equal(parseLocalTime("00:00"), 0);
  assert.equal(parseLocalTime("24:00"), null);
  assert.equal(parseLocalTime("13:60"), null);
  assert.equal(parseLocalTime("1:00"), null);
  assert.equal(parseLocalTime(""), null);
  assert.equal(parseLocalTime(null), null);
});

test("the wire shape is HH:mm:ss and the editable shape is HH:mm, both round-tripping", () => {
  assert.equal(toWireTime("13:00"), "13:00:00");
  assert.equal(toWireTime("09:05"), "09:05:00");
  assert.equal(toWireTime("24:00"), null);
  assert.equal(toEditableTime("13:00:00"), "13:00");
  assert.equal(toEditableTime("13:00"), "13:00");
  // یک مقدار ناخوانا فیلد را خالی می‌گذارد؛ «00:00» جعلی ساخته نمی‌شود.
  assert.equal(toEditableTime("not-a-time"), "");
  assert.equal(toEditableTime(null), "");
});

test("a real Sunday 13:00-14:00 window plus a second window is valid and serialises correctly", () => {
  const windows = [row(7, "13:00", "14:00"), row(7, "16:30", "18:00", "25")];
  assert.deepEqual(validateShippingScheduleForm({ timeZoneId: DEFAULT_TIME_ZONE_ID, windows }), []);

  const body = buildScheduleWriteRequest({
    serviceCode: "tipax",
    timeZoneId: DEFAULT_TIME_ZONE_ID,
    optionCode: "standard",
    windows,
  });
  assert.deepEqual(body, {
    serviceCode: "tipax",
    timeZoneId: "Asia/Tehran",
    optionCode: "standard",
    windows: [
      { weekday: 7, startTime: "13:00:00", endTime: "14:00:00", capacity: null },
      { weekday: 7, startTime: "16:30:00", endTime: "18:00:00", capacity: 25 },
    ],
  });
});

test("adjacent windows are accepted while overlapping windows are refused", () => {
  // بازهٔ نیم‌باز: 13:00–14:00 و 14:00–15:00 چسبیده‌اند و هم‌پوشانی ندارند.
  const adjacent = [row(7, "13:00", "14:00"), row(7, "14:00", "15:00")];
  assert.deepEqual(validateShippingScheduleForm({ timeZoneId: "Asia/Tehran", windows: adjacent }), []);

  const overlapping = [row(7, "13:00", "15:00"), row(7, "14:00", "16:00")];
  assert.deepEqual(
    codes(validateShippingScheduleForm({ timeZoneId: "Asia/Tehran", windows: overlapping })),
    [SHIPPING_SCHEDULE_ISSUE_CODES.overlap],
  );

  // یک روز دیگر هم‌پوشانی نمی‌سازد، حتی با بازهٔ کاملاً یکسان.
  const differentDays = [row(7, "13:00", "14:00"), row(1, "13:00", "14:00")];
  assert.deepEqual(validateShippingScheduleForm({ timeZoneId: "Asia/Tehran", windows: differentDays }), []);
});

test("reversed, zero-length and overnight windows are refused with the range meaning", () => {
  for (const windows of [
    [row(7, "14:00", "13:00")],
    [row(7, "13:00", "13:00")],
    [row(7, "22:00", "02:00")],
  ]) {
    assert.ok(
      codes(validateShippingScheduleForm({ timeZoneId: "Asia/Tehran", windows }))
        .includes(SHIPPING_SCHEDULE_ISSUE_CODES.rangeInvalid),
    );
  }
});

test("invalid weekday, invalid time, invalid capacity and empty form are refused locally", () => {
  assert.ok(
    codes(validateShippingScheduleForm({ timeZoneId: "Asia/Tehran", windows: [row(0, "13:00", "14:00")] }))
      .includes(SHIPPING_SCHEDULE_ISSUE_CODES.weekdayInvalid),
  );
  assert.ok(
    codes(validateShippingScheduleForm({ timeZoneId: "Asia/Tehran", windows: [row(7, "25:00", "14:00")] }))
      .includes(SHIPPING_SCHEDULE_ISSUE_CODES.timeInvalid),
  );
  for (const capacity of ["0", "-1", "abc", "1.5", String(MAX_WINDOW_CAPACITY + 1)]) {
    assert.ok(
      codes(validateShippingScheduleForm({ timeZoneId: "Asia/Tehran", windows: [row(7, "13:00", "14:00", capacity)] }))
        .includes(SHIPPING_SCHEDULE_ISSUE_CODES.capacityInvalid),
      capacity,
    );
  }
  assert.deepEqual(
    codes(validateShippingScheduleForm({ timeZoneId: "Asia/Tehran", windows: [] })),
    [SHIPPING_SCHEDULE_ISSUE_CODES.windowsRequired],
  );
  assert.deepEqual(
    codes(validateShippingScheduleForm({ timeZoneId: "  ", windows: [row(7, "13:00", "14:00")] })),
    [SHIPPING_SCHEDULE_ISSUE_CODES.timeZoneRequired],
  );
});

test("option identity: only real options are accepted and the service level is the explicit fallback", () => {
  const options = [{ code: "standard", labelFa: "معمولی" }, { code: "express", labelFa: "پیشتاز" }];
  assert.deepEqual(validateScheduleOption(null, options), []);
  assert.deepEqual(validateScheduleOption("standard", options), []);
  assert.deepEqual(
    codes(validateScheduleOption("drone", options)),
    [SHIPPING_SCHEDULE_ISSUE_CODES.optionUnknown],
  );
  assert.deepEqual(
    codes(validateScheduleOption("standard", [])),
    [SHIPPING_SCHEDULE_ISSUE_CODES.optionUnknown],
  );

  assert.equal(scheduleOptionLabelFa(null, options), "سطح خدمت (پیش‌فرض خدمت)");
  assert.equal(scheduleOptionLabelFa("standard", options), "معمولی");
  // کد ناشناخته با خود کد نمایش داده می‌شود تا هویت پنهان نماند.
  assert.equal(scheduleOptionLabelFa("drone", options), "drone");
  assert.equal(scheduleOptionKey(null), "");
  assert.equal(scheduleOptionKey("standard"), "standard");
});

function schedule(overrides: Partial<ShippingOperatingScheduleAdminItem>): ShippingOperatingScheduleAdminItem {
  return {
    scheduleId: "s1",
    serviceCode: "tipax",
    optionCode: "standard",
    timeZoneId: "Asia/Tehran",
    isActive: true,
    windows: [],
    configuredCapacityTotal: null,
    createdAt: "2026-10-15T04:57:06+00:00",
    updatedAt: "2026-10-15T04:57:06+00:00",
    ...overrides,
  };
}

test("a stored schedule becomes editable rows and keeps the server order", () => {
  const rows = windowRowsFromSchedule(schedule({
    windows: [
      { windowId: "w1", weekday: 7, startTime: "13:00:00", endTime: "14:00:00", capacity: null },
      { windowId: "w2", weekday: 7, startTime: "16:30:00", endTime: "18:00:00", capacity: 25 },
    ],
  }));
  assert.equal(rows.length, 2);
  assert.deepEqual(rows.map((r) => [r.weekday, r.startTime, r.endTime, r.capacity]), [
    [7, "13:00", "14:00", ""],
    [7, "16:30", "18:00", "25"],
  ]);
});

test("sorting is stable by day then start, and an incomplete row never sorts above a complete one", () => {
  const sorted = sortWindowRows([row(1, "09:00", "10:00"), row(7, "16:30", "18:00"), row(7, "", "")]);
  assert.deepEqual(sorted.map((r) => [r.weekday, r.startTime]), [[1, "09:00"], [7, "16:30"], [7, ""]]);
});

test("capacity is displayed as configured-only, never as remaining or reserved", () => {
  const withCapacity = formatWindowFa({ windowId: "w", weekday: 7, startTime: "16:30:00", endTime: "18:00:00", capacity: 25 });
  assert.match(withCapacity, /16:30–18:00/);
  assert.match(withCapacity, /ظرفیت تنظیم‌شده/);
  assert.doesNotMatch(withCapacity, /باقی/);
  assert.doesNotMatch(withCapacity, /رزرو/);

  const withoutCapacity = formatWindowFa({ windowId: "w", weekday: 7, startTime: "13:00:00", endTime: "14:00:00", capacity: null });
  assert.equal(withoutCapacity, "13:00–14:00");

  assert.equal(CAPACITY_CONFIG_ONLY_FA, "ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)");
});

test("an empty schedule set is a real answer and its disclosure never reads as a checkout block", () => {
  assert.equal(scheduleStateFa([]), "هیچ برنامهٔ عملیاتی ثبت نشده — یعنی هیچ ساعت صریحی برای تحویل به حامل تعیین نشده است.");
  assert.match(NO_SCHEDULE_DISCLOSURE_FA, /Checkout/);
  assert.match(NO_SCHEDULE_DISCLOSURE_FA, /نمی‌بندد/);
  assert.match(SERVICE_LEVEL_SCHEDULE_FA, /سطح خدمت/);
});

test("summaries count schedules, active rows, windows and configured capacity without inventing values", () => {
  assert.deepEqual(summarizeSchedules([]), {
    scheduleCount: 0,
    activeCount: 0,
    windowCount: 0,
    configuredCapacityTotal: null,
  });
  const summary = summarizeSchedules([
    schedule({
      scheduleId: "a",
      optionCode: "standard",
      isActive: true,
      configuredCapacityTotal: 25,
      windows: [
        { windowId: "w1", weekday: 7, startTime: "13:00:00", endTime: "14:00:00", capacity: null },
        { windowId: "w2", weekday: 7, startTime: "16:30:00", endTime: "18:00:00", capacity: 25 },
      ],
    }),
    schedule({ scheduleId: "b", optionCode: null, isActive: false, configuredCapacityTotal: null, windows: [] }),
  ]);
  assert.deepEqual(summary, {
    scheduleCount: 2,
    activeCount: 1,
    windowCount: 2,
    configuredCapacityTotal: 25,
  });
});

test("stable API failure codes map to Persian messages and unknown codes fall back", () => {
  assert.match(mapScheduleErrorFa("shipping.schedule.window.overlap", "x"), /هم‌پوشانی/);
  assert.match(mapScheduleErrorFa("shipping.validation.schedule_window_range_invalid", "x"), /پایان/);
  assert.match(mapScheduleErrorFa("shipping.service_option.unknown", "x"), /گزینه/);
  assert.match(mapScheduleErrorFa("admin.authorization.denied", "x"), /دسترسی/);
  assert.equal(mapScheduleErrorFa("something.else", "پیام پیش‌فرض"), "پیام پیش‌فرض");
  assert.equal(mapScheduleErrorFa(null, "پیام پیش‌فرض"), "پیام پیش‌فرض");
});

test("an empty new row never fabricates a weekday or a time", () => {
  const empty = emptyWindowRow();
  assert.equal(empty.startTime, "");
  assert.equal(empty.endTime, "");
  assert.equal(empty.capacity, "");
  assert.equal(empty.weekday, 7);
  assert.notEqual(empty.key, emptyWindowRow().key);
});
