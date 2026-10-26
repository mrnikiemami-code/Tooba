/**
 * TB-224 focused facts: ONE delivery range per delivery AREA, no early duplicate on branch exploration,
 * and exact money display/typing. These execute the real models/helpers the screen uses.
 */
import assert from "node:assert/strict";
import test from "node:test";
import type { EditorDocument, EditorRateRow } from "./model/editor-document.ts";
import { createRatesForm, ratesDirty, rowIssue, withAddedRowInheriting, withRemovedRow, type RateRowDraft } from "./model/editor-rates.ts";
import {
  applyAreaRange,
  areaRangeIssue,
  areaRangeOf,
  inheritedAreaRange,
} from "./model/editor-area-range.ts";
import {
  caretAnchorAt,
  caretForDigitIndex,
  caretOffsetFor,
  countDigits,
  groupMoneyText,
  interpretMoneyEdit,
  readMoneyText,
  sanitizeMoneyText,
} from "./model/money-text.ts";
import { formatDecimalText } from "./ui/authoring-format.ts";
import {
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseLevel,
  destinationAddAvailable,
  isDuplicateSelection,
  type DestinationChoice,
} from "./model/destination-choice.ts";
import { authoringMessages } from "./messages/authoring-messages.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const FA = "44444444-4444-4444-8444-444444444441";
const G1 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";
const G2 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2";
const R1 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1";
const R2 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2";
const R3 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb3";
const CITY = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";
const SERVICE = "cccccccc-cccc-4ccc-8ccc-ccccccccccc2";
const COUNTRY = "dddddddd-dddd-4ddd-8ddd-ddddddddddd1";
const PROVINCE = "dddddddd-dddd-4ddd-8ddd-ddddddddddd2";

function row(overrides: Partial<EditorRateRow> = {}): EditorRateRow {
  return {
    rateRowId: R1,
    minWeightGrams: 0,
    maxWeightGrams: 5000,
    minItemCount: null,
    maxItemCount: null,
    minMerchandiseAmount: "0.50",
    maxMerchandiseAmount: null,
    freightAmount: "120000.00",
    packagingAmount: "5000.00",
    minDeliveryDays: 1,
    maxDeliveryDays: 3,
    ...overrides,
  };
}

function documentWith(overrides: Partial<EditorDocument> = {}): EditorDocument {
  return {
    draftId: DRAFT,
    owner: { kind: "Store", sellerPartyId: null },
    serviceCode: "tooba.post",
    optionCode: null,
    revision: 3,
    createdAt: "2026-10-24T00:15:00+03:30",
    updatedAt: "2026-10-24T01:45:10+03:30",
    type: { draftId: DRAFT, revision: 3, shippingTypeId: null, resolution: "Unconfigured", shippingType: null },
    settings: null,
    presentation: {
      methodTranslations: [{ languageId: FA, name: "پست", description: null }],
      groupTitles: [],
      contentLanguages: [
        {
          languageId: FA,
          code: "fa",
          culture: "fa-IR",
          urlPrefix: "",
          direction: "rtl",
          displayName: "fa",
          nativeName: "fa",
          isDefault: true,
          sortOrder: 1,
        },
      ],
      defaultContentLanguageId: FA,
      hasDefaultLanguageName: true,
    },
    weeklySchedule: null,
    regionGroups: [
      {
        groupId: G1,
        order: 0,
        currencyCode: "IRR",
        areas: [{ kind: "City", id: CITY }],
        rows: [row(), row({ rateRowId: R2, minWeightGrams: 5001, maxWeightGrams: 100_000 })],
        titles: [],
      },
      {
        groupId: G2,
        order: 1,
        currencyCode: "IRR",
        areas: [{ kind: "ServiceArea", id: SERVICE }],
        rows: [row({ rateRowId: R3, minDeliveryDays: 5, maxDeliveryDays: 7 })],
        titles: [],
      },
    ],
    destinationFacts: null,
    ...overrides,
  };
}

