/**
 * TB-225 focused regressions: destination modal (sequential 4-stage path), rate-row modal (isolated draft +
 * local overlap/duplicate mirror), and integer-rial money entry/display.
 *
 * These are LOCAL contracts only: nothing here sends a request, and the server remains the authority for
 * coverage overlap and price.
 */
import test from "node:test";
import assert from "node:assert/strict";
import type { EditorDestinationFact, EditorDocument, EditorRateRow, SelectedAreaRead } from "./model/editor-document.ts";
import type { GeographyOption, GeographyQuery, GeographyReadOutcome } from "./api/geography-lookup-api.ts";
import {
  choiceTerminal,
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseServiceArea,
  clearAdministrativeArea,
  clearCity,
  destinationAddAvailable,
  emptyStagedPath,
  isDuplicateSelection,
  pathChoice,
} from "./model/destination-choice.ts";
import {
  conditionTuple,
  conflictingRowKey,
  draftFromRow,
  newRateRow,
  rateIssues,
  rowConditionIssue,
  storedRateAmounts,
  toleratedStoredFractions,
  withConfirmedRow,
  withRowField,
  type RateRowDraft,
} from "./model/editor-rates.ts";
import {
  displayMoneyText,
  interpretMoneyEdit,
  interpretMoneyTyping,
  moneyHasFraction,
} from "./model/money-text.ts";
import { createDestinationReadCoordinator, emptyDestinationReadState } from "./ui/destination-read-coordinator.ts";
import { readStoredTrailSegments, storedTrailChains } from "./model/destination-trail-resolver.ts";

const DRAFT = "01a16eeb-70b3-7000-87c2-564c82835496";
const IRAN = "dddddddd-dddd-4ddd-8ddd-ddddddddddd1";
const TEHRAN = "dddddddd-dddd-4ddd-8ddd-ddddddddddd2";
const CITY = "dddddddd-dddd-4ddd-8ddd-ddddddddddd3";
const REGION = "dddddddd-dddd-4ddd-8ddd-ddddddddddd4";
const GROUP = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";
const ROW_A = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1";
const ROW_B = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2";

function option(
  id: string,
  label: string,
  parentId: string | null,
  kind: GeographyOption["kind"] = "City",
): GeographyOption {
  return { kind, id, code: "", label, parentId, isActive: true };
}

function row(overrides: Partial<EditorRateRow> = {}): EditorRateRow {
  return {
    rateRowId: ROW_A,
    minWeightGrams: 0,
    maxWeightGrams: 1000,
    minItemCount: null,
    maxItemCount: null,
    minMerchandiseAmount: null,
    maxMerchandiseAmount: null,
    freightAmount: "120000.00",
    packagingAmount: "5000.00",
    minDeliveryDays: 1,
    maxDeliveryDays: 3,
    ...overrides,
  };
}

function documentWith(rows: readonly EditorRateRow[]): EditorDocument {
  return {
    draftId: DRAFT,
    owner: { kind: "Store", sellerPartyId: null },
    serviceCode: "tooba.post",
    optionCode: null,
    revision: 2,
    createdAt: "2026-10-24T00:00:00+03:30",
    updatedAt: "2026-10-24T19:05:52.452036+00:00",
    type: { draftId: DRAFT, revision: 2, shippingTypeId: null, resolution: "Unconfigured", shippingType: null },
    settings: null,
    presentation: {
      methodTranslations: [],
      groupTitles: [],
      contentLanguages: [
        {
          languageId: "11111111-1111-4111-8111-111111111111",
          code: "fa",
          culture: "fa-IR",
          urlPrefix: "",
          direction: "rtl",
          displayName: "فارسی",
          nativeName: "فارسی",
          isDefault: true,
          sortOrder: 1,
        },
      ],
      defaultContentLanguageId: "11111111-1111-4111-8111-111111111111",
      hasDefaultLanguageName: true,
    },
    weeklySchedule: null,
    regionGroups: [
      {
        groupId: GROUP,
        order: 0,
        currencyCode: "IRR",
        areas: [{ kind: "City", id: CITY }],
        rows,
        titles: [],
      },
    ],
    destinationFacts: [
      {
        kind: "City",
        id: CITY,
        resolution: "Resolved",
        label: "تهران",
        area: {
          kind: "City",
          countryId: IRAN,
          administrativeAreaId: TEHRAN,
          cityId: CITY,
          serviceAreaId: null,
        },
      },
    ],
  };
}

