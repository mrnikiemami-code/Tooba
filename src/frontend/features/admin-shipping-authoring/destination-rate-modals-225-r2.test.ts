/**
 * TB-225-R2 — the money field's EDIT PROVENANCE, executed as the same state transitions the component owns.
 *
 * `field()` is a faithful model of `MoneyField`: it keeps the committed token AND the provenance record tied
 * to that token, derives the visible text exactly as the component does, and exposes the same
 * paste / typing / backspace / delete / external-reseed entries. Nothing is re-inferred from the text alone,
 * which is the defect this task repairs: `12,34` + `5` produced the structurally lawful `12,345`, and one more
 * keystroke then healed it into the accepted `123456`.
 */
import assert from "node:assert/strict";
import test from "node:test";

import {
  displayMoneyText,
  interpretMoneyBackspace,
  interpretMoneyDelete,
  interpretMoneyEdit,
  interpretMoneyTyping,
  readMoneyProvenance,
  type MoneyInputProvenance,
} from "./model/money-text.ts";

type Policy = "integer" | "scaled";

/** Model of the field's own state: the model token plus the provenance OF that token. */
function field(initial = "", policy: Policy = "scaled") {
  let value = initial;
  let provenance: MoneyInputProvenance | null = null;
  const current = (): MoneyInputProvenance =>
    provenance !== null && provenance.raw === value ? provenance : readMoneyProvenance(value);
  const commit = (raw: string, untrusted: boolean) => {
    value = raw;
    provenance = { raw, untrusted };
  };
  return {
    /** Visible text exactly as the component renders it. */
    get text(): string {
      return displayMoneyText(value, policy);
    },
    get raw(): string {
      return value;
    },
    /** The component's own `aria-invalid` rule. */
    get invalid(): boolean {
      return value !== "" && (!/^(?:0|[1-9][0-9]*)(?:\.[0-9]{1,2})?$/.test(value) || false);
    },
    /** A real paste/change whose text the operator supplied verbatim. */
    paste(text: string, caret = text.length) {
      const edit = interpretMoneyEdit(text, caret);
      commit(edit.raw, edit.untrusted);
      return edit;
    },
    /** A typing change: the browser reports the whole new display text and the caret. */
    type(text: string, caret = text.length) {
      const edit = interpretMoneyTyping(current(), text, caret);
      commit(edit.raw, edit.untrusted);
      return edit;
    },
    /** One typed character appended at the end of the CURRENT visible text. */
    key(character: string, caret = this.text.length) {
      return this.type(`${this.text}${character}`, caret + 1);
    },
    backspace(caret: number) {
      const edit = interpretMoneyBackspace(this.text, caret);
      if (edit !== null) commit(edit.raw, edit.untrusted);
      return edit;
    },
    deleteAt(caret: number) {
      const edit = interpretMoneyDelete(this.text, caret);
      if (edit !== null) commit(edit.raw, edit.untrusted);
      return edit;
    },
    /** An authoritative external reseed (new row, reopened modal, stored baseline). */
    reseed(next: string) {
      value = next;
      provenance = null;
    },
    /** A re-render/blur with no value change: state must survive untouched. */
    rerender() {
      return this.text;
    },
  };
}

/* ————————————————— NOT ACCEPTED THROUGH ANY NUMBER OF APPENDED KEYS ————————————————— */

test("225R2-1 an invalid pasted token stays invalid through 5, 6, 7 and every re-render", () => {
  const f = field();
  f.paste("12,34");
  assert.equal(f.raw, "12,34");
  assert.equal(f.invalid, true);

  for (const digit of ["5", "6", "7"]) {
    f.key(digit);
    assert.equal(f.invalid, true, `after typing ${digit}: ${f.text}`);
    assert.match(f.raw, /,/, "the operator's separator is still there");
  }
  assert.equal(f.raw, "12,34567");
  assert.equal(f.text, "12,34567");
  // Blur / re-render / another field changing never launders it.
  assert.equal(f.rerender(), "12,34567");
  assert.equal(f.invalid, true);
});

test("225R2-1b the operator's OWN deletion of the separator makes the text lawful", () => {
  const f = field();
  f.paste("12,34");
  f.key("5");
  // The operator removes the comma deliberately: the same logical text without it is a plain amount.
  f.type("12345", 2);
  assert.equal(f.raw, "12345");
  assert.equal(f.invalid, false);
  // …and normal grouping typing works again from that lawful token.
  f.key("6");
  assert.equal(f.raw, "123456");
  assert.equal(f.text, "123,456");
});

test("225R2-1c replacing the whole value is also a deliberate correction", () => {
  const f = field();
  f.paste("12,34");
  f.key("5");
  f.type("125000", 6);
  assert.equal(f.raw, "125000");
  assert.equal(f.invalid, false);
});

test("225R2-1d provenance never leaks to a different row or an external reseed", () => {
  const f = field();
  f.paste("12,34");
  f.key("5");
  assert.equal(f.invalid, true);
  // A different row (or the stored baseline restored by a cancel) reseeds the field from the model.
  f.reseed("120000");
  assert.equal(f.text, "120,000");
  assert.equal(f.invalid, false);
  f.key("0");
  assert.equal(f.raw, "1200000");
  assert.equal(f.invalid, false);
});

/* ————————————————— LAWFUL INPUT IS UNCHANGED ————————————————— */

test("225R2-2 a valid grouped value keeps middle insertion and caret", () => {
  const f = field("1234");
  // Insert `9` after the `2`: browser reports `1,2934` with caret 4.
  const edit = f.type("1,2934", 4);
  assert.equal(f.raw, "12934");
  assert.equal(f.text, "12,934");
  assert.equal(edit.caret.whole, 3);
});

test("225R2-2b sequential five million, zero, and the scaled decimal side", () => {
  const f = field("", "integer");
  for (const digit of "5000000") f.key(digit);
  assert.equal(f.raw, "5000000");
  assert.equal(f.text, "5,000,000");
  assert.equal(f.invalid, false);

  const zero = field("", "integer");
  zero.key("0");
  assert.equal(zero.raw, "0");
  assert.equal(zero.invalid, false);

  const scaled = field("12", "scaled");
  scaled.type("12.", 3);
  const afterDot = scaled.type("12.5", 4);
  assert.equal(scaled.raw, "12.5");
  assert.equal(afterDot.caret.inFraction, true);
  assert.equal(scaled.invalid, false);
});

test("225R2-3 malformed paste, negative and exponent stay invalid", () => {
  for (const text of ["12,34", "-100", "1e3", "abc100", "12.3.4"]) {
    const f = field();
    f.paste(text);
    assert.equal(f.invalid, true, text);
    assert.equal(f.raw, text, text);
  }
  // A stored legacy fraction is still a lawful stored token (refused only by the rate policy, not here).
  const legacy = field("100.50");
  assert.equal(legacy.invalid, false);
});

test("225R2-4 comma-adjacent Backspace and Delete stay symmetric", () => {
  const back = field("12000", "integer");
  back.backspace(3);
  assert.equal(back.raw, "1000");

  const del = field("12000", "integer");
  del.deleteAt(2);
  assert.equal(del.raw, "1200");

  // Deleting the operator's own comma is a correction, not a heal: the separator AND the digit after it go,
  // exactly as a Backspace behind a separator removes the digit it follows.
  const untrusted = field();
  untrusted.paste("12,34");
  const afterDelete = untrusted.deleteAt(2);
  assert.notEqual(afterDelete, null);
  assert.equal(untrusted.raw, "124");
  assert.equal(untrusted.invalid, false);
});
