/**
 * TB-225-R1 — focused regressions for the three source-confirmed defects of 225.
 *
 * These execute the SAME logical functions the field and the modal call (never a regex over the source):
 * the typing/backspace/delete interpreters, the display+caption pair, and the modal's own confirm decision.
 */
import assert from "node:assert/strict";
import test from "node:test";

import {
  draftFromRow,
  newRateRow,
  rateConfirmIssue,
  storedRateAmounts,
  toleratedStoredFractions,
} from "./model/editor-rates.ts";
import type { EditorRateRow } from "./model/editor-document.ts";
import {
  caretOffsetFor,
  displayMoneyText,
  interpretMoneyBackspace,
  interpretMoneyDelete,
  interpretMoneyEdit,
  interpretMoneyTyping,
} from "./model/money-text.ts";

const ROW_A = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";

/** One stored row of an IRR area (its whole amounts are lawful whole rial). */
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

/* ——————————————————— BLOCKER 1: invalid text is never healed into different money ——————————————————— */

test("225R1-1 an invalid pasted token cannot be healed by the next keystroke", () => {
  // Operator pastes the malformed `12,34`: the strict reader keeps it as typed and invalid.
  const pasted = interpretMoneyEdit("12,34", 5);
  assert.equal(pasted.valid, false);
  assert.equal(pasted.raw, "12,34");

  // Typing `5` at the end must NOT turn that same text into the accepted 12345.
  const typed = interpretMoneyTyping(pasted.raw, "12,345", 6);
  assert.equal(typed.valid, false, "an appended digit must not heal the operator's own separator");
  assert.equal(typed.raw, "12,345");

  // The field keeps showing exactly what was typed, so the refusal reason stays readable.
  assert.equal(displayMoneyText(typed.raw, "scaled"), "12,345");
});

test("225R1-1b only the operator's own correction makes an invalid token lawful again", () => {
  const pasted = interpretMoneyEdit("12,34", 5);
  // Deleting the offending separator really is a correction of the text itself.
  const corrected = interpretMoneyTyping(pasted.raw, "1234", 3);
  assert.equal(corrected.valid, true);
  assert.equal(corrected.raw, "1234");

  // Replacing the entire selection with a lawful amount is a correction too.
  const replaced = interpretMoneyTyping(pasted.raw, "125000", 6);
  assert.equal(replaced.valid, true);
  assert.equal(replaced.raw, "125000");

  // `1,2,3` deleted down to `1,23` is still the operator's malformed text: no silent acceptance.
  const partial = interpretMoneyTyping("1,2,3", "1,23", 4);
  assert.equal(partial.valid, false);
  assert.equal(partial.raw, "1,23");
});

test("225R1-1c signed/exponent/letter text keeps its exact characters through later typing", () => {
  for (const invalid of ["-100", "1e3", "abc100", "12.3.4"]) {
    const edit = interpretMoneyEdit(invalid, invalid.length);
    assert.equal(edit.valid, false, invalid);
    const appended = interpretMoneyTyping(edit.raw, `${edit.raw}7`, edit.raw.length + 1);
    assert.equal(appended.valid, false, invalid);
    assert.equal(appended.raw, `${invalid}7`, invalid);
  }
});

/* ——————————————————— BLOCKER 2: the caret is mapped through the separator removal ——————————————————— */

test("225R1-2 a middle insertion anchors the caret on the real digit, not the shorter text", () => {
  // Field shows `1,234`; the operator inserts `9` after the `2`: the browser reports text `1,2934`, caret 4.
  const edit = interpretMoneyTyping("1234", "1,2934", 4);
  assert.equal(edit.raw, "12934");
  assert.equal(edit.valid, true);
  // Logical digits before the caret are 1,2,9 — NOT four digits.
  assert.deepEqual(edit.caret, { whole: 3, fraction: 0, inFraction: false });
  assert.equal(displayMoneyText(edit.raw, "scaled"), "12,934");
  assert.equal(caretOffsetFor("12,934", edit.caret), 4, "the caret stays right after the inserted 9");
});

test("225R1-2b a deletion inside a grouped value maps the caret the same way", () => {
  // `1,234` with the `2` deleted → browser text `1,34`, caret 2 (after the 1).
  const edit = interpretMoneyTyping("1234", "1,34", 2);
  assert.equal(edit.raw, "134");
  assert.equal(edit.valid, true);
  assert.equal(caretOffsetFor(displayMoneyText(edit.raw, "scaled"), edit.caret), 1);
});

