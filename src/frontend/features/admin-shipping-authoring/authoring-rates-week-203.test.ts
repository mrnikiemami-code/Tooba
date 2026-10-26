import assert from "node:assert/strict";
import test from "node:test";
import type { EditorDocument, EditorRateRow, EditorWeeklySchedule } from "./model/editor-document.ts";
import {
  createRatesForm,
  ratesDirty,
  rowIssue,
  withAddedRow,
  withRemovedRow,
  withRowField,
} from "./model/editor-rates.ts";
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
  WEEKDAYS,
} from "./model/editor-week.ts";
import { buildAuthoringIntent, hasNoIssues, intentHasEdits } from "./model/authoring-intent.ts";
import { createEditorForm, withName } from "./model/editor-form.ts";
import { buildEditorDocumentBody } from "./api/editor-document-request.ts";
import { indexDestinationFacts } from "./model/editor-destination-ancestry.ts";
import { createEditorController, type EditorTransport } from "./session/editor-controller.ts";
import type { EditorOutcome } from "./api/editor-document-api.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const FA = "44444444-4444-4444-8444-444444444441";
const EN = "55555555-5555-4555-8555-555555555551";
const G1 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";
const G2 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2";
const R1 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1";
const R2 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2";
const R3 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb3";
const CITY = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";
const SERVICE = "cccccccc-cccc-4ccc-8ccc-ccccccccccc2";

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

function schedule(endTime = "18:00:00.5000000"): EditorWeeklySchedule {
  return {
    timeZoneId: "Asia/Tehran",
    days: [1, 2, 3, 4, 5, 6, 7].map((weekday) => ({
      weekday,
      isWorking: weekday <= 5,
      startTime: weekday <= 5 ? "09:30:00" : null,
      endTime: weekday <= 5 ? endTime : null,
    })),
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
      groupTitles: [{ languageId: FA, title: "تهران" }],
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
        rows: [row(), row({ rateRowId: R2, minWeightGrams: 5001, maxWeightGrams: 100_000, freightAmount: "2500000.00" })],
        titles: [{ languageId: FA, title: "تهران" }],
      },
      {
        groupId: G2,
        order: 1,
        currencyCode: "IRR",
        areas: [{ kind: "ServiceArea", id: SERVICE }],
        rows: [row({ rateRowId: R3, freightAmount: "0.50" })],
        titles: [],
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
      {
        kind: "ServiceArea",
        id: SERVICE,
        resolution: "Resolved",
        label: "Service area",
        area: {
          kind: "ServiceArea",
          countryId: "dddddddd-dddd-4ddd-8ddd-ddddddddddd1",
          administrativeAreaId: null,
          cityId: CITY,
          serviceAreaId: SERVICE,
        },
      },
    ],
    ...overrides,
  };
}

function addFilledRow(document: EditorDocument, groupId: string, key: string, freight = "1.00"): ReturnType<typeof createRatesForm> {
  let state = withAddedRow(createRatesForm(document), groupId, key);
  for (const [field, value] of [
    ["minWeightGrams", "0"],
    ["freightAmount", freight],
    ["packagingAmount", "0.00"],
    ["minDeliveryDays", "1"],
    ["maxDeliveryDays", "2"],
  ] as const) {
    state = withRowField(state, groupId, key, field, value);
  }
  return state;
}

function bodyText(
  document: EditorDocument,
  rates = createRatesForm(document),
  week = createWeekForm(document.weeklySchedule),
  form = createEditorForm(document),
): { body: Record<string, unknown>; text: string } {
  const intent = buildAuthoringIntent(document, form, rates, week);
  assert.deepEqual(intent.issues.rates, []);
  assert.deepEqual(intent.issues.week, []);
  assert.deepEqual(intent.issues.general, []);
  const index = indexDestinationFacts(document);
  const body = buildEditorDocumentBody(document, intent.edits, index.resolver);
  assert.equal(body.status, "ready", body.status === "ready" ? undefined : JSON.stringify(body));
  if (body.status !== "ready") throw new Error("unreachable");
  return { body: JSON.parse(body.text) as Record<string, unknown>, text: body.text };
}

test("203-01 editing one group keeps every other group id/order/areas/currency/rows exact", () => {
  const document = documentWith();
  const rates = withRowField(createRatesForm(document), G1, R1, "freightAmount", "1900000.00");
  assert.equal(ratesDirty(document, rates), true);

  const intent = buildAuthoringIntent(document, createEditorForm(document), rates, createWeekForm(document.weeklySchedule));
  assert.ok(intent.edits.regionGroups);
  const groups = intent.edits.regionGroups!;
  assert.equal(groups.length, 2);
  assert.equal(groups[0]!.groupId, G1);
  assert.equal(groups[0]!.rows[0]!.freightAmount, "1900000.00");
  assert.deepEqual(groups[0]!.areas, document.regionGroups[0]!.areas);
  assert.deepEqual(groups[0]!.titles, [], "titles stay patch-free");
  assert.deepEqual(groups[1]!.rows.map((entry) => entry.rateRowId), document.regionGroups[1]!.rows.map((entry) => entry.rateRowId));
  assert.deepEqual(groups[1]!.areas, document.regionGroups[1]!.areas);
  assert.ok(intent.edits.weeklySchedule === undefined, "an untouched week is omitted");

  const { body, text } = bodyText(document, rates);
  assert.equal(body.expectedRevision, 4);
  assert.equal(text.includes('"freightAmount":1900000.00'), true);
  assert.equal(text.includes('"freightAmount":0.50'), true);
});