/* ————————————————————————— A: sequential destination path ————————————————————————— */

test("225-A1 the deepest chosen stage is the ONE terminal; ancestors are only the path", () => {
  let choice = chooseCountry(
    { ...pathChoice(emptyStagedPath()) },
    option(IRAN, "ایران", null),
  );
  choice = chooseAdministrativeArea(choice, option(TEHRAN, "تهران", IRAN));
  choice = chooseCity(choice, option(CITY, "تهران", TEHRAN));
  choice = chooseServiceArea(choice, option(REGION, "ناحیه ۳", CITY));
  const staged = pathChoice(choice);
  assert.equal(choiceTerminal(staged)?.kind, "ServiceArea");
  assert.equal(choiceTerminal(staged)?.id, REGION);
  // Only ONE selection is produced: the terminal. The three ancestors are the breadcrumb, not selections.
  assert.equal([staged.country, staged.administrativeArea, staged.city].filter((entry) => entry !== null).length, 3);
});

test("225-A2 changing a parent clears every dependent stage (country > area > city > service area)", () => {
  let choice = chooseCountry(pathChoice(emptyStagedPath()), option(IRAN, "ایران", null));
  choice = chooseAdministrativeArea(choice, option(TEHRAN, "تهران", IRAN));
  choice = chooseCity(choice, option(CITY, "تهران", TEHRAN));
  choice = chooseServiceArea(choice, option(REGION, "ناحیه ۳", CITY));

  const afterCountry = chooseCountry(choice, option(IRAN, "ایران", null));
  assert.equal(afterCountry.city, null);
  assert.equal(afterCountry.serviceArea, null);
  assert.equal(afterCountry.administrativeArea, null);

  const afterAreaClear = clearAdministrativeArea(choice);
  assert.equal(afterAreaClear.administrativeArea, null);
  assert.equal(afterAreaClear.city, null);
  assert.equal(afterAreaClear.serviceArea, null);

  const afterCityClear = clearCity(choice);
  assert.equal(afterCityClear.city, null);
  assert.equal(afterCityClear.serviceArea, null);
  assert.notEqual(afterCityClear.administrativeArea, null);
});

test("225-A3 staging is never an error: only an explicit Add refuses an already stored terminal", () => {
  const staged = pathChoice({
    country: option(IRAN, "ایران", null),
    administrativeArea: option(TEHRAN, "تهران", IRAN),
    city: option(CITY, "تهران", TEHRAN),
    serviceArea: null,
  });
  const terminal = choiceTerminal(staged);
  assert.ok(terminal);
  const stored: SelectedAreaRead[] = [{ kind: "City", id: CITY }];
  assert.equal(
    isDuplicateSelection(terminal, (area) => stored.some((entry) => entry.kind === area.kind && entry.id === area.id)),
    true,
  );
  // The staged path itself stays explorable (no render-time duplicate check is implied by this module).
  assert.equal(staged.city?.id, CITY);
});

