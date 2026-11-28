import assert from "node:assert/strict";
import test from "node:test";
import {
  OFFER_LOCALE_EN,
  OFFER_LOCALE_FA,
  channelLabel,
  createOfferFormatters,
  offerListMessagesFor,
  offerLocaleFromRegistry,
  offerStatusLabel,
} from "./i18n.ts";

test("Persian formatters use Persian digits, grouping and Jalali date", () => {
  const format = createOfferFormatters(OFFER_LOCALE_FA);
  assert.equal(format.integer(1234567), "۱٬۲۳۴٬۵۶۷");
  assert.match(format.date("2026-01-20T10:00:00Z"), /۱۴۰۴/);
  assert.equal(format.amountInput("1234567"), "۱٬۲۳۴٬۵۶۷");
  assert.equal(format.amountInput("۱۲۳۴"), "۱٬۲۳۴");
  assert.equal(format.currencyName("IRR").length > 0, true);
});

test("English formatters use Latin digits and Gregorian date", () => {
  const format = createOfferFormatters(OFFER_LOCALE_EN);
  assert.equal(format.integer(1234567), "1,234,567");
  assert.match(format.date("2026-01-20T10:00:00Z"), /2026/);
  assert.equal(format.amountInput("1234567"), "1,234,567");
});

test("an isolated third-language registry fixture changes direction, numbers and fallback copy", () => {
  const turkish = offerLocaleFromRegistry({
    code: "tr-TR",
    culture: "tr-TR",
    direction: "ltr",
    calendarDisplay: "gregorian",
  });
  assert.equal(turkish.direction, "ltr");
  const format = createOfferFormatters(turkish);
  assert.equal(format.integer(1234), "1.234");
  assert.match(format.date("2026-01-20T10:00:00Z"), /2026/);
  const messages = offerListMessagesFor("tr", { title: "Satıcı teklifleri", view: "Görüntüle" });
  assert.equal(messages.title, "Satıcı teklifleri");
  assert.equal(messages.view, "Görüntüle");
  assert.equal(messages.product, "Product");
  assert.equal(channelLabel(messages, "Marketplace"), "Marketplace");
});

test("real channels and statuses are translated from contract values", () => {
  const fa = offerListMessagesFor("fa");
  const en = offerListMessagesFor("en");
  assert.equal(channelLabel(fa, "Marketplace"), "بازارگاه");
  assert.equal(channelLabel(en, "Marketplace"), "Marketplace");
  assert.equal(channelLabel(en, "Direct"), "Direct");
  assert.equal(offerStatusLabel(fa, "Suspended"), "معلق");
  assert.equal(offerStatusLabel(en, "Draft"), "Draft");
});

import { offerErrorText, offerWorkspaceMessages } from "./messages.ts";
import { offerWorkspaceMessagesRu } from "./messages-ru.ts";

test("offer create authorization and precision errors have localized copy in registry resources", () => {
  for (const t of [offerWorkspaceMessages.fa, offerWorkspaceMessages.en, offerWorkspaceMessagesRu]) {
    assert.ok(t.offerCreatePermissionRequired.length > 0);
    assert.equal(offerErrorText(t, "seller.authorization.denied"), t.denied);
    assert.equal(offerErrorText(t, "offer.price.decimals_not_allowed"), t.validationDecimals);
    assert.equal(offerErrorText(t, "offer.price.required"), t.priceRequired);
  }
  assert.notEqual(offerWorkspaceMessages.fa.offerCreatePermissionRequired, offerWorkspaceMessages.en.offerCreatePermissionRequired);
  assert.notEqual(offerWorkspaceMessagesRu.offerCreatePermissionRequired, offerWorkspaceMessages.en.offerCreatePermissionRequired);
});
