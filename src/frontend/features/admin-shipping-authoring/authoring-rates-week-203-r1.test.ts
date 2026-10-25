import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import type { EditorDocument, EditorRateRow, EditorWeeklySchedule } from "./model/editor-document.ts";
import {
  buildWeeklyScheduleEdit,
  configureWeek,
  createWeekForm,
  weekDirty,
  weekIssues,
  withDayTime,
  withDayWorking,
  withPendingTimeZone,
  withTimeZone,
  timeValue,
  WEEKDAYS,
} from "./model/editor-week.ts";
import { createRatesForm, withAddedRow, withRowField } from "./model/editor-rates.ts";
import { createEditorForm, withName } from "./model/editor-form.ts";
import { buildAuthoringIntent, hasNoIssues } from "./model/authoring-intent.ts";
import { resetAuthoringDrafts, seedAuthoringDrafts } from "./model/authoring-drafts.ts";
import { authoringMessages } from "./messages/authoring-messages.ts";
import type { RateRowDraft } from "./model/editor-rates.ts";
import { rateConditionText, rateDraftConditionText } from "./ui/rate-presentation.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const FA = "44444444-4444-4444-8444-444444444441";
const EN = "55555555-5555-4555-8555-555555555551";
const G1 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";
const R1 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1";
const CITY = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";

const messages = authoringMessages("en");

function row(overrides: Partial<EditorRateRow> = {}): EditorRateRow {
  return {
    rateRowId: R1,
    minWeightGrams: 0,
    maxWeightGrams: 5000,
    minItemCount: null,
    maxItemCount: null,
    minMerchandiseAmount: "0.50",
    maxMerchandiseAmount: null,
    freightAmount: "1850000.00",
    packagingAmount: "0.00",
    minDeliveryDays: 1,
    maxDeliveryDays: 3,
    ...overrides,
  };
}

function schedule(): EditorWeeklySchedule {
  return {
    timeZoneId: "Asia/Tehran",
    days: [1, 2, 3, 4, 5, 6, 7].map((weekday) =>
      weekday <= 5
        ? {
            weekday,
            isWorking: true,
            startTime: weekday === 1 ? "09:30:00.2500000" : "09:30:00",
            endTime: weekday === 1 ? "18:00:00.5000000" : "18:00:00",
          }
        : { weekday, isWorking: false, startTime: null, endTime: null },
    ),
  };
}

function documentWith(overrides: Partial<EditorDocument> = {}): EditorDocument {
  const language = (languageId: string, code: string, index: number) => ({
    languageId,
    code,
    culture: code === "fa" ? "fa-IR" : "en-US",
    urlPrefix: code === "fa" ? "" : code,
    direction: code === "fa" ? ("rtl" as const) : ("ltr" as const),
    displayName: code,
    nativeName: code,
    isDefault: index === 0,
    sortOrder: index + 1,
  });
  return {
    draftId: DRAFT,
    owner: { kind: "Store", sellerPartyId: null },
    serviceCode: "tooba.post",
    optionCode: null,
    revision: 4,
    createdAt: "2026-10-23T00:15:00+03:30",
    updatedAt: "2026-10-23T01:45:10+03:30",
    type: { draftId: DRAFT, revision: 4, shippingTypeId: null, resolution: "Unconfigured", shippingType: null },
    settings: null,
    presentation: {
      methodTranslations: [{ languageId: FA, name: "پست", description: null }],
      groupTitles: [],
      contentLanguages: [language(FA, "fa", 0), language(EN, "en", 1)],
      defaultContentLanguageId: FA,
      hasDefaultLanguageName: true,
    },
    weeklySchedule: schedule(),
    regionGroups: [
      {
        groupId: G1,
        order: 0,
        currencyCode: "IRR",
        areas: [{ kind: "City", id: CITY }],
        rows: [row()],
        titles: [{ languageId: FA, title: "تهران" }],
      },
    ],
    destinationFacts: [
      {
        kind: "City",
        id: CITY,
        resolution: "Resolved",
        label: "Tehran",
        area: {
          kind: "City",
          countryId: "dddddddd-dddd-4ddd-8ddd-ddddddddddd1",
          administrativeAreaId: null,
          cityId: CITY,
          serviceAreaId: null,
        },
      },
    ],
    ...overrides,
  };
}

test("R203-01 editing only the start keeps the exact untouched fractional end", () => {
  const document = documentWith();
  const base = createWeekForm(document.weeklySchedule);
  const startOnly = withDayTime(base, 1, "startTime", "10:00");

  assert.deepEqual(weekIssues(startOnly), [], "one-bound editing is not a broken pair");
  const write = buildWeeklyScheduleEdit(document.weeklySchedule, startOnly)!;
  const monday = write.days.find((day) => day.weekday === 1)!;
  assert.equal(monday.startTime, "10:00:00");
  assert.equal(monday.endTime, "18:00:00.5000000", "the untouched bound keeps its exact token");
});

