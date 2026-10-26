/**
 * TB-224-R1 focused facts: a TYPED shared delivery range is the form state that blocks Apply, an INVALID
 * money text can never become a different accepted amount, and the caret survives the decimal separator
 * and local digits. These execute the real models the screen and the money input use.
 */
import assert from "node:assert/strict";
import test from "node:test";
import type { EditorDocument, EditorRateRow } from "./model/editor-document.ts";
import { createEditorForm } from "./model/editor-form.ts";
import { createGroupsForm, withAddedGroup as withAddedGroupDraft } from "./model/editor-groups.ts";
import { createRatesForm, ratesDirty, rowIssue, withAddedRowInheriting, withRowField } from "./model/editor-rates.ts";
import {
  areaRangeDraftIssues,
  areaRangeDraftsDirty,
  areaRangeEdit,
  createAreaRangeDrafts,
  inheritedAreaRange,
  storedAreaRangeText,
  type AreaRangeDrafts,
} from "./model/editor-area-range.ts";
import {
  caretAnchorAt,
  caretForDigitIndex,
  caretOffsetFor,
  countDigits,
  groupMoneyText,
  interpretMoneyEdit,
  readMoneyText,
} from "./model/money-text.ts";
import { buildAuthoringIntent, hasNoIssues } from "./model/authoring-intent.ts";
import { createWeekForm } from "./model/editor-week.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const FA = "44444444-4444-4444-8444-444444444441";
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
        rows,
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
  };
}

/** The composed intent exactly the way the screen builds it (group collection + typed ranges). */
function compose(
  document: EditorDocument,
  rates: ReturnType<typeof createRatesForm>,
  areaRanges: AreaRangeDrafts,
) {
  const languageIds = document.presentation.contentLanguages.map((language) => language.languageId);
  return buildAuthoringIntent(
    document,
    createEditorForm(document),
    rates,
    createWeekForm(document.weeklySchedule),
    createGroupsForm(document, languageIds),
    areaRanges,
  );
}

/* --------------------------------------------------------------- A: typed range is committed state */

test("224R1-A1 a partial or invalid typed range is REAL local work and blocks Apply", () => {
  const document = documentWith([row(), row({ rateRowId: R2, minWeightGrams: 5001 })]);
  const rates = createRatesForm(document);
  const drafts = createAreaRangeDrafts(document);
  assert.equal(areaRangeDraftsDirty(document, drafts, rates), false, "the seeded baseline is clean");
  assert.deepEqual(areaRangeDraftIssues(drafts, (key) => rates[key]), []);

  // The operator clears the minimum: the text is work, the stored 1..3 is NOT silently resubmitted.
  const cleared = areaRangeEdit(rates, drafts, G1, "", "3");
  assert.equal(cleared.rates, rates, "no row is rewritten by a partial pair");
  assert.equal(areaRangeDraftsDirty(document, cleared.drafts, cleared.rates), true, "blank text is dirty");
  assert.deepEqual(areaRangeDraftIssues(cleared.drafts, (key) => cleared.rates[key]), [
    { groupKey: G1, code: "durationRequired" },
  ]);
  const blankIntent = compose(document, cleared.rates, cleared.drafts);
  assert.equal(hasNoIssues(blankIntent.issues), false, "Apply is blocked by the typed text");
  assert.equal(blankIntent.edits.regionGroups, undefined, "nothing about the duration is sent");

  // A reversed pair is invalid and still blocks, without normalizing the stored rows.
  const reversed = areaRangeEdit(rates, drafts, G1, "9", "6");
  assert.deepEqual(
    reversed.rates[G1]!.map((entry) => `${entry.minDeliveryDays}-${entry.maxDeliveryDays}`),
    ["1-3", "1-3"],
    "the stored pair survives an invalid entry",
  );
  assert.equal(areaRangeDraftIssues(reversed.drafts, (key) => reversed.rates[key])[0]?.code, "durationInvalid");
  assert.equal(hasNoIssues(compose(document, reversed.rates, reversed.drafts).issues), false);

  // Out-of-domain and non-integer text are invalid too.
  for (const [min, max] of [["0", "366"], ["1.5", "3"], ["", ""]]) {
    const entry = areaRangeEdit(rates, drafts, G1, min!, max!);
    assert.equal(
      areaRangeDraftIssues(entry.drafts, (key) => entry.rates[key]).length,
      1,
      `${min}..${max} blocks Apply`,
    );
  }
});