/* ------------------------------------------------------------------ A: one range per AREA */

test("224-A1 the shared range writes every row of THAT area only", () => {
  const document = documentWith();
  const state = createRatesForm(document);
  const before = state[G2];
  assert.deepEqual(areaRangeOf(state[G1]!), { kind: "uniform", min: "1", max: "3" });

  const updated = applyAreaRange(state[G1]!, "2", "6");
  assert.equal(updated.length, 2, "every row of the area carries the chosen range");
  assert.ok(updated.every((entry) => entry.minDeliveryDays === "2" && entry.maxDeliveryDays === "6"));
  assert.ok(updated.every((entry) => entry.freightAmount === "120000.00"), "money is untouched by the range");
  assert.equal(state[G2], before, "the other area's rows keep their own identity and values");
  assert.deepEqual(areaRangeOf(before!), { kind: "uniform", min: "5", max: "7" });
});

test("224-A2 a new row inherits only its OWN area's chosen range", () => {
  const document = documentWith();
  const state = createRatesForm(document);
  const uniform = inheritedAreaRange(state[G1]!, null);
  assert.deepEqual(uniform, { min: "1", max: "3" });

  const added = withAddedRowInheriting(state, G1, "new-1", uniform);
  const created = added[G1]!.at(-1)!;
  assert.equal(created.minDeliveryDays, "1");
  assert.equal(created.maxDeliveryDays, "3");
  assert.deepEqual(areaRangeOf(added[G1]!), { kind: "uniform", min: "1", max: "3" });
  assert.equal(added[G2]!.length, 1, "the other area gains no row");

  // An EMPTY area stages a range locally; the first row inherits exactly that staged pair.
  const empty: readonly RateRowDraft[] = [];
  assert.equal(inheritedAreaRange(empty, null), null, "no staged range means no invented default");
  assert.deepEqual(inheritedAreaRange(empty, { min: "4", max: "9" }), { min: "4", max: "9" });
  assert.equal(inheritedAreaRange(empty, { min: "9", max: "4" }), null, "an inverted staged pair is not inherited");
});

test("224-A3 unequal stored durations are a LEGACY CONFLICT until an explicit choice", () => {
  const document = documentWith({
    regionGroups: [
      {
        groupId: G1,
        order: 0,
        currencyCode: "IRR",
        areas: [{ kind: "City", id: CITY }],
        rows: [row(), row({ rateRowId: R2, minDeliveryDays: 4, maxDeliveryDays: 8 })],
        titles: [],
      },
    ],
  });
  const state = createRatesForm(document);
  const range = areaRangeOf(state[G1]!);
  assert.equal(range.kind, "conflict");
  assert.deepEqual(range.kind === "conflict" ? range.pairs : [], [
    { min: "1", max: "3" },
    { min: "4", max: "8" },
  ]);
  assert.equal(inheritedAreaRange(state[G1]!, null), null, "a conflict has nothing to inherit");

  // A partial/invalid entry NEVER rewrites the stored differences.
  assert.equal(applyAreaRange(state[G1]!, "", "6"), state[G1]);
  assert.equal(applyAreaRange(state[G1]!, "2", ""), state[G1]);
  assert.equal(applyAreaRange(state[G1]!, "9", "6"), state[G1]);
  assert.deepEqual(
    state[G1]!.map((entry) => `${entry.minDeliveryDays}-${entry.maxDeliveryDays}`),
    ["1-3", "4-8"],
    "the original per-row values survive an unrelated/partial edit",
  );

  // Only a deliberate lawful pair normalizes them.
  const normalized = applyAreaRange(state[G1]!, "2", "6");
  assert.deepEqual(areaRangeOf(normalized), { kind: "uniform", min: "2", max: "6" });
});

