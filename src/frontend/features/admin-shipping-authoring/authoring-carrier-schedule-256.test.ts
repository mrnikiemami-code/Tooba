/**
 * TB-256: the independent carrier-acceptance calendar and the one dispatch plan intent.
 *
 * Covers only what this slice claims: decode/absence, independent editing, the ONE-TIME customer copy,
 * issue blocking, exact PUT wire shape and server-rejection placement. Nothing here asserts a backend
 * migration, a dispatch calculation or a customer delivery change.
 */
import assert from "node:assert/strict";
import test from "node:test";
import type { EditorDocument, EditorScheduleDay, EditorWeeklySchedule } from "./model/editor-document.ts";
import {
  buildCarrierScheduleEdit,
  carrierWeekDirty,
  carrierWeekIssues,
  configureCarrierWeek,
  copyCarrierWeekFromCustomer,
  createCarrierWeekForm,
  resetCarrierWeek,
  withCarrierDayTime,
  withCarrierDayWorking,
  withCarrierPendingTimeZone,
  withCarrierTimeZone,
} from "./model/editor-carrier-week.ts";
import {
  createWeekForm,
  withDayTime,
  withDayWorking,
  withPendingTimeZone,
} from "./model/editor-week.ts";
import { buildAuthoringIntent, hasNoIssues, intentHasEdits } from "./model/authoring-intent.ts";
import { createEditorForm } from "./model/editor-form.ts";
import { createRatesForm } from "./model/editor-rates.ts";
import { buildEditorDocumentBody } from "./api/editor-document-request.ts";
import { decodeEditorDocument } from "./api/editor-document-decode.ts";
import { indexDestinationFacts } from "./model/editor-destination-ancestry.ts";
import type { JsonNode } from "./api/wire-json.ts";
import { serverFieldIssues } from "./ui/server-field-error.ts";
import { authoringMessages } from "./messages/authoring-messages.ts";
import type { EditorTransportFailure } from "./api/editor-document-api.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const FA = "44444444-4444-4444-8444-444444444441";
const MONDAY = 1;

const MESSAGES = authoringMessages("fa");

function day(weekday: number, isWorking: boolean, from: string | null, to: string | null): EditorScheduleDay {
  return { weekday, isWorking, startTime: from, endTime: to };
}

const WEEK: EditorWeeklySchedule = {
  timeZoneId: "Asia/Tehran",
  days: [day(1, true, "09:00:00", "17:00:00"), day(2, true, "09:00:00", "17:00:00"), day(3, true, "09:00:00", "17:00:00"), day(4, true, "09:00:00", "17:00:00"), day(5, true, "09:00:00", "17:00:00"), day(6, false, null, null), day(7, false, null, null)],
};

function documentWith(overrides: Partial<EditorDocument> = {}): EditorDocument {
  return {
    draftId: DRAFT,
    owner: { kind: "Store", sellerPartyId: null },
    serviceCode: "post",
    optionCode: null,
    revision: 4,
    createdAt: "2026-11-01T10:00:00Z",
    updatedAt: "2026-11-01T10:00:00Z",
    type: { draftId: DRAFT, revision: 4, shippingTypeId: null, resolution: "Unconfigured", shippingType: null },
    settings: { freightPaymentMode: "Prepaid", calculationMode: "Manual" },
    presentation: {
      methodTranslations: [{ languageId: FA, name: "پست", description: null }],
      groupTitles: [],
      contentLanguages: [
        {
          languageId: FA,
          code: "fa",
          culture: "fa-IR",
          urlPrefix: "fa",
          direction: "rtl",
          displayName: "Persian",
          nativeName: "فارسی",
          isDefault: true,
          sortOrder: 0,
        },
      ],
      defaultContentLanguageId: FA,
      hasDefaultLanguageName: true,
    },
    weeklySchedule: WEEK,
    carrierSchedule: null,
    regionGroups: [],
    destinationFacts: [],
    ...overrides,
  };
}

