import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  PREVIEW_PLACES,
  PREVIEW_SAMPLE_DAY_HOURS,
  PREVIEW_WEEKDAY_IDS,
  clonePreviewMethod,
  previewDayHoursFieldErrors,
  previewDayHoursIssue,
  previewMethodSelectedPlaces,
  previewNewMethod,
  previewPlaceAncestors,
  previewPlaceCandidateState,
  previewPlaceConflict,
  previewPlacePath,
  previewRangeError,
  previewRowFromRawInputs,
  previewRowInputErrors,
  previewRowInputsAreValid,
  previewSampleSchedule,
  previewScheduleActiveDays,
  previewScheduleIsValid,
  previewScheduleSegments,
  previewScheduleWithDay,
  previewTextFor,
  previewTranslationStates,
  previewTranslationSummary,
  type PreviewMethod,
  type PreviewRowRawInputs,
} from "./preview-model.ts";
import { PREVIEW_METHOD_FIXTURES } from "./preview-fixtures.ts";
import {
  describePreviewRange,
  describePreviewScheduleSummary,
  formatPreviewMoney,
  previewMessagesEn,
  previewMessagesFa,
  previewMessages,
  previewFormat,
} from "./preview-i18n.ts";

const here = path.dirname(fileURLToPath(import.meta.url));

test("PREVIEW-186: duplicate and parent/child selections are blocked within the same method", () => {
  // ایران اضافه شده ⇒ هیچ زیرمجموعه‌ای دوباره قابل اضافه شدن نیست.
  assert.deepEqual(previewPlaceConflict(["place-ir"], "place-ir-23"), { kind: "covers", placeId: "place-ir" });
  assert.deepEqual(previewPlaceConflict(["place-ir"], "place-ir-23-thr-1"), { kind: "covers", placeId: "place-ir" });

  // استان تهران اضافه شده ⇒ شهر/ناحیهٔ زیر آن ممنوع.
  assert.equal(previewPlaceConflict(["place-ir-23"], "place-ir-23-thr")?.kind, "covers");

  // گلپایگان اضافه شده ⇒ استان اصفهان و ایران ممنوع، شهر مستقل دیگر مجاز.
  assert.equal(previewPlaceConflict(["place-ir-10-glp"], "place-ir-10")?.kind, "covered");
  assert.equal(previewPlaceConflict(["place-ir-10-glp"], "place-ir")?.kind, "covered");
  assert.equal(previewPlaceConflict(["place-ir-10-glp"], "place-ir-25-qom"), null);

  // تکرار دقیق ممنوع است.
  assert.equal(previewPlaceConflict(["place-ir-25-qom"], "place-ir-25-qom")?.kind, "duplicate");
});

test("PREVIEW-186: explicit ancestry is the only conflict source", () => {
  // گلپایگان فرزند اصفهان است، نه تهران — هیچ استنتاجی از برچسب انجام نمی‌شود.
  assert.deepEqual(previewPlaceAncestors("place-ir-10-glp"), ["place-ir-10", "place-ir"]);
  assert.equal(previewPlaceConflict(["place-ir-23"], "place-ir-10-glp"), null);
  assert.ok(PREVIEW_PLACES.every((place) => place.parentId === null || PREVIEW_PLACES.some((p) => p.id === place.parentId)));
});

test("PREVIEW-186: readable geographic path instead of technical codes", () => {
  assert.deepEqual(previewPlacePath("place-ir-23-thr-1", "fa"), ["ایران", "تهران", "تهران", "ناحیه ۱ تهران"]);
  assert.deepEqual(previewPlacePath("place-ir-10-glp", "en"), ["Iran", "Isfahan", "Golpayegan"]);
});

test("PREVIEW-186: locale fixtures are independent and never synthesized", () => {
  const method = PREVIEW_METHOD_FIXTURES.find((row) => row.id === "method-express");
  assert.ok(method);
  const edited = { ...method, names: { ...method.names, en: "Edited English only" } };
  assert.equal(previewTextFor(edited.names, "fa"), method.names.fa);
  assert.equal(previewTextFor(edited.names, "en"), "Edited English only");

  const states = previewTranslationStates({ fa: "نام", en: "   " });
  assert.equal(states.find((state) => state.languageId === "fa")?.translated, true);
  assert.equal(states.find((state) => state.languageId === "en")?.translated, false);
  assert.deepEqual(previewTranslationSummary({ fa: "نام", en: "" }), { translated: 1, missing: 1, total: 2 });
});

