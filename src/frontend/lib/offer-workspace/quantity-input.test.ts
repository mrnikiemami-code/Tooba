import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import test from "node:test";
import { createElement, useState } from "react";
import { mount, q, setInputValue } from "../../features/order-carrier-execution/test-dom-harness.ts";
import { QuantityInput, quantityInputValue } from "./quantity-input";
import { createOfferFormatters, OFFER_LOCALE_FA, OFFER_LOCALE_EN } from "./i18n";
import { parseDecimal } from "./model";

test("quantity editor accepts empty and localized decimal drafts but rejects text and exponent", () => {
  assert.equal(quantityInputValue("", 2, "fa-IR"), "");
  assert.equal(quantityInputValue("۱۲٫۵", 2, "fa-IR"), "12.5");
  assert.equal(quantityInputValue("1,5", 2, "ru-RU"), "1.5");
  assert.equal(quantityInputValue("۱۲", 0, "fa-IR"), "12");
  for (const value of ["text", "سیب", "5abc", "1e3", "-1", "1.2.3"]) assert.equal(quantityInputValue(value, 2, "en-US"), null);
  assert.equal(quantityInputValue("1.5", 0, "en-US"), null);
  assert.equal(parseDecimal("1e3"), null);
  assert.equal(parseDecimal("0x10"), null);
  assert.equal(parseDecimal(""), null);
});
for (const [locale, scale] of [[OFFER_LOCALE_FA, 2], [OFFER_LOCALE_EN, 0]] as const) {
  test("mounted quantity input rejects text and permits clearing: " + locale.code, async () => {
    const format = createOfferFormatters(locale);
    function Fixture() {
      const [value, setValue] = useState("");
      return createElement(QuantityInput, {value, onChange:setValue, scale, format, testId:"quantity"});
    }
    const tree = await mount(createElement(Fixture));
    try {
      const node = q(tree.container, "[data-testid=quantity]") as HTMLInputElement;
      const raw = scale ? "12.5" : "7";
      await setInputValue(node, raw);
      const expected = scale ? "۱۲٫۵" : "7";
      assert.equal(node.value, expected);
      await setInputValue(node, "text");
      assert.equal(node.value, expected);
      await setInputValue(node, "");
      assert.equal(node.value, "");
    } finally { await tree.unmount(); }
  });
}