function resolverFor(document: EditorDocument) {
  return indexDestinationFacts(document.destinationFacts ?? []).resolver;
}

/** An untouched document with no local work: the carrier draft starts unconfigured. */
test("an absent/unconfigured carrier calendar is NOT an invented work week", () => {
  const document = documentWith();
  const carrier = createCarrierWeekForm(document.carrierSchedule ?? null);
  assert.equal(carrier.kind, "unconfigured");
  assert.deepEqual(carrierWeekIssues(carrier), []);
  assert.equal(carrierWeekDirty(document.carrierSchedule ?? null, carrier), false);
  assert.equal(buildCarrierScheduleEdit(document.carrierSchedule ?? null, carrier), undefined);

  const intent = buildAuthoringIntent(
    document,
    createEditorForm(document),
    createRatesForm(document),
    createWeekForm(document.weeklySchedule),
    null,
    null,
    carrier,
  );
  assert.equal(intent.edits.carrierSchedule, undefined, "an untouched carrier calendar never reaches the PUT");
  assert.equal(intentHasEdits(intent.edits), false);
  assert.equal(hasNoIssues(intent.issues), true);
});

test("a typed-but-unconfigured carrier time zone is work AND blocks Apply", () => {
  const document = documentWith();
  const carrier = withCarrierPendingTimeZone(createCarrierWeekForm(null), "Asia/Tehran");
  assert.equal(carrierWeekDirty(null, carrier), true);
  assert.deepEqual(carrierWeekIssues(carrier), [{ code: "pendingTimeZoneUnapplied", weekday: null }]);
  assert.equal(buildCarrierScheduleEdit(null, carrier), undefined, "no fabricated configured calendar");
});

test("configured carrier calendar requires real open/close pairs and keeps closed-day hours", () => {
  let carrier = configureCarrierWeek(withCarrierPendingTimeZone(createCarrierWeekForm(null), "Asia/Tehran"));
  assert.equal(carrier.kind, "configured");
  assert.deepEqual(carrierWeekIssues(carrier), [], "seven closed days are lawful");

  // Working with no hours is refused locally.
  carrier = withCarrierDayWorking(carrier, MONDAY, true);
  assert.deepEqual(carrierWeekIssues(carrier), [{ code: "dayHoursRequired", weekday: MONDAY }]);

  carrier = withCarrierDayTime(carrier, MONDAY, "opensAt", "08:00");
  assert.deepEqual(carrierWeekIssues(carrier), [{ code: "dayHoursRequired", weekday: MONDAY }]);

  carrier = withCarrierDayTime(carrier, MONDAY, "closesAt", "08:00");
  assert.deepEqual(carrierWeekIssues(carrier), [{ code: "dayRangeInvalid", weekday: MONDAY }], "zero-length is invalid");

  carrier = withCarrierDayTime(carrier, MONDAY, "closesAt", "16:00");
  assert.deepEqual(carrierWeekIssues(carrier), []);

  // A CLOSED day refuses bound edits at the handler boundary; its remembered hours survive.
  carrier = withCarrierDayWorking(carrier, MONDAY, false);
  const before = carrier.days.find((entry) => entry.weekday === MONDAY)!;
  carrier = withCarrierDayTime(carrier, MONDAY, "opensAt", "10:00");
  const after = carrier.days.find((entry) => entry.weekday === MONDAY)!;
  assert.equal(after.opensAt, before.opensAt, "closed days never receive a programmatic time edit");
  assert.equal(after.isWorking, false);
});

