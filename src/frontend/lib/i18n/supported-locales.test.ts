import assert from "node:assert/strict";
import test from "node:test";
import { contentApiLocaleForUrlPrefix, mapSupportedLocale } from "./supported-locales.ts";

test("supported locales include fa-IR default and en-US", () => {
  const fa = mapSupportedLocale({
    code: "fa-IR",
    urlPrefix: "fa",
    displayName: "Persian (Iran)",
    nativeName: "فارسی (ایران)",
    direction: "rtl",
    culture: "fa-IR",
    calendarDisplay: "Jalali",
    active: true,
    isDefault: true,
    sortOrder: 0,
  });
  assert.ok(fa?.default);
  assert.equal(fa?.calendarDisplay, "jalali");
  assert.equal(contentApiLocaleForUrlPrefix("fa"), "fa-IR");
  assert.equal(contentApiLocaleForUrlPrefix("en"), "en-US");
});