test("225R1-2c sequential typing of five million groups without ever going invalid", () => {
  let raw = "";
  let display = "";
  for (const digit of "5000000") {
    const text = `${display}${digit}`;
    const edit = interpretMoneyTyping(raw, text, text.length);
    assert.equal(edit.valid, true, text);
    raw = edit.raw;
    display = displayMoneyText(raw, "integer");
    // The caret always lands at the end of what the operator just typed.
    assert.equal(caretOffsetFor(display, edit.caret), display.length, text);
  }
  assert.equal(raw, "5000000");
  assert.equal(display, "5,000,000");
});

test("225R1-2d zero is lawful and local digits keep their digit positions", () => {
  const zero = interpretMoneyTyping("", "0", 1);
  assert.equal(zero.raw, "0");
  assert.equal(zero.valid, true);

  // Persian digits with the Arabic thousands separator: normalized, and the caret counts real digits.
  const persian = interpretMoneyTyping("1200", "۱٬۲۰۰", 3);
  assert.equal(persian.raw, "1200");
  assert.equal(persian.valid, true);
  assert.equal(persian.caret.whole, 2);
  // Two real digits precede the caret, so it sits right after `2` in the re-grouped `1,200`.
  assert.equal(caretOffsetFor("1,200", persian.caret), 3);
});

test("225R1-2e comma-adjacent Backspace and Delete remove the digit, not just the separator", () => {
  const backspace = interpretMoneyBackspace("12,000", 3);
  assert.notEqual(backspace, null);
  assert.equal(backspace?.raw, "1000");
  assert.equal(backspace?.valid, true);

  const del = interpretMoneyDelete("12,000", 2);
  assert.notEqual(del, null);
  assert.equal(del?.raw, "1200");
  assert.equal(del?.valid, true);
  // Nothing happens on a Delete that is not directly before a separator.
  assert.equal(interpretMoneyDelete("12,000", 0), null);
});

/* ——————————————————— MODAL: tolerance is re-derived per confirm ——————————————————— */

test("225R1-3 an UNTOUCHED stored fraction is still preserved by the modal", () => {
  const stored = row({ freightAmount: "100.50" });
  const draft = draftFromRow(stored);
  assert.deepEqual(toleratedStoredFractions(draft, storedRateAmounts(stored)), ["freightAmount"]);
  assert.equal(rateConfirmIssue(draft, [draft], "IRR", storedRateAmounts(stored), true), null);
});

test("225R1-3b an EDITED non-zero fraction is refused before the modal can commit", () => {
  const stored = row({ freightAmount: "100.50" });
  const baseline = storedRateAmounts(stored);
  const edited = { ...draftFromRow(stored), freightAmount: "200.75" };

  // The stale open-time tolerance (computed from the STORED value) must not be what the confirm uses.
  assert.deepEqual(toleratedStoredFractions(draftFromRow(stored), baseline), ["freightAmount"]);
  assert.deepEqual(toleratedStoredFractions(edited, baseline), []);
  assert.deepEqual(rateConfirmIssue(edited, [edited], "IRR", baseline, true), {
    kind: "shape",
    code: "rialFractionInvalid",
  });
});

test("225R1-3c a view/busy state refuses the confirm silently and never reaches the parent", () => {
  const draft = newRateRow("new-x");
  const complete = { ...draft, freightAmount: "1000", packagingAmount: "0" };
  assert.equal(rateConfirmIssue(complete, [complete], "IRR", null, true), null);
  assert.deepEqual(rateConfirmIssue(complete, [complete], "IRR", null, false), { kind: "blocked" });
});

test("225R1-3d self is still excluded by stable key and the duplicate scope stays the same area", () => {
  const stored = row({ freightAmount: "1000" });
  const draft = draftFromRow(stored);
  // The row itself is in the list: confirming its own unchanged values is NOT a duplicate.
  assert.equal(rateConfirmIssue(draft, [draft], "IRR", storedRateAmounts(stored), true), null);

  const twin = { ...draftFromRow(stored), rowKey: "twin" };
  assert.deepEqual(rateConfirmIssue(draft, [draft, twin], "IRR", storedRateAmounts(stored), true), {
    kind: "duplicate",
  });
});