test("the one-time customer copy is an ordinary starting point, never a link", () => {
  const workingSaturday = withDayWorking(createWeekForm(WEEK), 6, true);
  const customer = withDayTime(
    withDayTime(workingSaturday, 6, "startTime", "10:00"),
    6,
    "endTime",
    "14:00",
  );
  assert.deepEqual(createWeekForm(WEEK).kind, "configured");
  const copied = copyCarrierWeekFromCustomer(createCarrierWeekForm(null), customer);
  assert.equal(copied.kind, "configured");
  if (copied.kind !== "configured") return;
  assert.equal(copied.timeZoneId, "Asia/Tehran");
  assert.equal(copied.days.length, 7);
  assert.equal(copied.days.find((entry) => entry.weekday === 2)!.opensAt, "09:00:00");
  assert.equal(copied.days.find((entry) => entry.weekday === 6)!.opensAt, "10:00:00");

  // Editing the copy afterwards must not touch the customer week, and the reverse must not either.
  const editedCarrier = withCarrierDayTime(withCarrierDayWorking(copied, 2, true), 2, "closesAt", "12:00");
  assert.equal(customer.days.find((entry) => entry.weekday === 2)!.endTime, "17:00:00");
  const editedCustomer = withDayTime(customer, 2, "endTime", "11:00");
  const stillIndependent = editedCarrier.days.find((entry) => entry.weekday === 2)!;
  assert.equal(stillIndependent.closesAt, "12:00");
  assert.equal(editedCustomer.days.find((entry) => entry.weekday === 2)!.endTime, "11:00");
});

test("copy refuses an unconfigured or still-invalid customer week", () => {
  const unconfiguredCarrier = createCarrierWeekForm(null);
  assert.deepEqual(
    copyCarrierWeekFromCustomer(unconfiguredCarrier, withPendingTimeZone(createWeekForm(null), "Asia/Tehran")),
    unconfiguredCarrier,
    "an unconfigured customer week cannot be copied",
  );

  const brokenCustomer = withDayTime(withDayWorking(createWeekForm(WEEK), 6, true), 6, "startTime", "18:00");
  assert.equal(brokenCustomer.kind === "configured" && brokenCustomer.days.some((entry) => entry.weekday === 6), true);
  assert.deepEqual(
    copyCarrierWeekFromCustomer(unconfiguredCarrier, brokenCustomer),
    unconfiguredCarrier,
    "an invalid customer week is never laundered into carrier acceptance",
  );
});

test("the carrier edit is serialized as its own independent seven-day section", () => {
  const document = documentWith();
  let carrier = configureCarrierWeek(withCarrierPendingTimeZone(createCarrierWeekForm(null), "Asia/Tehran"));
  carrier = withCarrierDayWorking(carrier, MONDAY, true);
  carrier = withCarrierDayTime(carrier, MONDAY, "opensAt", "08:30");
  carrier = withCarrierDayTime(carrier, MONDAY, "closesAt", "15:15");

  const intent = buildAuthoringIntent(
    document,
    createEditorForm(document),
    createRatesForm(document),
    createWeekForm(document.weeklySchedule),
    null,
    null,
    carrier,
  );
  assert.ok(intent.edits.carrierSchedule, "the carrier edit is carried by the SAME single patch");
  assert.equal(intent.edits.weeklySchedule, undefined, "the customer week is untouched");

  const body = buildEditorDocumentBody(document, intent.edits, resolverFor(document));
  assert.equal(body.status, "ready");
  if (body.status !== "ready") return;
  const parsed = JSON.parse(body.text) as {
    carrierSchedule: { timeZoneId: string; days: Array<{ weekday: number; isWorking: boolean; startTime: string | null; endTime: string | null }> };
    weeklySchedule?: unknown;
  };
  assert.equal(parsed.weeklySchedule, undefined);
  assert.equal(parsed.carrierSchedule.timeZoneId, "Asia/Tehran");
  assert.equal(parsed.carrierSchedule.days.length, 7);
  const monday = parsed.carrierSchedule.days.find((entry) => entry.weekday === MONDAY)!;
  assert.deepEqual(monday, { weekday: MONDAY, isWorking: true, startTime: "08:30:00", endTime: "15:15:00" });
  assert.equal((parsed.carrierSchedule as { slotDurationMinutes?: unknown }).slotDurationMinutes, undefined, "carrier acceptance has no customer slot length");
});

