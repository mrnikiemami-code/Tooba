import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import type { EditorDocument, EditorRateRow, EditorRegionGroup } from "./model/editor-document.ts";
import { createEditorForm, withName } from "./model/editor-form.ts";
import {
  buildGroupsEdits,
  createGroupsForm,
  groupIssues,
  groupsDirty,
  withAddedGroup,
  withGroupAreaAdded,
  withGroupAreaRemoved,
  withGroupTitle,
  withGroupTitleSlots,
  withMovedGroup,
  withRemovedGroup,
} from "./model/editor-groups.ts";
import { createRatesForm, withAddedRow, withRowField } from "./model/editor-rates.ts";
import { createWeekForm } from "./model/editor-week.ts";
import { buildAuthoringIntent, hasNoIssues } from "./model/authoring-intent.ts";
import { resetAuthoringDrafts, seedAuthoringDrafts } from "./model/authoring-drafts.ts";
import { indexDestinationFacts } from "./model/editor-destination-ancestry.ts";
import {
  areaIdentity,
  choiceAncestry,
  choiceTerminal,
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseLevel,
  chooseServiceArea,
  composeAncestryResolver,
  emptyChoice,
  unresolvedDraftSelections,
} from "./model/destination-choice.ts";
import { decodeGeographyOptions, geographyQueryPath } from "./api/geography-lookup-api.ts";
import type { GeographyQuery, GeographyReadOutcome } from "./api/geography-lookup-api.ts";
import { createDestinationReadCoordinator } from "./ui/destination-read-coordinator.ts";
import type { DestinationReadState } from "./ui/destination-read-coordinator.ts";
import { buildEditorDocumentBody } from "./api/editor-document-request.ts";
import { createEditorController, type EditorTransport } from "./session/editor-controller.ts";
import type { EditorOutcome } from "./api/editor-document-api.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const FA = "44444444-4444-4444-8444-444444444441";
const EN = "55555555-5555-4555-8555-555555555551";
const G1 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";
const G2 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2";
const R1 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1";
const R3 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb3";
const CITY = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";
const SERVICE = "cccccccc-cccc-4ccc-8ccc-ccccccccccc2";
const COUNTRY = "dddddddd-dddd-4ddd-8ddd-ddddddddddd1";
const AREA = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1";
const NEW_CITY = "cccccccc-cccc-4ccc-8ccc-cccccccccc10";

const read = (relative: string): string => readFileSync(new URL(`./${relative}`, import.meta.url), "utf8");

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