test("224R1-A2 an applied lawful range moves to the rows and re-entering the baseline is clean again", () => {
  const document = documentWith([row(), row({ rateRowId: R2, minWeightGrams: 5001 })]);
  const rates = createRatesForm(document);
  const drafts = createAreaRangeDrafts(document);

  // 4..9 is applied to exactly this area's rows (the other area keeps its own identity and values).
  const applied = areaRangeEdit(rates, drafts, G1, "4", "9");
  assert.ok(applied.rates[G1]!.every((entry) => entry.minDeliveryDays === "4" && entry.maxDeliveryDays === "9"));
  assert.equal(applied.rates[G2], rates[G2], "the other area keeps its own rows and range");
  assert.deepEqual(areaRangeDraftIssues(applied.drafts, (key) => applied.rates[key]), []);
  const intent = compose(document, applied.rates, applied.drafts);
  assert.equal(intent.issues.areaRanges.length, 0);
  assert.equal(intent.localDirty, true);
  const g1 = intent.edits.regionGroups?.find((group) => group.groupKey === G1);
  const g2 = intent.edits.regionGroups?.find((group) => group.groupKey === G2);
  assert.ok(g1?.rows.every((entry) => entry.minDeliveryDays === 4 && entry.maxDeliveryDays === 9));
  assert.ok(g2?.rows.every((entry) => entry.minDeliveryDays === 5 && entry.maxDeliveryDays === 7));

  // Typing the BASELINE pair back is not work at all.
  const reverted = areaRangeEdit(applied.rates, applied.drafts, G1, "1", "3");
  assert.equal(areaRangeDraftsDirty(document, reverted.drafts, reverted.rates), false);
  assert.equal(ratesDirty(document, reverted.rates), false, "typing/grouping alone never dirties the rows");
  assert.equal(compose(document, reverted.rates, reverted.drafts).localDirty, false);
});

test("224R1-A3 an invalid edit AFTER a lawful one cannot silently save the previous duration", () => {
  const document = documentWith([row()]);
  const rates = createRatesForm(document);
  const applied = areaRangeEdit(rates, createAreaRangeDrafts(document), G1, "4", "9");
  // The operator keeps 4..9 in the rows but corrupts the maximum in the control.
  const broken = areaRangeEdit(applied.rates, applied.drafts, G1, "4", "9x");
  assert.deepEqual(
    broken.rates[G1]!.map((entry) => `${entry.minDeliveryDays}-${entry.maxDeliveryDays}`),
    ["4-9"],
    "the rows keep the last lawful pair",
  );
  const intent = compose(document, broken.rates, broken.drafts);
  assert.equal(intent.issues.areaRanges[0]?.code, "durationInvalid");
  assert.equal(hasNoIssues(intent.issues), false, "the corrupted text can never be applied as 4..9");
});

test("224R1-A4 legacy unequal rows stay untouched and clean until an explicit lawful pair", () => {
  const document = documentWith([row(), row({ rateRowId: R2, minDeliveryDays: 4, maxDeliveryDays: 8 })]);
  const rates = createRatesForm(document);
  const drafts = createAreaRangeDrafts(document);
  assert.deepEqual(storedAreaRangeText(document.regionGroups[0]!.rows), { min: "", max: "" });
  assert.equal(areaRangeDraftsDirty(document, drafts, rates), false, "a conflict is not pending work");
  assert.deepEqual(areaRangeDraftIssues(drafts, (key) => rates[key]), []);
  assert.equal(inheritedAreaRange(rates[G1]!, drafts[G1] ?? null), null, "nothing to inherit");

  // An UNRELATED money edit must not normalize or block because of the differing durations.
  const moneyEdited = withRowField(rates, G1, R1, "freightAmount", "130000");
  const untouchedIntent = compose(document, moneyEdited, drafts);
  assert.equal(untouchedIntent.issues.areaRanges.length, 0);
  assert.deepEqual(
    moneyEdited[G1]!.map((entry) => `${entry.minDeliveryDays}-${entry.maxDeliveryDays}`),
    ["1-3", "4-8"],
    "the legacy difference survives an unrelated edit",
  );

  // Only a deliberate lawful pair unifies them.
  const unified = areaRangeEdit(moneyEdited, drafts, G1, "2", "6");
  assert.ok(unified.rates[G1]!.every((entry) => entry.minDeliveryDays === "2" && entry.maxDeliveryDays === "6"));
  assert.equal(compose(document, unified.rates, unified.drafts).issues.areaRanges.length, 0);
});