test("resetting the carrier draft restores the confirmed calendar, not the customer week", () => {
  const stored = {
    timeZoneId: "Asia/Tehran",
    days: [day(1, true, "07:00:00", "12:00:00"), day(2, false, null, null), day(3, false, null, null), day(4, false, null, null), day(5, false, null, null), day(6, false, null, null), day(7, false, null, null)],
  };
  const document = documentWith({ carrierSchedule: stored });
  const seeded = createCarrierWeekForm(document.carrierSchedule ?? null);
  assert.equal(carrierWeekDirty(stored, seeded), false);

  const changed = withCarrierTimeZone(seeded, "Asia/Dubai");
  assert.equal(carrierWeekDirty(stored, changed), true);
  const restored = resetCarrierWeek(stored);
  assert.equal(carrierWeekDirty(stored, restored), false);
  assert.equal(restored.kind === "configured" ? restored.timeZoneId : "", "Asia/Tehran");
});

/** -------- strict decode of the additive section (backward compatibility) -------- */

function rawDocument(extra: Record<string, unknown>): JsonNode {
  return {
    kind: "object",
    fields: new Map(
      Object.entries({
        draftId: { kind: "string", value: DRAFT },
        owner: { kind: "object", fields: new Map([["ownerKind", { kind: "string", value: "Store" }], ["sellerPartyId", { kind: "null" }]]) },
        serviceCode: { kind: "string", value: "post" },
        optionCode: { kind: "null" },
        revision: { kind: "number", raw: "4" },
        createdAt: { kind: "string", value: "2026-11-01T10:00:00Z" },
        updatedAt: { kind: "string", value: "2026-11-01T10:00:00Z" },
        type: {
          kind: "object",
          fields: new Map([
            ["draftId", { kind: "string", value: DRAFT }],
            ["revision", { kind: "number", raw: "4" }],
            ["resolution", { kind: "string", value: "Unconfigured" }],
            ["shippingTypeId", { kind: "null" }],
            ["shippingType", { kind: "null" }],
          ]),
        },
        settings: {
          kind: "object",
          fields: new Map([
            ["freightPaymentMode", { kind: "string", value: "Prepaid" }],
            ["calculationMode", { kind: "string", value: "Manual" }],
          ]),
        },
        presentation: {
          kind: "object",
          fields: new Map<string, JsonNode>([
            ["methodTranslations", { kind: "array", items: [] }],
            ["groupTitles", { kind: "array", items: [] }],
            [
              "contentLanguages",
              {
                kind: "array",
                items: [
                  {
                    kind: "object",
                    fields: new Map<string, JsonNode>([
                      ["languageId", { kind: "string", value: FA }],
                      ["code", { kind: "string", value: "fa" }],
                      ["culture", { kind: "string", value: "fa-IR" }],
                      ["urlPrefix", { kind: "string", value: "fa" }],
                      ["direction", { kind: "string", value: "rtl" }],
                      ["displayName", { kind: "string", value: "Persian" }],
                      ["nativeName", { kind: "string", value: "فارسی" }],
                      ["isDefault", { kind: "boolean", value: true }],
                      ["sortOrder", { kind: "number", raw: "0" }],
                    ]),
                  },
                ],
              },
            ],
            ["defaultContentLanguageId", { kind: "string", value: FA }],
            ["hasDefaultLanguageName", { kind: "boolean", value: true }],
          ]),
        },
        weeklySchedule: {
          kind: "object",
          fields: new Map<string, JsonNode>([
            ["timeZoneId", { kind: "string", value: "Asia/Tehran" }],
            [
              "days",
              {
                kind: "array",
                items: [1, 2, 3, 4, 5, 6, 7].map((weekday) => ({
                  kind: "object" as const,
                  fields: new Map<string, JsonNode>([
                    ["weekday", { kind: "number", raw: String(weekday) }],
                    ["isWorking", { kind: "boolean", value: weekday <= 5 }],
                    ["startTime", weekday <= 5 ? { kind: "string", value: "09:00:00" } : { kind: "null" }],
                    ["endTime", weekday <= 5 ? { kind: "string", value: "17:00:00" } : { kind: "null" }],
                  ]),
                })),
              },
            ],
          ]),
        },
        regionGroups: { kind: "array", items: [] },
        ...extra,
      }),
    ),
  } as JsonNode;
}