test("225-A4 no city list is read before an administrative area is chosen", async () => {
  const queries: GeographyQuery[] = [];
  const reader = async (query: GeographyQuery): Promise<GeographyReadOutcome> => {
    queries.push(query);
    return { state: "ok", options: [option("x", "x", null)] };
  };
  const coordinator = createDestinationReadCoordinator(reader, () => undefined);
  coordinator.update({
    enabled: true,
    languageId: "fa",
    countryId: IRAN,
    administrativeAreaId: null,
    cityId: null,
    citiesRequireAdministrativeArea: true,
  });
  await new Promise((resolve) => setTimeout(resolve, 10));
  assert.equal(queries.some((query) => query.kind === "City"), false, "no eager country-wide city read");
  assert.deepEqual(coordinator.getState().lists.cities, []);
  assert.equal(emptyDestinationReadState().settled.cities, false);
  coordinator.dispose();
});

test("225-A5 Add availability needs the verified chain, not only staged flags", () => {
  const city = option(CITY, "تهران", TEHRAN);
  const staged = pathChoice({
    country: option(IRAN, "ایران", null),
    administrativeArea: option(TEHRAN, "تهران", IRAN),
    city,
    serviceArea: null,
  });
  const evidence = {
    lists: {
      countries: [option(IRAN, "ایران", null)],
      areas: [option(TEHRAN, "تهران", IRAN)],
      cities: [city],
      serviceAreas: [],
    },
    settled: { countries: true, areas: true, cities: true, serviceAreas: false },
  };
  assert.equal(destinationAddAvailable(staged, evidence, false), true);
  assert.equal(destinationAddAvailable(staged, evidence, true), false, "a busy/view editor cannot add");
  // Eligibility is decided by the CURRENT verified rows, never by the staged copy: a city row whose real
  // parent is not the chosen area cannot be added under that area.
  const mismatch = {
    ...evidence,
    lists: { ...evidence.lists, cities: [option(CITY, "تهران", "dddddddd-dddd-4ddd-8ddd-ddddddddddd9")] },
  };
  assert.equal(destinationAddAvailable(staged, mismatch, false), false);
});

/* ————————————————————————— B: isolated rate-row modal ————————————————————————— */

function draft(overrides: Partial<RateRowDraft> = {}): RateRowDraft {
  return { ...newRateRow("local-1"), freightAmount: "100", packagingAmount: "0", ...overrides };
}

test("225-B1 an edit replaces exactly that stable row key and never reorders the others", () => {
  let state = { [GROUP]: [draftFromRow(row()), draftFromRow(row({ rateRowId: ROW_B, minWeightGrams: 2000, maxWeightGrams: 3000 }))] };
  const edited = withRowField(state, GROUP, ROW_A, "freightAmount", "150");
  const committed = withConfirmedRow(edited, GROUP, { ...edited[GROUP]![0]!, packagingAmount: "7" });
  assert.equal(committed[GROUP]!.length, 2);
  assert.equal(committed[GROUP]![0]!.rowKey, ROW_A);
  assert.equal(committed[GROUP]![0]!.rateRowId, ROW_A);
  assert.equal(committed[GROUP]![0]!.packagingAmount, "7");
  assert.equal(committed[GROUP]![1]!.rowKey, ROW_B);
});

test("225-B2 a new confirmed row is appended once and keeps no server id", () => {
  const state = { [GROUP]: [draftFromRow(row())] };
  const confirmed = withConfirmedRow(state, GROUP, draft({ rowKey: "new-1" }));
  assert.equal(confirmed[GROUP]!.length, 2);
  assert.equal(confirmed[GROUP]![1]!.rateRowId, null);
  assert.equal(withConfirmedRow(confirmed, GROUP, draft({ rowKey: "new-1" }))[GROUP]!.length, 2);
});

test("225-B3 an EXACT duplicate condition tuple is ambiguity even when the prices differ", () => {
  const first = draft({ minWeightGrams: "0", maxWeightGrams: "1000" });
  const same = draft({ rowKey: "other", minWeightGrams: "0", maxWeightGrams: "1000", freightAmount: "999" });
  assert.equal(conditionTuple(first, "IRR"), conditionTuple(same, "IRR"));
  assert.equal(conflictingRowKey(first, [first, same], "IRR"), "other");
  assert.equal(conflictingRowKey(first, [first], "IRR"), null, "self is excluded");
});