test("R203-02 editing only the end keeps the exact untouched start", () => {
  const document = documentWith();
  const endOnly = withDayTime(createWeekForm(document.weeklySchedule), 1, "endTime", "16:45");
  assert.deepEqual(weekIssues(endOnly), []);
  const monday = buildWeeklyScheduleEdit(document.weeklySchedule, endOnly)!.days.find((day) => day.weekday === 1)!;
  assert.equal(monday.startTime, "09:30:00.2500000");
  assert.equal(monday.endTime, "16:45:00");
});

test("R203-03 work/closed toggle alone preserves BOTH hour texts", () => {
  const document = documentWith();
  const base = createWeekForm(document.weeklySchedule);
  const toggled = withDayWorking(base, 1, false);
  const monday = buildWeeklyScheduleEdit(document.weeklySchedule, toggled)!.days.find((day) => day.weekday === 1)!;
  assert.equal(monday.isWorking, false);
  assert.equal(monday.startTime, "09:30:00.2500000");
  assert.equal(monday.endTime, "18:00:00.5000000");

  // Re-opening the day keeps the remembered pair, so the draft is back to the stored baseline.
  const reopened = withDayWorking(toggled, 1, true);
  assert.equal(buildWeeklyScheduleEdit(document.weeklySchedule, reopened), undefined, "no phantom edit");
  assert.equal(weekDirty(document.weeklySchedule, reopened), false);
  const reopenedMonday = reopened.kind === "configured" ? reopened.days.find((day) => day.weekday === 1)! : null;
  assert.equal(reopenedMonday!.startTime, "09:30:00.2500000");
  assert.equal(reopenedMonday!.endTime, "18:00:00.5000000");
});

test("R203-04 range checks compare real time values, and a zero interval stays invalid", () => {
  assert.equal(timeValue("18:00:00"), timeValue("18:00:00.0000000"));
  assert.equal(timeValue("09:30:00.2500000")! > timeValue("09:30:00")!, true);

  const document = documentWith();
  const base = createWeekForm(document.weeklySchedule);
  // 18:00:00 as the start with an untouched fractional end is a REVERSED pair by value.
  const reversed = withDayTime(base, 1, "startTime", "19:00");
  assert.deepEqual(weekIssues(reversed), [{ code: "dayRangeInvalid", weekday: 1 }]);

  // Equal spelling-equivalent bounds are a zero interval, not a lawful pair.
  const saturday = withDayWorking(base, 6, true);
  const equal = withDayTime(withDayTime(saturday, 6, "startTime", "08:00"), 6, "endTime", "08:00");
  assert.deepEqual(weekIssues(equal), [{ code: "dayRangeInvalid", weekday: 6 }]);

  const partial = withDayWorking(withDayTime(base, 7, "startTime", "09:00"), 7, true);
  assert.deepEqual(weekIssues(partial), [{ code: "dayHoursRequired", weekday: 7 }]);

  const malformed = withDayTime(base, 1, "startTime", "9:5");
  assert.ok(weekIssues(malformed).some((issue) => issue.code === "dayTimeInvalid"));
});

test("R203-05 a genuine valid pair change is serialized as canonical HH:mm:00", () => {
  const document = documentWith();
  const base = createWeekForm(document.weeklySchedule);
  const changed = withDayTime(withDayTime(base, 2, "startTime", "08:15"), 2, "endTime", "13:45");
  assert.deepEqual(weekIssues(changed), []);
  const tuesday = buildWeeklyScheduleEdit(document.weeklySchedule, changed)!.days.find((day) => day.weekday === 2)!;
  assert.equal(tuesday.startTime, "08:15:00");
  assert.equal(tuesday.endTime, "13:45:00");
  // Every other day is untouched and precision-retaining.
  const wednesday = buildWeeklyScheduleEdit(document.weeklySchedule, changed)!.days.find((day) => day.weekday === 3)!;
  assert.equal(wednesday.startTime, "09:30:00");
});

test("R203-06 pending unconfigured timezone is dirty local work, never a configured week", () => {
  const document = documentWith({ weeklySchedule: null });
  const empty = createWeekForm(null);
  assert.equal(weekDirty(null, empty), false);
  assert.deepEqual(weekIssues(empty), []);

  const typed = withPendingTimeZone(empty, "Typed/Zone");
  assert.equal(weekDirty(null, typed), true, "typed text is real local work");
  assert.deepEqual(weekIssues(typed), [{ code: "pendingTimeZoneUnapplied", weekday: null }]);
  assert.equal(buildWeeklyScheduleEdit(null, typed), undefined, "no wrapper and no fabricated days");

  const intent = buildAuthoringIntent(document, createEditorForm(document), createRatesForm(document), typed);
  assert.equal(intent.localDirty, true);
  assert.equal(hasNoIssues(intent.issues), false, "Apply is blocked until configure-or-clear");
  assert.equal(intent.edits.weeklySchedule, undefined);

  // Reverting the text to empty is clean again.
  assert.equal(weekDirty(null, withPendingTimeZone(typed, "")), false);

  // Configure still creates only seven CLOSED local days from the explicit timezone.
  const configured = configureWeek(typed);
  assert.equal(configured.kind, "configured");
  if (configured.kind !== "configured") throw new Error("unreachable");
  assert.deepEqual(configured.days.map((day) => day.weekday), [...WEEKDAYS]);
  assert.ok(configured.days.every((day) => !day.isWorking && day.startTime === "" && day.endTime === ""));
  assert.deepEqual(weekIssues(configured), []);
});

