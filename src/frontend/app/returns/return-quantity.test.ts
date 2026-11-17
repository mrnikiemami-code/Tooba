/**
 * TB-277-R1 — EXACT quantity arithmetic for the receiving surface.
 *
 * The operator types a number of THINGS; every comparison in the receipt dialog must therefore be exact. These facts pin
 * the integer-millionth representation (`numeric(18,6)` units) that replaced floating-point `Number` arithmetic:
 *
 * 1. `0.1 + 0.2` is exactly `0.3` (the same split a `Number` would render as `0.30000000000000004`);
 * 2. `requested - received` never yields `0.19999999999999998`;
 * 3. `1` and `1.000000` are the SAME quantity, not two representations;
 * 4. Persian digits and the Persian decimal separator normalize instead of being rejected;
 * 5. a value beyond the persisted six decimals is refused, never rounded or clamped;
 * 6. the owning unit's quantum decides what the line may receive (0.5 of a piece is not a piece; 0.125 g is a mass).
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  addUnits,
  compareUnits,
  isAlignedToUnit,
  normalizeQuantityInput,
  parseExactUnits,
  parsePositiveExactUnits,
  quantityStep,
  subtractUnits,
  unitsFromWireNumber,
  unitsToExactText,
  unitsToWireNumber,
  QUANTITY_UNITS_PER_ONE,
} from "./return-quantity.ts";

test("fractional goods add exactly: 0.1 + 0.2 is 0.3 and never 0.30000000000000004", () => {
  const tenth = parsePositiveExactUnits("0.1") as number;
  const fifth = parsePositiveExactUnits("0.2") as number;
  const three = parsePositiveExactUnits("0.3") as number;

  assert.equal(addUnits(tenth, fifth), three);
  assert.equal(compareUnits(addUnits(tenth, fifth), three), 0);
  assert.equal(unitsToExactText(addUnits(tenth, fifth)), "0.3");

  // The same two values through JavaScript numbers are NOT equal to 0.3 — this is the defect the units fix.
  assert.notEqual(0.1 + 0.2, 0.3);
});

test("remaining coverage subtracts exactly instead of drifting a whole binary step", () => {
  const requested = unitsFromWireNumber(1) as number;
  const received = unitsFromWireNumber(0.8) as number;
  const remaining = subtractUnits(requested, received);

  assert.equal(remaining, 200_000);
  assert.equal(unitsToExactText(remaining), "0.2");
  assert.notEqual(1 - 0.8, 0.2); // the drift the integer path removes
  assert.equal(unitsToWireNumber(remaining), 0.2);
});

test("one and one-with-six-decimals are the same quantity, not two representations", () => {
  const whole = parseExactUnits("1") as number;
  const scaled = parseExactUnits("1.000000") as number;

  assert.equal(whole, QUANTITY_UNITS_PER_ONE);
  assert.equal(scaled, QUANTITY_UNITS_PER_ONE);
  assert.equal(compareUnits(whole, scaled), 0);
});

test("Persian digits and the Persian decimal separator are normalized, not rejected", () => {
  assert.equal(normalizeQuantityInput("۱٫۵"), "1.5");
  assert.equal(parsePositiveExactUnits("۱٫۵"), 1_500_000);
  assert.equal(parsePositiveExactUnits(" ۲ "), 2_000_000);
  assert.equal(parseExactUnits("٠.٢٥"), 250_000);
});

test("a quantity finer than the persisted scale is refused, never rounded or clamped", () => {
  assert.equal(parseExactUnits("1.0000001"), null);
  assert.equal(parseExactUnits("0.0000004"), null);
  assert.equal(parseExactUnits("1.000001"), 1_000_001);
  assert.equal(parsePositiveExactUnits("0"), null);
  assert.equal(parsePositiveExactUnits(""), null);
  assert.equal(parsePositiveExactUnits("1e3"), null);
});

test("the owning unit's quantum decides what the line may receive", () => {
  const half = parsePositiveExactUnits("0.5") as number;
  const gram = parsePositiveExactUnits("0.125") as number;
  const tooFine = parsePositiveExactUnits("0.0001") as number;
  const piece = parsePositiveExactUnits("3") as number;

  assert.equal(isAlignedToUnit(half, 0), false); // whole pieces only
  assert.equal(isAlignedToUnit(piece, 0), true);
  assert.equal(isAlignedToUnit(gram, 3), true); // grams accept three decimals
  assert.equal(isAlignedToUnit(tooFine, 3), false); // 0.0001 g is finer than the unit
  assert.equal(isAlignedToUnit(gram, 6), true); // a six-decimal unit accepts it
});

test("field steps follow the same unit precision", () => {
  assert.equal(quantityStep(0), "1");
  assert.equal(quantityStep(3), "0.001");
  assert.equal(quantityStep(6), "0.000001");
  assert.equal(quantityStep(99), "0.000001");
});