test("224-A4 an untouched uniformly-valued area stays CLEAN (no dirty edit, no PUT body)", () => {
  const document = documentWith();
  const state = createRatesForm(document);
  assert.equal(ratesDirty(document, state), false);

  const applied = applyAreaRange(state[G1]!, "1", "3");
  assert.equal(applied, state[G1], "re-entering the SAME values changes nothing");
  assert.equal(ratesDirty(document, { ...state, [G1]: applied }), false);

  const changed = applyAreaRange(state[G1]!, "2", "6");
  assert.equal(ratesDirty(document, { ...state, [G1]: changed }), true);
});

test("224-A5 cancel restores the range from the SAME baseline (reseeding clears local choices)", () => {
  const document = documentWith();
  const seeded = createRatesForm(document);
  const edited = withRemovedRow({ ...seeded, [G1]: applyAreaRange(seeded[G1]!, "9", "12") }, G1, R2);
  assert.deepEqual(areaRangeOf(edited[G1]!), { kind: "uniform", min: "9", max: "12" });

  const reseeded = createRatesForm(document);
  assert.deepEqual(areaRangeOf(reseeded[G1]!), { kind: "uniform", min: "1", max: "3" });
  assert.equal(reseeded[G1]!.length, 2, "the removed row comes back from the baseline");
  assert.equal(ratesDirty(document, reseeded), false);
});

test("224-A6 range/duration validation and honest read text use the real domain bounds", () => {
  assert.equal(areaRangeIssue("1", "3"), null);
  assert.equal(areaRangeIssue("0", "0"), null);
  assert.equal(areaRangeIssue("", "3"), "durationRequired");
  assert.equal(areaRangeIssue("3", ""), "durationRequired");
  assert.equal(areaRangeIssue("1.5", "3"), "durationInvalid");
  assert.equal(areaRangeIssue("3", "1"), "durationInvalid");
  assert.equal(areaRangeIssue("0", "366"), "durationInvalid");

  const fa = authoringMessages("fa");
  assert.equal(fa.durationRequired.length > 0, true);
  assert.equal(fa.areaRangeConflict.includes("{pairs}"), true, "the conflict text states the differing pairs");
  assert.equal(fa.areaRangeUnset.length > 0, true);

  // The row no longer needs its own duration entry once the area carries it.
  assert.equal(rowIssue({ ...createRatesForm(documentWith())[G1]![0]! }), null);
});

/* ------------------------------------------------------------------ B: duplicate only at Add */

function option(kind: "Country" | "AdministrativeArea" | "City" | "ServiceArea", id: string, parentId: string | null, isActive = true) {
  return { kind, id, parentId, isActive, code: id.slice(0, 4), label: `label-${id.slice(0, 4)}` };
}

const evidence = {
  lists: {
    countries: [option("Country", COUNTRY, null)],
    areas: [option("AdministrativeArea", PROVINCE, COUNTRY)],
    cities: [option("City", CITY, PROVINCE)],
    serviceAreas: [option("ServiceArea", SERVICE, CITY)],
  },
  settled: { countries: true, areas: true, cities: true, serviceAreas: true },
};

test("224-B1 an already stored country can still be staged to reach a province/city", () => {
  const country = chooseCountry({ level: "Country", country: null, administrativeArea: null, city: null, serviceArea: null }, option("Country", COUNTRY, null));
  const stored = (area: { kind: string; id: string }) => area.id === COUNTRY;

  // No early rejection: the country stage is available although that exact country is already stored.
  assert.equal(destinationAddAvailable(country, evidence, false), true);
  assert.equal(isDuplicateSelection({ kind: "Country", id: COUNTRY }, stored), true, "submitting THAT level is refused");

  // Browsing deeper under the same stored country stays available.
  const provinceLevel: DestinationChoice = chooseLevel(country, "AdministrativeArea");
  const withProvince = chooseAdministrativeArea(provinceLevel, option("AdministrativeArea", PROVINCE, COUNTRY));
  const city = chooseCity(chooseLevel(withProvince, "City"), option("City", CITY, PROVINCE));
  assert.equal(destinationAddAvailable(city, evidence, false), true);
  assert.equal(isDuplicateSelection({ kind: "City", id: CITY }, stored), false, "the deeper terminal is not the stored one");
});

