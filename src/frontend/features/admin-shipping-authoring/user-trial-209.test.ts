/**
 * TB-209 focused regressions for the rejected user trial:
 * 1. a freshly picked destination must show its REAL label, not "name unavailable";
 * 2. a rejected save must be placed on the actual field with a localized reason (never a bare
 *    "server rejected some fields") while the technical code stays available in diagnostics;
 * 3. the weekly schedule must only ever send an identifier the operator picked from the list, and a
 *    human label can never be sent as if it were an id.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { authoringMessages } from "./messages/authoring-messages.ts";
import { indexDestinationFacts, withPickedLabels } from "./model/editor-destination-ancestry.ts";
import { choiceTerminalLabel, chooseCountry, emptyChoice, areaIdentity } from "./model/destination-choice.ts";
import { createWeekForm, configureWeek, withTimeZone, withPendingTimeZone, buildWeeklyScheduleEdit, weekIssues } from "./model/editor-week.ts";
import type { EditorDocument } from "./model/editor-document.ts";
import { serverFieldIssues, serverErrorCodes, targetTab } from "./ui/server-field-error.ts";
import { TIME_ZONE_OPTIONS, isOfferedTimeZone } from "./ui/time-zone-options.ts";
import type { EditorTransportFailure } from "./api/editor-document-api.ts";

const COUNTRY_ID = "e3451206-e503-51a8-91ad-afa9ab70e330";
const FA = "01a06a53-4139-7000-bb9b-3078a0d2e5c4";
const messages = authoringMessages("fa");

function emptyDocument(): EditorDocument {
  return {
    draftId: "01a16eeb-70b3-7000-87c2-564c82835496",
    revision: 1,
    owner: { kind: "Store", sellerPartyId: null },
    service: { serviceCode: "post", optionCode: null },
    shippingType: { resolution: "Unconfigured", shippingTypeId: null, translations: [] },
    presentation: {
      defaultContentLanguageId: FA,
      contentLanguages: [{ languageId: FA, code: "fa-IR", name: "فارسی", isDefault: true }],
    },
    methodTranslations: [],
    methodSettings: null,
    weeklySchedule: null,
    regionGroups: [],
    destinationFacts: [],
    currencyCode: null,
  } as unknown as EditorDocument;
}

test("209-01 a freshly picked destination is labelled from the row the operator chose", () => {
  const picked = chooseCountry(emptyChoice("Country"), {
    kind: "Country",
    id: COUNTRY_ID,
    code: "IR",
    label: "ایران",
    parentId: null,
    isActive: true,
  });
  assert.equal(choiceTerminalLabel(picked), "ایران");

  const area = { kind: "Country" as const, id: COUNTRY_ID };
  const base = indexDestinationFacts(emptyDocument());
  assert.equal(base.labelOf(area), null, "a pick the server has not stored yet has no fact");

  const composed = withPickedLabels(base, new Map([[areaIdentity(area), "ایران"]]));
  assert.equal(composed.labelOf(area), "ایران", "the chip shows the real chosen label");
  assert.equal(composed.labelOf({ kind: "City", id: COUNTRY_ID }), null, "no label is invented for another key");
  assert.equal(base.labelOf(area), null, "the baseline index is not mutated");
});

test("209-02 a stored server fact always wins over a local pick label", () => {
  const document = emptyDocument();
  const area = { kind: "Country" as const, id: COUNTRY_ID };
  (document as unknown as { destinationFacts: unknown[] }).destinationFacts = [
    {
      kind: "Country",
      id: COUNTRY_ID,
      label: "ایران (سرور)",
      resolution: "Resolved",
      area: { kind: "Country", countryId: COUNTRY_ID, administrativeAreaId: null, cityId: null, serviceAreaId: null },
    },
  ];
  const composed = withPickedLabels(indexDestinationFacts(document), new Map([[areaIdentity(area), "ایران"]]));
  assert.equal(composed.labelOf(area), "ایران (سرور)");
});

test("209-03 the real timezone rejection is placed on the schedule time zone field", () => {
  const failure: EditorTransportFailure = {
    kind: "httpError",
    status: 400,
    errors: [],
    errorCode: "shipping.schedule.timezone.invalid",
    untrusted: null,
  } as unknown as EditorTransportFailure;

  const issues = serverFieldIssues(failure, messages);
  assert.equal(issues.length, 1);
  assert.equal(issues[0].target, "weekTimeZone");
  assert.equal(issues[0].field, messages.fieldWeekTimeZoneLabel);
  assert.equal(issues[0].reason, messages.weekTimeZoneInvalidServer);
  assert.equal(targetTab(issues[0].target), "general");
  assert.deepEqual(serverErrorCodes(failure), ["shipping.schedule.timezone.invalid"]);
});

test("209-04 registered property errors map to their own fields, never to the name by default", () => {
  const failure = {
    kind: "validation",
    status: 400,
    errors: [
      { property: "regionGroups[0].rows[0].freightAmount", codes: ["shipping.validation.amount_invalid"] },
      { property: "regionGroups[0].areas", codes: ["shipping.validation.area_unresolved"] },
    ],
    errorCode: null,
  } as unknown as EditorTransportFailure;

  const issues = serverFieldIssues(failure, messages);
  assert.deepEqual(issues.map((issue) => issue.target), ["rates", "regions"]);
  // 209-R1: a rejected ROW is labelled with its 1-based row number so two rows stay distinct.\n  assert.equal(issues[0].field, `${messages.fieldRatesLabel} 1`);
  assert.equal(targetTab("rates"), "regions");
});

test("209-05 an unknown code keeps the technical code available and invents no cause", () => {
  const failure = {
    kind: "httpError",
    status: 400,
    errors: [],
    errorCode: "shipping.something.new",
  } as unknown as EditorTransportFailure;
  const issues = serverFieldIssues(failure, messages);
  assert.equal(issues.length, 1);
  assert.equal(issues[0].target, "unknown");
  // 209-R1: an unregistered code keeps the honest generic field reason, never a guessed cause.
  assert.equal(issues[0].reason, messages.fieldRejectedServer);
  assert.deepEqual(serverErrorCodes(failure), ["shipping.something.new"]);
});

test("209-06 the schedule sends a listed identifier, never a human label", () => {
  assert.equal(isOfferedTimeZone("تهران"), false);
  assert.equal(isOfferedTimeZone("Asia/Tehran"), true);
  assert.ok(TIME_ZONE_OPTIONS.every((option) => option.id.trim() === option.id && option.id.length > 0));

  const unconfigured = withPendingTimeZone(createWeekForm(null), "Asia/Tehran");
  const configured = configureWeek(unconfigured);
  assert.equal(buildWeeklyScheduleEdit(null, configured)?.timeZoneId, "Asia/Tehran");

  const humanLabel = withTimeZone(configured, "تهران");
  const edit = buildWeeklyScheduleEdit(null, humanLabel);
  assert.equal(edit?.timeZoneId, "تهران", "the model never rewrites operator input silently");
  assert.equal(weekIssues(humanLabel).length, 0, "the server stays the authority on the identifier");
});