test("225-B4 half-open intersection is refused, touching endpoints are not", () => {
  const base = draft({ minWeightGrams: "0", maxWeightGrams: "1000" });
  const overlapping = draft({ rowKey: "o", minWeightGrams: "500", maxWeightGrams: "1500" });
  const touching = draft({ rowKey: "t", minWeightGrams: "1000", maxWeightGrams: "1500" });
  assert.equal(conflictingRowKey(base, [base, overlapping], "IRR"), "o");
  assert.equal(conflictingRowKey(base, [base, touching], "IRR"), null);
  // A different axis is an ALTERNATIVE only when every axis overlaps; a second axis that misses is lawful.
  const otherAxis = draft({ rowKey: "c", minWeightGrams: "0", maxWeightGrams: "1000", minItemCount: "5", maxItemCount: "9" });
  assert.equal(conflictingRowKey(draft({ minWeightGrams: "0", maxWeightGrams: "1000", minItemCount: "1", maxItemCount: "3" }), [otherAxis], "IRR"), null);
});

test("225-B5 zero freight/packaging are valid amounts; a blank one is required", () => {
  assert.equal(rowConditionIssue(draft({ freightAmount: "0", packagingAmount: "0" }), "IRR"), null);
  assert.equal(rowConditionIssue(draft({ freightAmount: "", packagingAmount: "0" }), "IRR"), "amountRequired");
  assert.equal(rowConditionIssue(draft({ freightAmount: "100", packagingAmount: "" }), "IRR"), "amountRequired");
});

/* ————————————————————————— C: integer-rial money ————————————————————————— */

test("225-C1 an integral IRR token is displayed as an integer, never with a forced .00", () => {
  assert.equal(displayMoneyText("120000.00", "integer"), "120,000");
  assert.equal(displayMoneyText("5000", "integer"), "5,000");
  assert.equal(displayMoneyText("0", "integer"), "0");
  // A real fraction is NOT rounded away: it stays visible so the honest refusal can be read.
  assert.equal(displayMoneyText("0.50", "integer"), "0.50");
  // A non-IRR currency keeps the stored scale exactly.
  assert.equal(displayMoneyText("120000.00", "scaled"), "120,000.00");
});

test("225-C1b a whole IRR token is never reported as a visible fraction", () => {
  // The mounted editor flagged `1,000` as `aria-invalid` because a token with NO separator was read as a
  // fraction; a whole amount must never be reported as decimal.
  for (const whole of ["0", "1000", "120000", "120000.00", "120000.0"]) {
    assert.equal(moneyHasFraction(whole), false, whole);
  }
  for (const fractional of ["0.50", "1000.01", "120000.005"]) {
    assert.equal(moneyHasFraction(fractional), true, fractional);
  }
});

test("225-C2 a typing edit never turns its own display grouping into an invalid amount", () => {
  // The PRIOR token was lawful, so the separators in the new text are the field's own grouping.
  const edit = interpretMoneyTyping("1200000", "1,200000", 9);
  assert.equal(edit.valid, true);
  assert.equal(edit.raw, "1200000");
});

test("225-C3 a strict paste keeps unsupported text exactly as typed", () => {
  for (const text of ["-100", "1e3", "12,34"]) {
    const edit = interpretMoneyEdit(text, text.length);
    assert.equal(edit.valid, false, text);
    assert.equal(edit.raw, text.replace(/\u066c/g, ","));
  }
  assert.equal(interpretMoneyEdit("1,850,000", 9).raw, "1850000");
  assert.equal(interpretMoneyEdit("۵٬۰۰۰", 5).raw, "5000");
});