test("PREVIEW-186: clone isolates the in-memory draft from the fixture", () => {
  const source = PREVIEW_METHOD_FIXTURES[0]!;
  const copy = clonePreviewMethod(source);
  (copy.names as Record<string, string>).fa = "changed";
  copy.regionGroups[0]!.rows[0]!.freight = 1;
  assert.notEqual(source.names.fa, "changed");
  assert.notEqual(source.regionGroups[0]!.rows[0]!.freight, 1);
});

test("PREVIEW-186: range shape errors are explicit and bounds stay inclusive/exclusive", () => {
  assert.equal(previewRangeError({ from: 2, to: 1 }), "inverted");
  assert.equal(previewRangeError({ from: 2, to: 2 }), "inverted");
  assert.equal(previewRangeError({ from: -1, to: null }), "negative");
  assert.equal(previewRangeError({ from: 1, to: 2 }), null);
  assert.equal(previewRangeError({ from: null, to: null }), null);
  assert.equal(previewRangeError({ from: 1.5, to: 2 }, { integer: true }), "notInteger");
});

test("PREVIEW-186: locale-aware money, weight and range wording", () => {
  assert.equal(formatPreviewMoney(850000, "fa"), `${new Intl.NumberFormat("fa-IR").format(850000)} ریال`);
  assert.equal(formatPreviewMoney(850000, "en"), "IRR 850,000");
  assert.equal(describePreviewRange({ from: 1000, to: 2000 }, "en", "weight"), "1 kg to under 2 kg");
  assert.equal(describePreviewRange(null, "en", "weight"), previewMessagesEn.noCondition);
  assert.equal(describePreviewRange(null, "fa", "days"), previewMessagesFa.dash);
  assert.equal(previewFormat(previewMessagesFa.translationSummary, { translated: 1, total: 2 }).includes("2"), true);
});

test("PREVIEW-186: every fixture group uses existing places and non-overlapping row examples", () => {
  for (const method of PREVIEW_METHOD_FIXTURES) {
    for (const group of method.regionGroups) {
      assert.ok(group.places.length > 0, `${method.id}/${group.id} must show at least one region`);
      for (const placeId of group.places) {
        assert.ok(PREVIEW_PLACES.some((place) => place.id === placeId), `${placeId} must exist in the fixture`);
      }
      // ردیف‌های نمایشی نباید ترکیب کامل شرط یکسانی داشته باشند.
      const keys = group.rows.map((row) => JSON.stringify([row.weightGrams, row.itemCount, row.merchandiseAmount]));
      assert.equal(new Set(keys).size, keys.length, `${method.id}/${group.id} has identical row conditions`);
      assert.equal(group.rows.length >= 1, true);
    }
    assert.equal(previewMethodSelectedPlaces(method).length, method.regionGroups.reduce((total, group) => total + group.places.length, 0));
  }
});

test("PREVIEW-186: both dictionaries are complete and no Persian fallback leaks into English", () => {
  assert.deepEqual(Object.keys(previewMessagesEn).sort(), Object.keys(previewMessagesFa).sort());
  for (const [key, value] of Object.entries(previewMessagesEn)) {
    assert.ok(value.trim().length > 0, `${key} must not be empty in en`);
    assert.equal(/[\u0600-\u06FF]/.test(value), false, `${key} must not contain Persian text in en`);
  }
  assert.equal(previewMessages("fa").tabGeneral, previewMessagesFa.tabGeneral);
});