test("224R1-A5 a row-less area cannot lose its staged range silently", () => {
  const document = documentWith([row()]);
  const rates = createRatesForm(document);
  const key = "group-new-1";
  const withEmptyGroup = { ...rates, [key]: [] };
  const drafts = createAreaRangeDrafts(document);

  const staged = areaRangeEdit(withEmptyGroup, drafts, key, "2", "5");
  assert.deepEqual(staged.drafts[key], { min: "2", max: "5" });
  assert.equal(areaRangeDraftsDirty(document, staged.drafts, staged.rates), true);
  assert.deepEqual(areaRangeDraftIssues(staged.drafts, (groupKey) => staged.rates[groupKey]), [
    { groupKey: key, code: "rowsRequired" },
  ]);
  const blocked = compose(document, staged.rates, staged.drafts);
  assert.equal(blocked.issues.areaRanges[0]?.code, "rowsRequired");
  assert.equal(hasNoIssues(blocked.issues), false, "Apply cannot pretend the range was saved");

  // The first row inherits that staged pair, and then the row really carries the range.
  const inherited = inheritedAreaRange([], staged.drafts[key] ?? null);
  assert.deepEqual(inherited, { min: "2", max: "5" });
  const withRow = withAddedRowInheriting(staged.rates, key, "new-1", inherited);
  assert.deepEqual(areaRangeDraftIssues(staged.drafts, (groupKey) => withRow[groupKey]), []);
  const groups = withAddedGroupDraft(createGroupsForm(document, [FA]), key);
  const withRowIntent = buildAuthoringIntent(
    document,
    createEditorForm(document),
    withRow,
    createWeekForm(document.weeklySchedule),
    groups,
    staged.drafts,
  );
  assert.equal(withRowIntent.issues.areaRanges.length, 0);
  const newGroup = withRowIntent.edits.regionGroups?.find((group) => group.groupKey === key);
  assert.equal(newGroup?.groupId, null);
  assert.ok(newGroup?.rows.every((entry) => entry.minDeliveryDays === 2 && entry.maxDeliveryDays === 5));

  // Clearing the text removes the pending range entirely.
  const cleared = areaRangeEdit(withEmptyGroup, staged.drafts, key, "", "");
  assert.equal(areaRangeDraftIssues(cleared.drafts, (groupKey) => cleared.rates[groupKey]).length, 0);
  assert.equal(areaRangeDraftsDirty(document, cleared.drafts, cleared.rates), false);
});

test("224R1-A6 a removed/foreign key never blocks or leaks into another area", () => {
  const document = documentWith([row()]);
  const rates = createRatesForm(document);
  const drafts = { ...createAreaRangeDrafts(document), "group-removed": { min: "7", max: "1" } };
  assert.deepEqual(areaRangeDraftIssues(drafts, (groupKey) => rates[groupKey]), [], "a stale key is ignored");
  assert.equal(areaRangeDraftsDirty(document, drafts, rates), false, "and is not pending work");
  // The SAME pair placed in the real area is judged normally.
  const poison = { ...createAreaRangeDrafts(document), [G1]: { min: "7", max: "1" } };
  assert.equal(areaRangeDraftIssues(poison, (groupKey) => rates[groupKey])[0]?.code, "durationInvalid");
});

test("224R1-A7 cancel reseeds the typed ranges from the SAME confirmed baseline", () => {
  const document = documentWith([row()]);
  const rates = createRatesForm(document);
  const edited = areaRangeEdit(rates, createAreaRangeDrafts(document), G1, "9", "12");
  assert.equal(areaRangeDraftsDirty(document, edited.drafts, edited.rates), true);

  const reseeded = createAreaRangeDrafts(document);
  assert.deepEqual(reseeded[G1], { min: "1", max: "3" });
  const reseededRates = createRatesForm(document);
  assert.equal(areaRangeDraftsDirty(document, reseeded, reseededRates), false);
  assert.deepEqual(areaRangeDraftIssues(reseeded, (key) => reseededRates[key]), []);
});

/* ----------------------------------------------------------- B: invalid money is never rewritten */