test("225-C4 a STORED fraction is tolerated only while untouched; an edit is refused honestly", () => {
  const stored = row({ minMerchandiseAmount: "0.50" });
  const untouched = draftFromRow(stored);
  const tolerated = toleratedStoredFractions(untouched, storedRateAmounts(stored));
  assert.deepEqual(tolerated, ["minMerchandiseAmount"]);
  assert.equal(rowConditionIssue(untouched, "IRR", tolerated), null);

  // The operator edits another money field: the stored fraction is STILL untouched, so it stays lawful…
  const editedFreight = { ...untouched, freightAmount: "130000.00" };
  assert.equal(rowConditionIssue(editedFreight, "IRR", toleratedStoredFractions(editedFreight, storedRateAmounts(stored))), null);

  // …but changing the fractional amount itself is refused, and nothing is rounded.
  const editedFraction = { ...untouched, minMerchandiseAmount: "0.75" };
  assert.equal(
    rowConditionIssue(editedFraction, "IRR", toleratedStoredFractions(editedFraction, storedRateAmounts(stored))),
    "rialFractionInvalid",
  );
  assert.equal(toleratedStoredFractions(editedFraction, storedRateAmounts(stored)).length, 0);
});

test("225-C5 the whole-area issue list never blocks Apply on an untouched stored fraction", () => {
  const stored = row({ minMerchandiseAmount: "0.50" });
  const document = documentWith([stored]);
  assert.deepEqual(rateIssues(document, { [GROUP]: [draftFromRow(stored)] }), []);

  const edited = { [GROUP]: [withRowField({ [GROUP]: [draftFromRow(stored)] }, GROUP, ROW_A, "freightAmount", "130000.00")[GROUP]![0]!] };
  assert.deepEqual(rateIssues(document, edited), []);

  const fractional = { [GROUP]: [{ ...draftFromRow(stored), minMerchandiseAmount: "0.75" }] };
  assert.deepEqual(
    rateIssues(document, fractional).map((issue) => issue.code),
    ["rialFractionInvalid"],
  );
});

/* ————————————————————————— A: stored breadcrumb after a reload ————————————————————————— */

function fact(
  kind: "Country" | "AdministrativeArea" | "City" | "ServiceArea",
  id: string,
  area: { countryId: string; administrativeAreaId: string | null; cityId: string | null; serviceAreaId: string | null },
  label = "",
): EditorDestinationFact {
  return { kind, id, resolution: "Resolved", area: { kind, ...area }, label };
}

const LABELS: Record<string, string> = {
  [IRAN]: "ایران",
  [TEHRAN]: "تهران",
  [CITY]: "تهران",
  [REGION]: "ناحیه ۳",
};

function fakeReader(seen: string[]): (query: GeographyQuery) => Promise<GeographyReadOutcome> {
  return async (query) => {
    seen.push(`${query.kind}:${query.countryId ?? ""}:${query.administrativeAreaId ?? ""}:${query.cityId ?? ""}`);
    const rows =
      query.kind === "Country"
        ? [option(IRAN, "ایران", null, "Country")]
        : query.kind === "AdministrativeArea"
          ? [option(TEHRAN, "تهران", IRAN, "AdministrativeArea")]
          : query.kind === "City"
            ? [option(CITY, "تهران", query.administrativeAreaId ?? null, "City")]
            : [option(REGION, "ناحیه ۳", CITY, "ServiceArea")];
    return { state: "ok", options: rows };
  };
}

test("225-A6 a stored selection's breadcrumb is rebuilt from real parent reads, not the session", async () => {
  const chains = storedTrailChains([
    fact("ServiceArea", REGION, { countryId: IRAN, administrativeAreaId: TEHRAN, cityId: CITY, serviceAreaId: REGION }, "ناحیه ۳"),
  ]);
  assert.equal(chains.length, 1);
  const seen: string[] = [];
  const trails = await readStoredTrailSegments(chains, "fa", fakeReader(seen));
  assert.deepEqual(trails.get(`ServiceArea:${REGION.toLowerCase()}`), ["ایران", "تهران", "تهران", "ناحیه ۳"]);
  // Bounded SET reads: one per ancestor LEVEL, never one per chip and never a service-area read.
  assert.equal(seen.length, 3);
});