function group(overrides: Partial<EditorRegionGroup> = {}): EditorRegionGroup {
  return {
    groupId: G1,
    order: 0,
    currencyCode: "IRR",
    areas: [{ kind: "City", id: CITY }],
    rows: [row()],
    titles: [{ languageId: FA, title: "تهران" }],
    ...overrides,
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
  const base: EditorDocument = {
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
    weeklySchedule: null,
    regionGroups: [
      group(),
      group({
        groupId: G2,
        order: 1,
        areas: [{ kind: "ServiceArea", id: SERVICE }],
        rows: [row({ rateRowId: R3, freightAmount: "0.50" })],
        titles: [],
      }),
    ],
    destinationFacts: [
      {
        kind: "City",
        id: CITY,
        resolution: "Resolved",
        label: "Tehran",
        area: { kind: "City", countryId: COUNTRY, administrativeAreaId: null, cityId: CITY, serviceAreaId: null },
      },
      {
        kind: "ServiceArea",
        id: SERVICE,
        resolution: "Resolved",
        label: "Service area",
        area: { kind: "ServiceArea", countryId: COUNTRY, administrativeAreaId: null, cityId: CITY, serviceAreaId: SERVICE },
      },
    ],
  };
  return { ...base, ...overrides };
}

const ACTIVE = [FA, EN];

function wire(document: EditorDocument, edits: ReturnType<typeof buildAuthoringIntent>["edits"]) {
  const body = buildEditorDocumentBody(document, edits, indexDestinationFacts(document).resolver);
  assert.equal(body.status, "ready");
  if (body.status !== "ready") throw new Error("unreachable");
  return { body: JSON.parse(body.text) as Record<string, unknown>, text: body.text };
}

test("204-01 empty draft → new group + real destination + nested row + title in ONE body", () => {
  const empty = documentWith({ regionGroups: [], destinationFacts: [] });
  const languageIds = ACTIVE;
  let groups = createGroupsForm(empty, languageIds);
  let rates = createRatesForm(empty);
  const key = "group-new-1";
  groups = withGroupTitleSlots(withAddedGroup(groups, key), key, languageIds);
  rates = { ...rates, [key]: [] };
  groups = withGroupAreaAdded(groups, key, { kind: "City", id: NEW_CITY });
  groups = withGroupTitle(withGroupTitle(groups, key, FA, "گروه تازه"), key, EN, "New group");
  rates = withRowField(withAddedRow(rates, key, "new-1"), key, "new-1", "freightAmount", "1200.00");
  rates = withRowField(rates, key, "new-1", "packagingAmount", "0.00");
  rates = withRowField(rates, key, "new-1", "minDeliveryDays", "1");
  rates = withRowField(rates, key, "new-1", "maxDeliveryDays", "4");
  assert.deepEqual(groupIssues(groups), []);

  const resolver = composeAncestryResolver(indexDestinationFacts(empty).resolver, new Map([
    [areaIdentity({ kind: "City", id: NEW_CITY }), { kind: "City", countryId: COUNTRY, administrativeAreaId: null, cityId: NEW_CITY, serviceAreaId: null }],
  ]));
  const intent = buildAuthoringIntent(empty, createEditorForm(empty), rates, createWeekForm(null), groups);
  assert.equal(hasNoIssues(intent.issues), true);
  assert.equal(intent.localDirty, true);

  const body = buildEditorDocumentBody(empty, intent.edits, resolver);
  assert.equal(body.status, "ready");
  if (body.status !== "ready") throw new Error("unreachable");
  const parsed = JSON.parse(body.text) as { expectedRevision: number; regionGroups: { groupId: string | null; titles: unknown[] }[] };
  assert.equal(parsed.expectedRevision, 4);
  assert.equal(parsed.regionGroups.length, 1);
  assert.equal(parsed.regionGroups[0]!.groupId, null, "a new group carries no invented id");
  assert.equal(parsed.regionGroups[0]!.titles.length, 2);
  assert.equal(body.text.includes(key), false, "the client-local group key never reaches the wire");
  assert.equal(body.text.includes("new-1"), false, "the local row key never reaches the wire");
  assert.equal(body.text.includes('"freightAmount":1200.00'), true);
});

test("204-02 reorder/remove/add keeps untouched ids, areas, rows and exact money; all-remove is []", () => {
  const document = documentWith();
  let groups = createGroupsForm(document, ACTIVE);
  let rates = createRatesForm(document);

  groups = withMovedGroup(groups, G2, -1);
  groups = withRemovedGroup(groups, G1);
  const key = "group-new-2";
  groups = withGroupTitleSlots(withAddedGroup(groups, key), key, ACTIVE);
  rates = { ...rates, [key]: [] };
  groups = withGroupAreaAdded(groups, key, { kind: "Country", id: COUNTRY });
  rates = withRowField(withAddedRow(rates, key, "row-x"), key, "row-x", "freightAmount", "0.50");

  const intent = buildAuthoringIntent(document, createEditorForm(document), rates, createWeekForm(null), groups);
  const edits = intent.edits.regionGroups!;
  assert.equal(edits.length, 2);
  assert.equal(edits[0]!.groupId, G2, "the retained group keeps its exact id after moving");
  assert.deepEqual(edits[0]!.areas, document.regionGroups[1]!.areas);
  assert.equal(edits[0]!.rows[0]!.rateRowId, R3);
  assert.equal(edits[0]!.rows[0]!.freightAmount, "0.50", "exact money token is preserved");
  assert.deepEqual(edits[0]!.titles, [], "an untouched group has no title patch");
  assert.equal(edits[1]!.groupId, null);
  assert.equal(groupsDirty(document, groups), true);

  const allRemoved = withRemovedGroup(withRemovedGroup(createGroupsForm(document, ACTIVE), G1), G2);
  const cleared = buildGroupsEdits(document, allRemoved, createRatesForm(document));
  assert.deepEqual(cleared, [], "explicit removal of every group clears the collection");
  assert.deepEqual(buildGroupsEdits(document, createGroupsForm(document, ACTIVE), createRatesForm(document)), undefined);
});

test("204-03 title patches: touched active pair, explicit clear, untouched inactive text preserved", () => {
  const base = group({ titles: [{ languageId: FA, title: "تهران" }, { languageId: EN, title: "Tehran" }] });
  const document = documentWith({ regionGroups: [base], destinationFacts: documentWith().destinationFacts });
  let groups = createGroupsForm(document, ACTIVE);
  assert.deepEqual(buildGroupsEdits(document, groups, createRatesForm(document)), undefined, "seed is clean");

  groups = withGroupTitle(groups, G1, FA, "تهران بزرگ");
  let edits = buildGroupsEdits(document, groups, createRatesForm(document))!;
  assert.deepEqual(edits[0]!.titles, [{ languageId: FA, title: "تهران بزرگ" }], "an untouched EN pair is not echoed");

  groups = withGroupTitle(groups, G1, EN, "   ");
  edits = buildGroupsEdits(document, groups, createRatesForm(document))!;
  assert.deepEqual(edits[0]!.titles, [
    { languageId: FA, title: "تهران بزرگ" },
    { languageId: EN, title: null },
  ], "a blank active title clears exactly that pair");

  const back = withGroupTitle(withGroupTitle(createGroupsForm(document, ACTIVE), G1, FA, "تهران بزرگ"), G1, EN, "Tehran");
  assert.deepEqual(buildGroupsEdits(document, back, createRatesForm(document))![0]!.titles, [{ languageId: FA, title: "تهران بزرگ" }]);
  assert.equal(buildGroupsEdits(document, withGroupTitle(createGroupsForm(document, ACTIVE), G1, EN, " Tehran "), createRatesForm(document)), undefined, "trim-equivalent text is not an edit");
});

test("204-04 geography routes are exact and decoding is strict, never a filtered success", () => {
  assert.equal(geographyQueryPath({ kind: "Country", languageId: FA }), `/v1/admin/geography/countries?languageId=${FA}`);
  assert.equal(
    geographyQueryPath({ kind: "AdministrativeArea", languageId: FA, countryId: COUNTRY }),
    `/v1/admin/geography/countries/${COUNTRY}/administrative-areas?languageId=${FA}`,
  );
  assert.equal(
    geographyQueryPath({ kind: "City", languageId: FA, countryId: COUNTRY, administrativeAreaId: AREA }),
    `/v1/admin/geography/countries/${COUNTRY}/cities?languageId=${FA}&administrativeAreaId=${AREA}`,
  );
  assert.equal(
    geographyQueryPath({ kind: "City", languageId: FA, countryId: COUNTRY }),
    `/v1/admin/geography/countries/${COUNTRY}/cities?languageId=${FA}`,
  );
  assert.equal(
    geographyQueryPath({ kind: "ServiceArea", languageId: EN, cityId: CITY }),
    `/v1/admin/geography/cities/${CITY}/service-areas?languageId=${EN}`,
  );

  const country = { kind: "Country" as const, languageId: FA };
  assert.deepEqual(decodeGeographyOptions(country, []), { state: "ok", options: [] }, "an empty catalog is a lawful success");
  assert.deepEqual(decodeGeographyOptions(country, { data: [] }).state, "failure", "an unexpected envelope is untrusted");
  assert.equal(
    decodeGeographyOptions(country, [{ id: "not-a-guid", code: "IR", label: "Iran", parentId: null, isActive: true }]).state,
    "failure",
  );
  assert.equal(
    decodeGeographyOptions(country, [
      { id: COUNTRY, code: "IR", label: "Iran", parentId: null, isActive: true },
      { id: COUNTRY.toUpperCase(), code: "IR", label: "Iran", parentId: null, isActive: true },
    ]).state,
    "failure",
    "a duplicate identity is malformed, not deduplicated",
  );
  assert.equal(
    decodeGeographyOptions(country, [{ id: COUNTRY, code: "IR", label: "Iran", parentId: AREA, isActive: true }]).state,
    "failure",
    "a country row with a parent is foreign-parent data",
  );
  assert.equal(
    decodeGeographyOptions(country, [{ id: COUNTRY, code: "IR", label: "Iran", parentId: null }]).state,
    "failure",
    "a missing isActive flag is not silently false",
  );
  assert.deepEqual(
    decodeGeographyOptions({ kind: "AdministrativeArea", languageId: FA, countryId: COUNTRY }, [
      { id: AREA, code: "THR", label: "تهران", parentId: COUNTRY, isActive: true },
    ]).state,
    "ok",
  );
  assert.equal(
    decodeGeographyOptions({ kind: "AdministrativeArea", languageId: FA, countryId: COUNTRY }, [
      { id: AREA, code: "THR", label: "تهران", parentId: G1, isActive: true },
    ]).state,
    "failure",
  );
});

test("204-05 country-wide cities keep their ACTUAL admin; null-admin city is lawful; late epochs are guarded", () => {
  const cityContext = {
    kind: "City" as const,
    languageId: FA,
    countryId: COUNTRY,
    knownAdministrativeAreaIds: new Set([AREA.toLowerCase()]),
  };
  assert.equal(
    decodeGeographyOptions(cityContext, [{ id: NEW_CITY, code: "SHZ", label: "شیراز", parentId: AREA, isActive: true }]).state,
    "ok",
  );
  assert.equal(
    decodeGeographyOptions(cityContext, [{ id: NEW_CITY, code: "SHZ", label: "شیراز", parentId: null, isActive: true }]).state,
    "ok",
    "a city without an administrative area remains lawful",
  );
  assert.equal(
    decodeGeographyOptions(cityContext, [{ id: NEW_CITY, code: "SHZ", label: "شیراز", parentId: G1, isActive: true }]).state,
    "failure",
    "an admin outside the returned country areas is foreign-parent data",
  );

  // The staged choice keeps the row's own parent and never substitutes the highlighted area.
  const country = { kind: "Country" as const, id: COUNTRY, code: "IR", label: "Iran", parentId: null, isActive: true };
  const areaRow = { kind: "AdministrativeArea" as const, id: AREA, code: "THR", label: "Tehran", parentId: COUNTRY, isActive: true };
  const cityRow = { kind: "City" as const, id: NEW_CITY, code: "SHZ", label: "Shiraz", parentId: null, isActive: true };
  let choice = chooseCity(chooseAdministrativeArea(chooseCountry(emptyChoice(), country), areaRow), cityRow);
  choice = chooseLevel(choice, "City");
  assert.deepEqual(choiceAncestry(choice), {
    kind: "City",
    countryId: COUNTRY,
    administrativeAreaId: null,
    cityId: NEW_CITY,
    serviceAreaId: null,
  }, "the highlighted area is NOT substituted for the city's real parent");
  assert.deepEqual(choiceTerminal(chooseLevel(choice, "AdministrativeArea")), { kind: "AdministrativeArea", id: AREA });

  const serviceRow = { kind: "ServiceArea" as const, id: SERVICE, code: "S1", label: "S1", parentId: NEW_CITY, isActive: true };
  const withService = chooseLevel(chooseServiceArea(choice, serviceRow), "ServiceArea");
  assert.deepEqual(choiceAncestry(withService), {
    kind: "ServiceArea",
    countryId: COUNTRY,
    administrativeAreaId: null,
    cityId: NEW_CITY,
    serviceAreaId: SERVICE,
  });

  const picker = read("ui/authoring-destination-picker.tsx");
  assert.match(picker, /createDestinationReadCoordinator/, "the picker delegates ALL orchestration to the one coordinator");
  assert.match(picker, /choiceChainActive/, "Add re-checks active/chain coherence");
  assert.match(picker, /destination-level-/, "all four terminal levels are explicit");
  assert.equal(picker.includes("app/admin/shipping-services"), false, "no feature→app import of the legacy picker");
});

test("204-05b a late old-parent response is dropped by EXECUTION, not by a source pattern", async () => {
  const pending: {
    query: GeographyQuery;
    signal: AbortSignal;
    resolve(outcome: GeographyReadOutcome): void;
  }[] = [];
  let latest: DestinationReadState | undefined;
  const ctl = createDestinationReadCoordinator(
    (query, signal) => new Promise<GeographyReadOutcome>((resolve) => pending.push({ query, signal, resolve })),
    (state) => {
      latest = state;
    },
  );
  ctl.update({ enabled: true, languageId: FA, countryId: COUNTRY, administrativeAreaId: null, cityId: null });
  ctl.update({ enabled: true, languageId: FA, countryId: G1, administrativeAreaId: null, cityId: null });

  const areas = pending.filter((call) => call.query.kind === "AdministrativeArea");
  assert.equal(areas.length, 2, "each parent opens its own area read");
  assert.equal(areas[0].signal.aborted, true, "the obsolete branch read is aborted");
  assert.equal(latest?.lists.areas.length, 0, "old rows are cleared before the new parent answers");

  areas[0].resolve({
    state: "ok",
    options: [{ kind: "AdministrativeArea", id: AREA, code: "THR", label: "Tehran", parentId: COUNTRY, isActive: true }],
  });
  await new Promise((resolve) => setImmediate(resolve));
  assert.equal(latest?.lists.areas.length, 0, "the late response of the previous parent cannot win");

  areas[1].resolve({
    state: "ok",
    options: [{ kind: "AdministrativeArea", id: AREA, code: "THR", label: "Tehran", parentId: G1, isActive: true }],
  });
  await new Promise((resolve) => setImmediate(resolve));
  const areasAfterCurrent = latest?.lists.areas ?? [];
  assert.equal(areasAfterCurrent.length, 1, "the current parent's response is applied");
  assert.equal(areasAfterCurrent[0]?.parentId, G1, "the current parent's response is applied");
  ctl.dispose();
});

test("204-06 proposed resolver: retained facts + verified picks; removed unresolved stops blocking", () => {
  const document = documentWith({
    destinationFacts: [
      {
        kind: "City",
        id: CITY,
        resolution: "Resolved",
        label: "Tehran",
        area: { kind: "City", countryId: COUNTRY, administrativeAreaId: null, cityId: CITY, serviceAreaId: null },
      },
    ],
  });
  const base = indexDestinationFacts(document);
  const newArea = { kind: "Country" as const, id: COUNTRY };
  const resolvedNew = new Map([[areaIdentity(newArea), { kind: "Country" as const, countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null }]]);
  const resolver = composeAncestryResolver(base.resolver, resolvedNew);

  let groups = createGroupsForm(document, ACTIVE);
  assert.equal(unresolvedDraftSelections(groups, resolver).length, 1, "the unproven retained ServiceArea blocks");
  groups = withGroupAreaRemoved(groups, G2, { kind: "ServiceArea", id: SERVICE });
  assert.deepEqual(unresolvedDraftSelections(groups, resolver), [], "explicit removal unblocks the rest");
  groups = withGroupAreaAdded(groups, G2, newArea);
  assert.deepEqual(unresolvedDraftSelections(groups, resolver), [], "a verified session pick resolves");

  const intent = buildAuthoringIntent(document, createEditorForm(document), createRatesForm(document), createWeekForm(null), groups);
  assert.equal(hasNoIssues(intent.issues), true);
});

test("204-07 raw invalid work stays dirty+blocked; discard reseeds groups with the same baseline", () => {
  const document = documentWith();
  const key = "group-new-3";
  let groups = withGroupTitleSlots(withAddedGroup(createGroupsForm(document, ACTIVE), key), key, ACTIVE);
  const rates = { ...createRatesForm(document), [key]: withAddedRow({ [key]: [] }, key, "blank-1")[key]! };

  const intent = buildAuthoringIntent(document, createEditorForm(document), rates, createWeekForm(null), groups);
  assert.equal(hasNoIssues(intent.issues), false, "an unfilled new row blocks Apply");
  assert.equal(intent.localDirty, true);

  groups = withGroupAreaAdded(withGroupAreaAdded(groups, key, { kind: "City", id: NEW_CITY }), G1, { kind: "City", id: NEW_CITY });
  assert.equal(groups[0]!.areas.some((area) => area.id === NEW_CITY), false, "a cross-group duplicate is rejected locally");
  assert.deepEqual(groupIssues(groups), [], "the rejected duplicate never enters the drafts");
  assert.equal(withGroupAreaAdded(groups, key, { kind: "City", id: NEW_CITY }), groups, "a rejected duplicate never mutates the drafts");

  const seeded = seedAuthoringDrafts(document, ACTIVE);
  const discarded = resetAuthoringDrafts(document, ACTIVE);
  assert.deepEqual(discarded.groups, seeded.groups);
  assert.deepEqual(discarded.rates, seeded.rates);
  assert.deepEqual(discarded.form, seeded.form);
  assert.deepEqual(discarded.week, seeded.week);
  assert.equal(discarded.groups.some((entry) => entry.groupId === null), false, "no local group survives a discard");

  const screen = read("ui/authoring-screen.tsx");
  assert.match(screen, /setGroups\(drafts\.groups\)/, "the confirmed seed restores the whole collection");
  assert.match(screen, /setPickedFacts\(new Map\(\)\)/, "a discard clears staged/verified picker facts");
  assert.match(screen, /buildAuthoringIntent\(document, form, rates, week, groups\)/, "the screen composes the group draft");
  assert.match(screen, /composeAncestryResolver\(index\.resolver, pickedFacts\)/, "retained + picked facts feed one resolver");
  assert.match(screen, /withGroupAreaAdded\(groups, groupKey, area\)/, "the picker's Add uses the same group helper");
  assert.equal((screen.match(/controller\.edit\(/g) ?? []).length, 2, "edits flow through the ONE composed dispatch (+ cancel reset)");
});

test("204-08 one PUT for a new-group edit; duplicate gate and 403/409/unknown preserve the work", async () => {
  const document = documentWith();
  const server = documentWith({ revision: 5, regionGroups: [group({ groupId: "ffffffff-ffff-4fff-8fff-fffffffffff1" })] });
  let saves = 0;
  let loads = 0;
  let mode: "ok" | "denied" | "conflict" | "unknown" = "ok";
  const transport: EditorTransport = {
    load: async (): Promise<EditorOutcome> => {
      loads += 1;
      return { kind: "ok", document };
    },
    save: async (): Promise<EditorOutcome> => {
      saves += 1;
      if (mode === "denied") {
        return { kind: "failure", failure: { kind: "denied", status: 403, errorCode: null, title: null } };
      }
      if (mode === "conflict") {
        return {
          kind: "failure",
          failure: { kind: "conflict", status: 409, errorCode: null, title: null, errors: [] },
        };
      }
      if (mode === "unknown") return { kind: "failure", failure: { kind: "outcomeUnknown", reason: "network" } };
      return { kind: "ok", document: server };
    },
  };
  const controller = createEditorController(DRAFT, transport);
  controller.begin();
  await new Promise((resolve) => setImmediate(resolve));
  assert.equal(loads, 1);

  const empty = documentWith({ regionGroups: [], destinationFacts: [] });
  const controller2 = createEditorController(DRAFT, {
    load: async (): Promise<EditorOutcome> => ({ kind: "ok", document: empty }),
    save: transport.save,
  });
  controller2.begin();
  await new Promise((resolve) => setImmediate(resolve));

  const key = "group-new-9";
  let groups = withGroupTitleSlots(withAddedGroup(createGroupsForm(empty, ACTIVE), key), key, ACTIVE);
  groups = withGroupAreaAdded(groups, key, { kind: "Country", id: COUNTRY });
  const rates = { ...createRatesForm(empty), [key]: [] };
  const resolver = composeAncestryResolver(indexDestinationFacts(empty).resolver, new Map([
    [areaIdentity({ kind: "Country", id: COUNTRY }), { kind: "Country", countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null }],
  ]));
  const form = withName(createEditorForm(empty), FA, "پست تازه");
  const intent = buildAuthoringIntent(empty, form, rates, createWeekForm(null), groups);
  assert.equal(hasNoIssues(intent.issues), true);
  controller2.edit(intent.edits, intent.localDirty);

  const first = await controller2.apply(resolver);
  assert.equal(first.status, "sent");
  assert.equal(saves, 1);
  assert.equal(controller2.getState().session.baseline!.regionGroups[0]!.groupId, "ffffffff-ffff-4fff-8fff-fffffffffff1");

  // A second dispatch while the same intent is already sent is gated by the session, not a second PUT.
  const second = await controller2.apply(resolver);
  assert.notEqual(second.status, "sent");
  assert.equal(saves, 1, "no automatic retry / duplicate PUT");

  for (const failure of ["denied", "conflict", "unknown"] as const) {
    mode = failure;
    const fresh = createEditorController(DRAFT, {
      load: async (): Promise<EditorOutcome> => ({ kind: "ok", document: empty }),
      save: transport.save,
    });
    fresh.begin();
    await new Promise((resolve) => setImmediate(resolve));
    fresh.edit(intent.edits, intent.localDirty);
    const outcome = await fresh.apply(resolver);
    assert.equal(outcome.status, "failed", `${failure}: the save really failed`);
    const state = fresh.getState().session;
    assert.equal(state.unsaved, true, `${failure}: local work is preserved`);
    if (failure === "unknown") assert.equal(state.commitUncertain, true);
  }
  assert.equal(loads, 1, "no post-save read on any path");
});