test("PREVIEW-186: preview code performs no network, storage or business writes", () => {
  const files = fs.readdirSync(here).filter((name) => /\.(ts|tsx)$/.test(name) && !name.endsWith(".test.ts"));
  const forbidden = [
    /\bfetch\s*\(/,
    /XMLHttpRequest/,
    /localStorage/,
    /sessionStorage/,
    /document\.cookie/,
    /method:\s*["'](POST|PUT|PATCH|DELETE)["']/i,
    /hostBaseUrl|buildUpstreamAuthHeaders/,
  ];
  const stripComments = (source: string): string =>
    source.replace(/\/\*[\s\S]*?\*\//g, "").replace(/(^|[^:])\/\/.*$/gm, "$1");
  const violations: string[] = [];
  for (const name of files) {
    const source = stripComments(fs.readFileSync(path.join(here, name), "utf8"));
    for (const pattern of forbidden) {
      if (pattern.test(source)) violations.push(`${name} -> ${pattern}`);
    }
  }
  assert.deepEqual(violations, []);
});

/* —————————————————— TB-186-R1 regression coverage —————————————————— */

const EMPTY_ROW_RAW: PreviewRowRawInputs = {
  weightFromKg: "",
  weightToKg: "",
  countFrom: "",
  countTo: "",
  amountFrom: "",
  amountTo: "",
  freight: "0",
  packaging: "0",
  deliveryMin: "1",
  deliveryMax: "3",
};

test("PREVIEW-186-R1: browsing an ancestor is not adding it", () => {
  // گلپایگان انتخاب شده ⇒ ایران/اصفهان افزودنی نیست، اما پیمایش از آن‌ها برای رسیدن به قم آزاد است.
  const iran = previewPlaceCandidateState(["place-ir-10-glp"], "place-ir");
  assert.equal(iran.addable, false);
  assert.equal(iran.conflict?.kind, "covered");

  const isfahan = previewPlaceCandidateState(["place-ir-10-glp"], "place-ir-10");
  assert.equal(isfahan.addable, false);

  // مسیر عادی: ایران → قم (استان) → قم (شهر) → افزودن.
  assert.equal(previewPlaceCandidateState(["place-ir-10-glp"], "place-ir-25").addable, true);
  const qomCity = previewPlaceCandidateState(["place-ir-10-glp"], "place-ir-25-qom");
  assert.equal(qomCity.addable, true);
  assert.equal(qomCity.conflict, null);

  // استان تهران انتخاب شده ⇒ پیمایش به شهر/ناحیه‌اش هرگز افزودنی نیست.
  assert.equal(previewPlaceCandidateState(["place-ir-23"], "place-ir-23-thr").addable, false);
  assert.equal(previewPlaceCandidateState(["place-ir-23"], "place-ir-23-thr-1").addable, false);
});

test("PREVIEW-186-R1: conflict domain spans groups of the same method, not other owners", () => {
  const method: PreviewMethod = clonePreviewMethod(PREVIEW_METHOD_FIXTURES[0]!);
  method.regionGroups = [
    { id: "group-a", titles: { fa: "", en: "" }, places: ["place-ir-25-qom"], rows: [] },
    { id: "group-b", titles: { fa: "", en: "" }, places: [], rows: [] },
  ];
  const selected = previewMethodSelectedPlaces(method);
  // تکرار همان شهر در گروه دوم مسدود است.
  assert.equal(previewPlaceCandidateState(selected, "place-ir-25-qom").addable, false);
  // والدِ آن در گروه دوم هم مسدود است (پوشش کل زیرمجموعه).
  assert.equal(previewPlaceCandidateState(selected, "place-ir-25").addable, false);
  // کشور دیگر/شهر بی‌تعارض آزاد است.
  assert.equal(previewPlaceCandidateState(selected, "place-ir-23-thr").addable, true);
});

test("PREVIEW-186-R1: the first submit rejects min > max delivery days from raw input", () => {
  const errors = previewRowInputErrors({ ...EMPTY_ROW_RAW, deliveryMin: "5", deliveryMax: "2" });
  assert.equal(errors.delivery, "inverted");
  assert.equal(previewRowInputsAreValid(errors), false);

  // تصحیح همان ورودی، همان کلیک بعدی را مجاز می‌کند.
  const fixed = previewRowInputErrors({ ...EMPTY_ROW_RAW, deliveryMin: "2", deliveryMax: "5" });
  assert.equal(previewRowInputsAreValid(fixed), true);
  assert.deepEqual(previewRowFromRawInputs({ ...EMPTY_ROW_RAW, deliveryMin: "2", deliveryMax: "5" }, "row-1").delivery, {
    minDays: 2,
    maxDays: 5,
  });
});

test("PREVIEW-186-R1: integer and numeric validation happens on raw values before rounding", () => {
  assert.equal(previewRowInputErrors({ ...EMPTY_ROW_RAW, countFrom: "1.5" }).count, "notInteger");
  assert.equal(previewRowInputErrors({ ...EMPTY_ROW_RAW, countTo: "2.5" }).count, "notInteger");
  assert.equal(previewRowInputErrors({ ...EMPTY_ROW_RAW, amountFrom: "-3" }).amount, "negative");
  assert.equal(previewRowInputErrors({ ...EMPTY_ROW_RAW, amountFrom: "abc" }).amount, "notNumber");
  assert.equal(previewRowInputErrors({ ...EMPTY_ROW_RAW, weightFromKg: "abc" }).weight, "notNumber");
  assert.equal(previewRowInputErrors({ ...EMPTY_ROW_RAW, deliveryMin: "2.5" }).delivery, "notInteger");
  assert.equal(previewRowInputErrors({ ...EMPTY_ROW_RAW, deliveryMax: "-1" }).delivery, "negative");
  assert.equal(previewRowInputErrors({ ...EMPTY_ROW_RAW, countFrom: "4", countTo: "4" }).count, "inverted");
  assert.equal(previewRowInputsAreValid(previewRowInputErrors(EMPTY_ROW_RAW)), true, "empty axes carry no condition");

  // کیلوگرم → گرم با همان دقت پشتیبانی‌شده حفظ می‌شود؛ مقدار خام غیرصحیح برای تعداد گرد نمی‌شود.
  const row = previewRowFromRawInputs(
    { ...EMPTY_ROW_RAW, weightFromKg: "1.234", weightToKg: "2", countFrom: "1", countTo: "4", freight: "50", packaging: "5" },
    "row-2",
  );
  assert.deepEqual(row.weightGrams, { from: 1234, to: 2000 });
  assert.deepEqual(row.itemCount, { from: 1, to: 4 });
  assert.equal(row.freight, 50);
  assert.equal(row.packaging, 5);
});

test("PREVIEW-186-R1: the picker never disables options and both tab panels stay mounted", () => {
  const picker = fs.readFileSync(path.join(here, "preview-region-picker.tsx"), "utf8");
  // رگرسیون اصلی: غیرفعال‌کردن گزینه‌ها مسیر پیمایش را می‌بست.
  assert.equal(/<option[^>]*\bdisabled\b/.test(picker), false);
  assert.ok(picker.includes("previewPlaceCandidateState"));

  const screen = fs.readFileSync(path.join(here, "design-preview-screen.tsx"), "utf8");
  assert.ok(screen.includes('hidden={tab !== "general"}'));
  assert.ok(screen.includes('hidden={tab !== "regions"}'));
  assert.ok(screen.includes("discardAndChangeOwner"));
});

/* —————————————————— TB-186-R2 per-weekday schedule coverage —————————————————— */

const SAMPLE_SCHEDULE_METHOD = PREVIEW_METHOD_FIXTURES.find((method) => method.id === "method-express");

test("PREVIEW-186-R2: every weekday is configured independently", () => {
  const sample = SAMPLE_SCHEDULE_METHOD;
  assert.ok(sample, "the opening sample method must exist");
  // نمونهٔ درخواست مالک در روش SAMPLE: شنبه ۰۹:۰۰–۱۳:۰۰، یکشنبه ۱۵:۰۰–۱۸:۰۰، جمعه تعطیل.
  assert.deepEqual(
    { working: sample.schedule.hours.sat.working, from: sample.schedule.hours.sat.fromTime, to: sample.schedule.hours.sat.toTime },
    { working: true, from: "09:00", to: "13:00" },
  );
  assert.deepEqual(
    { working: sample.schedule.hours.sun.working, from: sample.schedule.hours.sun.fromTime, to: sample.schedule.hours.sun.toTime },
    { working: true, from: "15:00", to: "18:00" },
  );
  assert.equal(sample.schedule.hours.fri.working, false);

  // تغییر یک روز فقط همان روز را تغییر می‌دهد؛ هیچ بازهٔ مشترکی نوشته نمی‌شود.
  const draft = clonePreviewMethod(sample);
  const mondayBefore = { ...draft.schedule.hours.mon };
  const sundayBefore = { ...draft.schedule.hours.sun };
  const changed = previewScheduleWithDay(draft.schedule, "sun", {
    working: true,
    fromTime: "16:30",
    toTime: "19:00",
  });
  assert.deepEqual(changed.hours.sun, { working: true, fromTime: "16:30", toTime: "19:00" });
  assert.deepEqual(changed.hours.sat, draft.schedule.hours.sat);
  assert.deepEqual(changed.hours.mon, mondayBefore);
  assert.notDeepEqual(changed.hours.sun, sundayBefore);
  assert.equal(changed.cutoffHours, draft.schedule.cutoffHours);
});

test("PREVIEW-186-R2: cloning isolates schedule across methods, drafts and owners", () => {
  const source = PREVIEW_METHOD_FIXTURES[0]!;
  const copy = clonePreviewMethod(source);
  const neighbor = clonePreviewMethod(source);
  (copy.schedule.hours.sat as { fromTime: string }).fromTime = "07:15";
  assert.equal(source.schedule.hours.sat.fromTime, "09:00");
  assert.equal(neighbor.schedule.hours.sat.fromTime, "09:00");

  // روش تازه هم نمونهٔ مستقل می‌گیرد؛ دو روش تازه به یک آبجکت اشاره نمی‌کنند.
  const freshA = previewNewMethod("method-new-a", "marketplace");
  const freshB = previewNewMethod("method-new-b", "seller");
  (freshA.schedule.hours.fri as { working: boolean }).working = true;
  assert.equal(freshB.schedule.hours.fri.working, false);
  assert.equal(previewSampleSchedule().hours.fri.working, false);
  // پیش‌فرض روش تازه صریح و نمونه است، نه بازهٔ مشترک استنتاج‌شده.
  assert.deepEqual(
    { working: freshA.schedule.hours.sat.working, from: freshA.schedule.hours.sat.fromTime, to: freshA.schedule.hours.sat.toTime },
    { working: true, from: PREVIEW_SAMPLE_DAY_HOURS.fromTime, to: PREVIEW_SAMPLE_DAY_HOURS.toTime },
  );
});

test("PREVIEW-186-R2: a closed day has no operating interval and keeps its typed hours", () => {
  const sample = clonePreviewMethod(SAMPLE_SCHEDULE_METHOD!);
  // روز تعطیل هرگز خطا نمی‌دهد، حتی با ساعت خالی یا معکوس.
  assert.equal(previewDayHoursIssue({ working: false, fromTime: "", toTime: "" }), null);
  assert.equal(previewDayHoursIssue({ working: false, fromTime: "18:00", toTime: "09:00" }), null);

  // خاموش‌کردن یک روز ساعت تایپ‌شده‌اش را پاک نمی‌کند؛ روشن‌کردن دوباره همان ساعت را برمی‌گرداند.
  const closed = previewScheduleWithDay(sample.schedule, "sat", {
    working: false,
    fromTime: sample.schedule.hours.sat.fromTime,
    toTime: sample.schedule.hours.sat.toTime,
  });
  assert.deepEqual(closed.hours.sat, { working: false, fromTime: "09:00", toTime: "13:00" });
  assert.deepEqual(previewScheduleActiveDays(closed), previewScheduleActiveDays(sample.schedule).filter((day) => day !== "sat"));

  // روز تعطیل در خلاصهٔ صادقانه جدا گزارش می‌شود.
  const segments = previewScheduleSegments(closed);
  const satSegment = segments.find((segment) => segment.days.includes("sat"));
  assert.equal(satSegment?.kind, "closed");
  assert.equal(segments.some((segment) => segment.kind === "open" && segment.days.includes("sat")), false);

  // همهٔ روزها تعطیل: معتبر است، اما خلاصه صریحاً «هیچ بازهٔ عملیاتی» می‌گوید.
  const allClosed = previewScheduleWithDay(clonePreviewMethod(PREVIEW_METHOD_FIXTURES[0]!).schedule, "sat", {
    working: false,
    fromTime: "09:00",
    toTime: "13:00",
  });
  const everyClosed = PREVIEW_WEEKDAY_IDS.reduce(
    (schedule, day) => previewScheduleWithDay(schedule, day, { ...schedule.hours[day], working: false }),
    allClosed,
  );
  assert.deepEqual(previewScheduleActiveDays(everyClosed), []);
  assert.equal(previewScheduleIsValid(everyClosed), true);
  assert.equal(describePreviewScheduleSummary(everyClosed, "en"), previewMessagesEn.scheduleAllClosed);
});

test("PREVIEW-186-R2: invalid first Apply is rejected synchronously and never rewritten", () => {
  const base = clonePreviewMethod(SAMPLE_SCHEDULE_METHOD!).schedule;

  // شروع مساوی/بعد از پایان: خطا روی همان روز، بدون معکوس‌کردن یا کشیدن به روز بعد.
  const inverted = previewScheduleWithDay(base, "sun", { working: true, fromTime: "18:00", toTime: "18:00" });
  assert.equal(previewScheduleIsValid(inverted), false);
  assert.equal(previewDayHoursIssue(inverted.hours.sun), "notBefore");
  assert.deepEqual(inverted.hours.sun, { working: true, fromTime: "18:00", toTime: "18:00" });

  const backwards = previewScheduleWithDay(base, "wed", { working: true, fromTime: "17:00", toTime: "09:00" });
  assert.equal(previewDayHoursIssue(backwards.hours.wed), "notBefore");
  assert.deepEqual(backwards.hours.wed, { working: true, fromTime: "17:00", toTime: "09:00" });

  // ساعت خالی یا نامعتبر هم رد می‌شود و مقدار تایپ‌شده دست‌نخورده می‌ماند.
  const missing = previewScheduleWithDay(base, "mon", { working: true, fromTime: "", toTime: "18:00" });
  assert.equal(previewDayHoursIssue(missing.hours.mon), "missing");
  assert.equal(previewDayHoursFieldErrors(missing.hours.mon).start, "missing");
  assert.equal(previewScheduleIsValid(missing), false);

  const malformed = previewScheduleWithDay(base, "tue", { working: true, fromTime: "9", toTime: "18:00" });
  assert.equal(previewDayHoursIssue(malformed.hours.tue), "notTime");
  assert.equal(previewScheduleIsValid(malformed), false);

  // تصحیح همان روز، اعتبار را برمی‌گرداند.
  const fixed = previewScheduleWithDay(malformed, "tue", { working: true, fromTime: "10:00", toTime: "14:00" });
  assert.equal(previewScheduleIsValid(fixed), true);
  assert.equal(previewDayHoursIssue(fixed.hours.tue), null);
  // خطای یک روز، بقیهٔ روزها را نامعتبر نمی‌کند.
  assert.equal(previewDayHoursIssue(inverted.hours.sat), null);
});

test("PREVIEW-186-R2: the collapsed summary never fakes one shared interval", () => {
  const sample = SAMPLE_SCHEDULE_METHOD!;
  const fa = describePreviewScheduleSummary(sample.schedule, "fa");
  const en = describePreviewScheduleSummary(sample.schedule, "en");

  // ساعت شنبه و یکشنبه با هم متفاوت‌اند و هر دو در خلاصه دیده می‌شوند.
  assert.ok(en.includes("09:00–13:00"), en);
  assert.ok(en.includes("15:00–18:00"), en);
  assert.ok(en.includes(previewMessagesEn.weekdaySat), en);
  assert.ok(en.includes(previewMessagesEn.weekdaySun), en);
  assert.ok(/Friday closed|Thu.*Friday closed/.test(en), en);
  assert.ok(en.includes("order cutoff"), en);
  assert.equal(fa.includes("09:00"), false, "fa summary must format Persian digits, not raw digits");
  assert.ok(/[۰-۹]/.test(fa), fa);

  // روزهای پیوسته با ساعت یکسان به یک قطعه جمع می‌شوند، اما ساعت متفاوت ادغام نمی‌شود.
  const segments = previewScheduleSegments(sample.schedule);
  const openSegments = segments.filter((segment) => segment.kind === "open");
  assert.ok(openSegments.length >= 3, "different hours must stay in separate segments");
  for (const segment of openSegments) {
    if (segment.kind !== "open") continue;
    for (const day of segment.days) {
      assert.equal(sample.schedule.hours[day].fromTime, segment.fromTime);
      assert.equal(sample.schedule.hours[day].toTime, segment.toTime);
    }
  }
});

test("PREVIEW-186-R2: the weekly editor exposes per-day controls and cannot mutate in View mode", () => {
  const editor = fs.readFileSync(path.join(here, "preview-schedule-editor.tsx"), "utf8");
  assert.ok(editor.includes("PREVIEW_WEEKDAY_IDS.map"));
  assert.ok(editor.includes("shipping-preview-day-${day}-working"));
  assert.ok(editor.includes("shipping-preview-day-${day}-from"));
  assert.ok(editor.includes("shipping-preview-day-${day}-to"));
  assert.ok(editor.includes("disabled={readOnly}"));
  // هیچ ورودی بازهٔ مشترک قبلی باقی نمانده است.
  assert.equal(/schedule\.weekdays/.test(editor), false);

  const generalTab = fs.readFileSync(path.join(here, "preview-general-tab.tsx"), "utf8");
  assert.ok(generalTab.includes("describePreviewScheduleSummary"));
  assert.equal(/schedule\.weekdays/.test(generalTab), false);
  assert.equal(/weekday-\$\{day\}/.test(generalTab), false);

  const screen = fs.readFileSync(path.join(here, "design-preview-screen.tsx"), "utf8");
  assert.ok(screen.includes("previewScheduleIsValid"));
  assert.ok(screen.includes("scheduleApplyBlocked"));
});