test("R203-07 confirmed discard restores ALL three capabilities from the same baseline", () => {
  const document = documentWith();
  const seeded = seedAuthoringDrafts(document);

  // Dirty work in every capability.
  const form = withName(seeded.form, FA, "پست ویژه");
  const rates = withRowField(withAddedRow(seeded.rates, G1, "local-9"), G1, "local-9", "freightAmount", "12.00");
  const week = withDayTime(withDayWorking(seeded.week, 7, true), 7, "startTime", "08:00");
  const dirty = buildAuthoringIntent(document, form, rates, week);
  assert.equal(dirty.localDirty, true);
  assert.ok(dirty.edits.methodTranslations);
  assert.ok(dirty.edits.regionGroups);
  assert.ok(dirty.edits.weeklySchedule);

  const reset = resetAuthoringDrafts(document);
  const clean = buildAuthoringIntent(document, reset.form, reset.rates, reset.week);
  assert.deepEqual(clean.edits, {}, "nothing survives the discard");
  assert.equal(clean.localDirty, false);
  assert.equal(hasNoIssues(clean.issues), true);

  // View shows the persisted tree: exact stored money and the stored week, no new row.
  assert.deepEqual(reset.rates[G1]!.map((row) => row.rateRowId), [R1]);
  assert.equal(reset.rates[G1]![0]!.freightAmount, "1850000.00");
  assert.equal(reset.week.kind, "configured");
  // Editing only General again cannot resurrect the discarded rows/week.
  const generalOnly = buildAuthoringIntent(document, withName(reset.form, FA, "نام تازه"), reset.rates, reset.week);
  assert.deepEqual(Object.keys(generalOnly.edits).sort(), ["methodTranslations"]);
});

test("R203-08 the real screen Cancel Edit path calls the same whole-document reseed", () => {
  const source = readFileSync(new URL("./ui/authoring-screen.tsx", import.meta.url), "utf8");
  assert.ok(source.includes("resetAuthoringDrafts(document"), "the UI path uses the tested helper");
  const cancelBlock = source.slice(source.indexOf("controller.cancelDiscard();"), source.lastIndexOf('setMode("view")'));
  for (const call of ["setForm(drafts.form)", "setRates(drafts.rates)", "setWeek(drafts.week)"]) {
    assert.ok(cancelBlock.includes(call), `cancel edit reseeds ${call}`);
  }
  assert.ok(cancelBlock.includes("controller.edit({})"), "the session intent is cleared too");
});

function draftOf(overrides: Partial<RateRowDraft>): RateRowDraft {
  const base = createRatesForm(documentWith())[G1]![0]!;
  return { ...base, ...overrides };
}

test("R203-09 read-only rate conditions show every real axis, not only weight", () => {
  const countOnly = draftOf({
    minWeightGrams: "",
    maxWeightGrams: "",
    minItemCount: "2",
    maxItemCount: "10",
    minMerchandiseAmount: "",
    maxMerchandiseAmount: "",
  });
  assert.equal(
    rateDraftConditionText(countOnly, "en", messages),
    "Count: 2–10",
    "a count-only row is not 'no condition'",
  );

  const moneyOnly = draftOf({
    minWeightGrams: "",
    maxWeightGrams: "",
    minItemCount: "",
    maxItemCount: "",
    minMerchandiseAmount: "0.50",
    maxMerchandiseAmount: "1850000.00",
  });
  assert.equal(rateDraftConditionText(moneyOnly, "en", messages), "Merchandise: 0.50–1,850,000.00");
  assert.ok(rateDraftConditionText(moneyOnly, "en", messages).includes("0.50"), "exact decimal text is kept");

  const combined = draftOf({ minItemCount: "1", maxItemCount: "4", minMerchandiseAmount: "100000", maxMerchandiseAmount: "" });
  const text = rateDraftConditionText(combined, "en", messages);
  // The truthful label carries its unit ("Weight (g)"), so the assertion follows the real rendered text.
  assert.ok(text.includes("Weight (g): 0–5,000"));
  assert.ok(text.includes("Count: 1–4"));
  assert.ok(text.includes("Merchandise: 100,000–—"), "an open upper bound is never invented");

  const noCondition = draftOf({
    minWeightGrams: "",
    maxWeightGrams: "",
    minItemCount: "",
    maxItemCount: "",
    minMerchandiseAmount: "",
    maxMerchandiseAmount: "",
  });
  assert.equal(rateDraftConditionText(noCondition, "en", messages), "No condition");

  // The stored-row formatter agrees (it is the same function underneath).
  assert.equal(
    rateConditionText(
      row({ minWeightGrams: null, maxWeightGrams: null, minMerchandiseAmount: null, maxMerchandiseAmount: null, minItemCount: 3, maxItemCount: null }),
      "en",
      messages,
    ),
    "Count: 3–—",
  );
});