test("203-02 add/remove rows stays inside one group; a new row has no invented server id", () => {
  const document = documentWith();
  const added = addFilledRow(document, G1, "local-7");
  const intent = buildAuthoringIntent(document, createEditorForm(document), added, createWeekForm(document.weeklySchedule));
  const groups = intent.edits.regionGroups!;
  const newRow = groups[0]!.rows[2]!;
  assert.equal(newRow.rowKey, "local-7");
  assert.equal(newRow.rateRowId, null);
  const { body, text } = bodyText(document, added);
  assert.equal(text.includes("local-7"), false, "the local key never reaches the wire");
  const wireRows = (body.regionGroups as { rows: { rateRowId: string | null }[] }[])[0]!.rows;
  assert.equal(wireRows[2]!.rateRowId, null);

  const removed = withRemovedRow(createRatesForm(document), G2, R3);
  const afterRemove = buildAuthoringIntent(document, createEditorForm(document), removed, createWeekForm(document.weeklySchedule));
  const afterGroups = afterRemove.edits.regionGroups!;
  assert.deepEqual(afterGroups[1]!.rows, [], "the last row removal empties only that group");
  assert.equal(afterGroups[0]!.rows.length, 2, "the other group is untouched");
});

test("203-03 exact decimal tokens survive, blank bounds become null, invalid raw work blocks", () => {
  const document = documentWith();
  const base = createRatesForm(document);
  const edited = withRowField(withRowField(base, G1, R1, "maxMerchandiseAmount", ""), G1, R1, "minMerchandiseAmount", "0.50");
  const { body, text } = bodyText(document, edited);
  const wireRow = (body.regionGroups as { rows: Record<string, unknown>[] }[])[0]!.rows[0]!;
  assert.equal(text.includes('"freightAmount":1850000.00'), true, "the exact money token is written");
  assert.equal(text.includes('"packagingAmount":0.00'), true);
  assert.equal(text.includes('"minMerchandiseAmount":0.50'), true);
  assert.equal(wireRow.maxMerchandiseAmount, null);

  // Reverting every text back to the stored token is clean again.
  const reverted = withRowField(withRowField(edited, G1, R1, "minMerchandiseAmount", "0.50"), G1, R1, "maxMerchandiseAmount", "");
  assert.equal(ratesDirty(document, reverted), false);

  // Required blank / malformed / unsafe integer / half range are local blocking issues, never a throw.
  assert.equal(rowIssue({ ...base[G1]![0]!, freightAmount: "" }), "amountRequired");
  assert.equal(rowIssue({ ...base[G1]![0]!, freightAmount: "1.234" }), "amountInvalid");
  assert.equal(rowIssue({ ...base[G1]![0]!, minWeightGrams: "9007199254740993" }), "integerInvalid");
  assert.equal(rowIssue({ ...base[G1]![0]!, maxItemCount: "4" }), "boundIncomplete");
  assert.equal(rowIssue({ ...base[G1]![0]!, maxWeightGrams: "0" }), "boundRangeInvalid");

  const invalid = withRowField(base, G1, R1, "freightAmount", "");
  const intent = buildAuthoringIntent(document, createEditorForm(document), invalid, createWeekForm(document.weeklySchedule));
  assert.equal(hasNoIssues(intent.issues), false);
  assert.equal(intent.localDirty, true);
});

test("203-04 a week is omitted while untouched, configured only on purpose with 7 distinct ISO days", () => {
  const document = documentWith({ weeklySchedule: null });
  const untouched = createWeekForm(null);
  assert.equal(weekDirty(null, untouched), false);
  assert.equal(buildWeeklyScheduleEdit(null, untouched), undefined);

  const configured = configureWeek(withPendingTimeZone(untouched, "Asia/Tehran"));
  assert.equal(configured.kind, "configured");
  if (configured.kind !== "configured") throw new Error("unreachable");
  assert.deepEqual(configured.days.map((day) => day.weekday), [...WEEKDAYS]);
  assert.equal(new Set(configured.days.map((day) => day.weekday)).size, 7);
  assert.ok(configured.days.every((day) => !day.isWorking && day.startTime === "" && day.endTime === ""));
  assert.deepEqual(weekIssues(configured), []);
  assert.equal(weekDirty(null, configured), true);

  const { body } = bodyText(document, createRatesForm(document), configured);
  const scheduleWrite = body.weeklySchedule as { timeZoneId: string; days: unknown[] };
  assert.equal(scheduleWrite.timeZoneId, "Asia/Tehran");
  assert.equal(scheduleWrite.days.length, 7);
});