test("224-B2 verified/active/ancestry guard is still required for Add availability", () => {
  const country = chooseCountry({ level: "Country", country: null, administrativeArea: null, city: null, serviceArea: null }, option("Country", COUNTRY, null));
  assert.equal(destinationAddAvailable(country, evidence, true), false, "read-only/busy is never available");
  assert.equal(
    destinationAddAvailable(country, { ...evidence, settled: { ...evidence.settled, countries: false } }, false),
    false,
    "unsettled evidence",
  );
  assert.equal(
    destinationAddAvailable(
      country,
      { ...evidence, lists: { ...evidence.lists, countries: [option("Country", COUNTRY, null, false)] } },
      false,
    ),
    false,
    "inactive country",
  );
  const service = chooseCity(chooseLevel(country, "ServiceArea"), option("City", CITY, PROVINCE));
  assert.equal(destinationAddAvailable(service, evidence, false), false, "incomplete service-area ancestry");
});

/* ------------------------------------------------------------------ C/D: exact money */

test("224-C1 120000.00 is shown grouped and stays 120000.00 on the wire", () => {
  assert.equal(groupMoneyText("120000.00"), "120,000.00");
  assert.equal(groupMoneyText("5000.00"), "5,000.00");
  assert.equal(sanitizeMoneyText("120,000.00"), "120000.00");
  assert.equal(formatDecimalText("1850000.00", "en"), "1,850,000.00");
  assert.equal(groupMoneyText(""), "", "a deliberate blank is never turned into 0");
});

test("224-C2 typing, paste, deletion and local digits keep the exact value", () => {
  // typing the sixth digit into a grouped field: the canonical token is ungrouped
  const typed = interpretMoneyEdit("120,000.0", "120,000.0".length);
  assert.equal(typed.raw, "120000.0");
  assert.deepEqual(typed.caret, caretAnchorAt("120,000.0", "120,000.0".length));

  // pasting a grouped value must not change the number
  assert.equal(sanitizeMoneyText("1,850,000.00"), "1850000.00");
  assert.equal(sanitizeMoneyText(" 1,850,000 "), "1850000");
  // Persian/Arabic-Indic digits and the Arabic decimal separator
  assert.equal(sanitizeMoneyText("۱۲۰۰۰۰٫۵۰"), "120000.50");
  assert.equal(sanitizeMoneyText("١٢٣٤"), "1234");
  // decimals are preserved exactly, never padded or rounded
  assert.equal(sanitizeMoneyText("0.50"), "0.50");
  assert.equal(groupMoneyText("0.50"), "0.50");
  assert.equal(sanitizeMoneyText(".5"), "0.5");

  // deleting a separator only: the value is unchanged
  const deletedSeparator = interpretMoneyEdit("12000.00", 3);
  assert.equal(deletedSeparator.raw, "12000.00");

  // clearing the field means BLANK, not zero
  assert.equal(interpretMoneyEdit("", 0).raw, "");
});

test("224-C3 the caret is anchored to a DIGIT, so re-grouping cannot move it", () => {
  const display = groupMoneyText("1850000.00");
  assert.equal(display, "1,850,000.00");
  // caret right after "1,8" (digit anchor 2) must land after "8", not after the separator
  const anchor = caretAnchorAt("1,850,000.00", 3);
  assert.deepEqual(anchor, { whole: 2, fraction: 0, inFraction: false });
  assert.equal(caretOffsetFor(display, anchor), 3);
  assert.equal(caretForDigitIndex(display, 0), 0);
  assert.equal(caretForDigitIndex(display, countDigits(display)), display.length);
  // A malformed grouping is NOT silently reduced to its digits any more (TB-224-R1: it stays invalid).
  assert.equal(sanitizeMoneyText("1,2,3"), "1,2,3");
  assert.equal(readMoneyText("1,2,3").valid, false);
});