function carrierNode(days: number, timeZone = "Asia/Tehran"): JsonNode {
  return {
    kind: "object",
    fields: new Map<string, JsonNode>([
      ["timeZoneId", { kind: "string", value: timeZone }],
      [
        "days",
        {
          kind: "array",
          items: Array.from({ length: days }, (_unused, index) => ({
            kind: "object" as const,
            fields: new Map<string, JsonNode>([
              ["weekday", { kind: "number", raw: String(index + 1) }],
              ["isWorking", { kind: "boolean", value: false }],
              ["startTime", { kind: "null" }],
              ["endTime", { kind: "null" }],
            ]),
          })),
        },
      ],
    ]),
  } as JsonNode;
}

test("a document encoded before this section decodes safely with no invented calendar", () => {
  const decoded = decodeEditorDocument(rawDocument({}), DRAFT);
  assert.equal(decoded.ok, true);
  if (!decoded.ok) return;
  assert.equal(decoded.value.carrierSchedule, null);
});

test("a supplied carrier calendar decodes; a malformed one fails the WHOLE document", () => {
  const decoded = decodeEditorDocument(rawDocument({ carrierSchedule: carrierNode(7) }), DRAFT);
  assert.equal(decoded.ok, true);
  if (!decoded.ok) return;
  assert.equal(decoded.value.carrierSchedule?.timeZoneId, "Asia/Tehran");
  assert.equal(decoded.value.carrierSchedule?.days.length, 7);

  const sixDays = decodeEditorDocument(rawDocument({ carrierSchedule: carrierNode(6) }), DRAFT);
  assert.equal(sixDays.ok, false, "six days is not a lawful carrier week");

  // The two calendars are decoded independently: the customer week stays exactly as it was.
  const independent = decodeEditorDocument(
    rawDocument({ carrierSchedule: carrierNode(7, "Asia/Dubai") }),
    DRAFT,
  );
  assert.equal(independent.ok, true);
  if (!independent.ok) return;
  assert.equal(independent.value.weeklySchedule?.timeZoneId, "Asia/Tehran");
  assert.equal(independent.value.carrierSchedule?.timeZoneId, "Asia/Dubai");
});

/** -------- server rejection placement: the carrier section owns its own errors -------- */

function validation(property: string): EditorTransportFailure {
  return {
    kind: "validation",
    status: 400,
    errorCode: null,
    errors: [{ property, codes: [] }],
  } as unknown as EditorTransportFailure;
}

test("carrier rejections land on the carrier section, never on the customer week", () => {
  const timeZone = serverFieldIssues(validation("carrierSchedule.timeZoneId"), MESSAGES);
  assert.equal(timeZone[0]?.target, "carrierTimeZone");
  assert.equal(timeZone[0]?.testId, "shipping-authoring-carrier-timezone");

  const days = serverFieldIssues(validation("carrierSchedule.days[2].startTime"), MESSAGES);
  assert.equal(days[0]?.target, "carrierDays");
  assert.equal(days[0]?.testId, "shipping-authoring-carrier-week");

  const customer = serverFieldIssues(validation("weeklySchedule.timeZoneId"), MESSAGES);
  assert.equal(customer[0]?.target, "weekTimeZone");
});