test("225-A7 two stored chips under the same city share ONE read per level (no per-chip N+1)", async () => {
  const second = "dddddddd-dddd-4ddd-8ddd-ddddddddddd5";
  const chains = storedTrailChains([
    fact("ServiceArea", REGION, { countryId: IRAN, administrativeAreaId: TEHRAN, cityId: CITY, serviceAreaId: REGION }, "ناحیه ۳"),
    fact("ServiceArea", second, { countryId: IRAN, administrativeAreaId: TEHRAN, cityId: CITY, serviceAreaId: second }, "ناحیه ۴"),
  ]);
  const seen: string[] = [];
  const trails = await readStoredTrailSegments(chains, "fa", fakeReader(seen));
  assert.equal(seen.length, 3);
  assert.deepEqual(trails.get(`ServiceArea:${second.toLowerCase()}`), ["ایران", "تهران", "تهران", "ناحیه ۴"]);
});

test("225-A8 a country-wide city never triggers a country-wide city read and keeps a two-segment path", async () => {
  const chains = storedTrailChains([
    fact("City", CITY, { countryId: IRAN, administrativeAreaId: null, cityId: CITY, serviceAreaId: null }, "تهران"),
  ]);
  const seen: string[] = [];
  const trails = await readStoredTrailSegments(chains, "fa", fakeReader(seen));
  assert.ok(!seen.some((entry) => entry.startsWith("City:")));
  assert.deepEqual(trails.get(`City:${CITY.toLowerCase()}`), ["ایران", "تهران"]);
});

test("225-A9 an unproven level is absent, never invented; an unproven ancestor publishes no path", async () => {
  const serviceArea = storedTrailChains([
    fact("ServiceArea", REGION, { countryId: IRAN, administrativeAreaId: TEHRAN, cityId: CITY, serviceAreaId: REGION }, "ناحیه ۳"),
  ]);
  // The city label is unavailable: the path would silently skip the city, so nothing is published.
  const failing = async (query: GeographyQuery): Promise<GeographyReadOutcome> =>
    query.kind === "City" ? { state: "failure", failure: { kind: "unavailable" } } : fakeReader([])(query);
  assert.equal((await readStoredTrailSegments(serviceArea, "fa", failing)).size, 0);

  // A country label that the read did not prove means the path cannot start at its real root, so nothing
  // is published — the chip keeps its own proven leaf name instead of a truncated path.
  const city = storedTrailChains([
    fact("City", CITY, { countryId: IRAN, administrativeAreaId: null, cityId: CITY, serviceAreaId: null }, "تهران"),
  ]);
  const countryless = async (query: GeographyQuery): Promise<GeographyReadOutcome> =>
    query.kind === "Country" ? { state: "ok", options: [] } : fakeReader([])(query);
  assert.equal((await readStoredTrailSegments(city, "fa", countryless)).size, 0);
});

test("225-A11 a country selection's path is the country itself, never repeated as root+leaf", async () => {
  const chains = storedTrailChains([
    fact("Country", IRAN, { countryId: IRAN, administrativeAreaId: null, cityId: null, serviceAreaId: null }, "ایران"),
  ]);
  const trails = await readStoredTrailSegments(chains, "fa", fakeReader([]));
  assert.deepEqual(trails.get(`Country:${IRAN.toLowerCase()}`), ["ایران"]);
});

test("225-A10 unresolved facts contribute no chain, so no breadcrumb is fabricated for them", () => {
  assert.deepEqual(
    storedTrailChains([
      { kind: "City", id: CITY, resolution: "Unresolved", area: null, label: null },
      { kind: "Country", id: IRAN, resolution: "Resolved", area: { kind: "Country", countryId: IRAN, administrativeAreaId: null, cityId: null, serviceAreaId: null }, label: "ایران" },
    ]).map((chain) => chain.kind),
    ["Country"],
  );
});