test("224R1-B1 unsupported money text stays VISIBLY invalid instead of becoming another amount", () => {
  for (const text of ["-100", "1e3", "abc100", "12.3.4", "12,34", "1,2,3", "1 850 000", "١٢٣٫٤٫٥"]) {
    assert.equal(readMoneyText(text).valid, false, `${text} is not a lawful amount`);
  }
  // The exact defect: removing the unsupported parts must never accept the remaining digits.
  assert.equal(readMoneyText("-100").raw, "-100");
  assert.equal(readMoneyText("1e3").raw, "1e3");
  assert.equal(readMoneyText("12,34").raw, "12,34");
  assert.notEqual(readMoneyText("-100").raw, "100");
  assert.notEqual(readMoneyText("1e3").raw, "13");
  assert.notEqual(readMoneyText("12,34").raw, "1234");

  // A row holding that text is refused by the real row validator and by the composed intent.
  const document = documentWith([row()]);
  const rates = withRowField(createRatesForm(document), G1, R1, "freightAmount", readMoneyText("-100").raw);
  assert.equal(rowIssue(rates[G1]![0]!), "amountInvalid");
  const intent = compose(document, rates, createAreaRangeDrafts(document));
  assert.equal(hasNoIssues(intent.issues), false, "Apply is blocked, no amount is sent");
});

test("224R1-B2 lawful display normalization is unchanged and exact", () => {
  assert.deepEqual(readMoneyText("120,000.00"), { raw: "120000.00", valid: true });
  assert.deepEqual(readMoneyText(" ۱۲۰٬۰۰۰٫۵۰ "), { raw: "120000.50", valid: true });
  assert.deepEqual(readMoneyText("١٢٣٤"), { raw: "1234", valid: true });
  assert.deepEqual(readMoneyText("0.50"), { raw: "0.50", valid: true });
  assert.deepEqual(readMoneyText(".5"), { raw: "0.5", valid: true }, "a leading separator is the same number");
  assert.deepEqual(readMoneyText(""), { raw: "", valid: true }, "a deliberate blank stays blank");
  assert.deepEqual(readMoneyText("1,850,000"), { raw: "1850000", valid: true });
  assert.equal(groupMoneyText("120000.00"), "120,000.00");
  assert.equal(groupMoneyText("0.50"), "0.50");
  assert.equal(groupMoneyText("-100"), "-100", "invalid text is never displayed as a grouped amount");
  assert.equal(groupMoneyText("1850000.00"), "1,850,000.00");

  // Grouping alone never dirties the row: the SAME canonical token keeps the baseline clean.
  const document = documentWith([row()]);
  const rates = withRowField(createRatesForm(document), G1, R1, "freightAmount", readMoneyText("120,000.00").raw);
  assert.equal(ratesDirty(document, rates), false);
  const changed = withRowField(rates, G1, R1, "freightAmount", readMoneyText("120,000.01").raw);
  assert.equal(ratesDirty(document, changed), true);
});

test("224R1-B3 the caret distinguishes both sides of the decimal separator and counts local digits", () => {
  // Typing `12`, `.`, `5`: the caret is AFTER the separator, so the 5 lands in the fraction.
  const afterDot = interpretMoneyEdit("12.", 3);
  assert.deepEqual(afterDot.caret, { whole: 2, fraction: 0, inFraction: true });
  assert.equal(afterDot.raw, "12.");
  const offset = caretOffsetFor(groupMoneyText(afterDot.raw), afterDot.caret);
  assert.equal(offset, 3, "the caret stays after the separator");
  const display = groupMoneyText(afterDot.raw);
  const typed = `${display.slice(0, offset)}5${display.slice(offset)}`;
  assert.equal(typed, "12.5", "the digit lands after the separator");
  assert.equal(readMoneyText(typed).raw, "12.5", "and the amount is 12.5, not 125");

  // The caret BEFORE the separator is a different position of the SAME text.
  const beforeDot = interpretMoneyEdit("12.", 2);
  assert.deepEqual(beforeDot.caret, { whole: 2, fraction: 0, inFraction: false });
  assert.equal(caretOffsetFor("12.", beforeDot.caret), 2);

  // Local digits are REAL digits for the anchor (previously they counted as zero).
  const persian = interpretMoneyEdit("۱۲.۵", 4);
  assert.equal(persian.raw, "12.5");
  assert.deepEqual(persian.caret, { whole: 2, fraction: 1, inFraction: true });
  assert.equal(countDigits("۱۲.۵"), 3);
  assert.equal(caretOffsetFor("12.5", persian.caret), 4);

  // Grouping re-insertion keeps the caret on the same digit; a comma deletion moves by one digit.
  assert.equal(caretAnchorAt("1,234.00", 3).whole, 2);
  assert.equal(caretForDigitIndex("1,234.00", 2), 3);
  const backspace = interpretMoneyEdit("1234", 1);
  assert.deepEqual(backspace.caret, { whole: 1, fraction: 0, inFraction: false });
  assert.equal(caretOffsetFor(groupMoneyText(backspace.raw), backspace.caret), 1);
  assert.equal(backspace.raw, "1234", "deleting a separator changes no digit");
});