test("203-05 untouched fractional TimeText is retained; deliberate edits become canonical; bad pairs block", () => {
  const document = documentWith();
  const base = createWeekForm(document.weeklySchedule);
  assert.equal(buildWeeklyScheduleEdit(document.weeklySchedule, base), undefined, "no phantom week edit");

  const saturday = withDayTime(withDayWorking(base, 6, true), 6, "startTime", "10:00");
  const saturdayDone = withDayTime(saturday, 6, "endTime", "14:30");
  assert.deepEqual(weekIssues(saturdayDone), []);
  const write = buildWeeklyScheduleEdit(document.weeklySchedule, saturdayDone)!;
  assert.equal(write.days.find((day) => day.weekday === 6)!.startTime, "10:00:00");
  assert.equal(write.days.find((day) => day.weekday === 6)!.endTime, "14:30:00");
  assert.equal(write.days.find((day) => day.weekday === 1)!.endTime, "18:00:00.5000000", "precision is untouched");
  assert.equal(write.days.find((day) => day.weekday === 7)!.endTime, null);

  const partial = withDayTime(withDayWorking(base, 7, true), 7, "startTime", "09:00");
  assert.deepEqual(weekIssues(partial), [{ code: "dayHoursRequired", weekday: 7 }]);
  const reversed = withDayTime(withDayTime(withDayWorking(base, 7, true), 7, "startTime", "18:00"), 7, "endTime", "08:00");
  assert.deepEqual(weekIssues(reversed), [{ code: "dayRangeInvalid", weekday: 7 }]);
  const clearedZone = withTimeZone(base, "  ");
  assert.deepEqual(weekIssues(clearedZone), [{ code: "timeZoneRequired", weekday: null }]);

  // A configured all-closed week is a real configuration, distinct from unconfigured.
  const allClosed = configureWeek(withPendingTimeZone(createWeekForm(null), "Asia/Tehran"));
  assert.equal(weekDirty(null, allClosed), true);
  assert.deepEqual(weekIssues(allClosed), []);
});

test("203-06 General → rates → week → General keeps every pending capability in ONE intent", () => {
  const document = documentWith();
  const form1 = withName(createEditorForm(document), FA, "پست ویژه");
  const rates = withRowField(createRatesForm(document), G2, R3, "packagingAmount", "1500.00");
  const week = withDayTime(withDayWorking(createWeekForm(document.weeklySchedule), 7, true), 7, "startTime", "08:00");
  const weekDone = withDayTime(week, 7, "endTime", "12:00");
  const form2 = withName(form1, EN, "Special post");

  const intent = buildAuthoringIntent(document, form2, rates, weekDone);
  assert.ok(intent.edits.methodTranslations, "the later General edit is kept");
  assert.ok(intent.edits.regionGroups, "the earlier rate edit is kept");
  assert.ok(intent.edits.weeklySchedule, "the week edit is kept");
  assert.equal(intentHasEdits(intent.edits), true);

  const { body } = bodyText(document, rates, weekDone, form2);
  const translations = body.methodTranslations as { languageId: string; name: string }[];
  assert.equal(translations.find((entry) => entry.languageId === FA)!.name, "پست ویژه");
  assert.ok(body.regionGroups);
  assert.ok(body.weeklySchedule);
});

test("203-07 accepted save adopts server child ids with ONE PUT and no post-save read", async () => {
  const document = documentWith();
  let saves = 0;
  let loads = 0;
  const serverDocument = documentWith({
    revision: 5,
    regionGroups: [
      {
        ...document.regionGroups[0]!,
        rows: [...document.regionGroups[0]!.rows, row({ rateRowId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1", freightAmount: "1.00" })],
      },
      document.regionGroups[1]!,
    ],
  });
  const transport: EditorTransport = {
    load: async (): Promise<EditorOutcome> => {
      loads += 1;
      return { kind: "ok", document };
    },
    save: async (): Promise<EditorOutcome> => {
      saves += 1;
      return { kind: "ok", document: serverDocument };
    },
  };
  const controller = createEditorController(DRAFT, transport);
  controller.begin();
  await new Promise((resolve) => setImmediate(resolve));

  const filled = addFilledRow(document, G1, "local-1");
  const intent = buildAuthoringIntent(document, createEditorForm(document), filled, createWeekForm(document.weeklySchedule));
  assert.equal(hasNoIssues(intent.issues), true);
  controller.edit(intent.edits, intent.localDirty);

  const outcome = await controller.apply(indexDestinationFacts(document).resolver);
  assert.equal(outcome.status, "sent");
  assert.equal(saves, 1);
  assert.equal(loads, 1, "no post-save GET");
  const baseline = controller.getState().session.baseline!;
  assert.equal(baseline.revision, 5);
  assert.equal(baseline.regionGroups[0]!.rows.length, 3);
  assert.equal(baseline.regionGroups[0]!.rows[2]!.rateRowId, "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1");
});


