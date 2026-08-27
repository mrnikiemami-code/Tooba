import assert from "node:assert/strict";
import test from "node:test";
import {
  assertLocaleMarketSeparation,
  DEFAULT_LOCALE,
  dirForLocale,
  langForLocale,
  openGraphLocaleFor,
  parseLocale,
} from "./locale.ts";
import { readLocaleFromCookieString } from "./locale-cookie.ts";
import { chromeMessagesFor } from "./messages.ts";

test("dirForLocale: fa is rtl, en is ltr", () => {
  assert.equal(dirForLocale("fa"), "rtl");
  assert.equal(dirForLocale("en"), "ltr");
});

test("langForLocale and openGraph locale tags", () => {
  assert.equal(langForLocale("fa"), "fa");
  assert.equal(langForLocale("en"), "en");
  assert.equal(openGraphLocaleFor("fa"), "fa_IR");
  assert.equal(openGraphLocaleFor("en"), "en_US");
});

test("parseLocale defaults to fa and accepts en", () => {
  assert.equal(parseLocale(undefined), DEFAULT_LOCALE);
  assert.equal(parseLocale("en"), "en");
  assert.equal(parseLocale("EN-US"), "en");
  assert.equal(parseLocale("fa-IR"), "fa");
  assert.equal(parseLocale("xx"), "fa");
});

test("locale does NOT imply currency or market", () => {
  const ok = assertLocaleMarketSeparation({ locale: "fa", currency: "IRR", market: "IR" });
  assert.equal(ok.independent, true);
  assert.equal(ok.locale, "fa");
  assert.notEqual(ok.locale, ok.currency);
  assert.notEqual(ok.locale, ok.market);

  const enOk = assertLocaleMarketSeparation({ locale: "en", currency: "USD", market: "US" });
  assert.equal(enOk.independent, true);

  assert.throws(() => assertLocaleMarketSeparation({ locale: "fa", currency: "fa" }));
  assert.throws(() => assertLocaleMarketSeparation({ locale: "en", market: "en" }));
});

test("cookie reader parses tooba_locale", () => {
  assert.equal(readLocaleFromCookieString("tooba_locale=en; other=1"), "en");
  assert.equal(readLocaleFromCookieString("a=1; tooba_locale=fa"), "fa");
  assert.equal(readLocaleFromCookieString(""), "fa");
});

test("chrome messages share retry key style", () => {
  assert.equal(chromeMessagesFor("fa").retry, "تلاش دوباره");
  assert.equal(chromeMessagesFor("en").retry, "Retry");
});
