import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  PREVIEW_PLACES,
  clonePreviewMethod,
  previewMethodSelectedPlaces,
  previewPlaceAncestors,
  previewPlaceConflict,
  previewPlacePath,
  previewRangeError,
  previewTextFor,
  previewTranslationStates,
  previewTranslationSummary,
} from "./preview-model.ts";
import { PREVIEW_METHOD_FIXTURES } from "./preview-fixtures.ts";
import {
  describePreviewRange,
  formatPreviewMoney,
  previewMessagesEn,
  previewMessagesFa,
  previewMessages,
  previewFormat,
} from "./preview-i18n.ts";

const here = path.dirname(fileURLToPath(import.meta.url));

test("PREVIEW-186: duplicate and parent/child selections are blocked within the same method", () => {
  // ایران اضافه شده ⇒ هیچ زیرمجموعه‌ای دوباره قابل اضافه شدن نیست.
  assert.deepEqual(previewPlaceConflict(["place-ir"], "place-ir-23"), { kind: "covers", placeId: "place-ir" });
  assert.deepEqual(previewPlaceConflict(["place-ir"], "place-ir-23-thr-1"), { kind: "covers", placeId: "place-ir" });

  // استان تهران اضافه شده ⇒ شهر/ناحیهٔ زیر آن ممنوع.
  assert.equal(previewPlaceConflict(["place-ir-23"], "place-ir-23-thr")?.kind, "covers");

  // گلپایگان اضافه شده ⇒ استان اصفهان و ایران ممنوع، شهر مستقل دیگر مجاز.
  assert.equal(previewPlaceConflict(["place-ir-10-glp"], "place-ir-10")?.kind, "covered");
  assert.equal(previewPlaceConflict(["place-ir-10-glp"], "place-ir")?.kind, "covered");
  assert.equal(previewPlaceConflict(["place-ir-10-glp"], "place-ir-25-qom"), null);

  // تکرار دقیق ممنوع است.
  assert.equal(previewPlaceConflict(["place-ir-25-qom"], "place-ir-25-qom")?.kind, "duplicate");
});

test("PREVIEW-186: explicit ancestry is the only conflict source", () => {
  // گلپایگان فرزند اصفهان است، نه تهران — هیچ استنتاجی از برچسب انجام نمی‌شود.
  assert.deepEqual(previewPlaceAncestors("place-ir-10-glp"), ["place-ir-10", "place-ir"]);
  assert.equal(previewPlaceConflict(["place-ir-23"], "place-ir-10-glp"), null);
  assert.ok(PREVIEW_PLACES.every((place) => place.parentId === null || PREVIEW_PLACES.some((p) => p.id === place.parentId)));
});

test("PREVIEW-186: readable geographic path instead of technical codes", () => {
  assert.deepEqual(previewPlacePath("place-ir-23-thr-1", "fa"), ["ایران", "تهران", "تهران", "ناحیه ۱ تهران"]);
  assert.deepEqual(previewPlacePath("place-ir-10-glp", "en"), ["Iran", "Isfahan", "Golpayegan"]);
});

test("PREVIEW-186: locale fixtures are independent and never synthesized", () => {
  const method = PREVIEW_METHOD_FIXTURES.find((row) => row.id === "method-express");
  assert.ok(method);
  const edited = { ...method, names: { ...method.names, en: "Edited English only" } };
  assert.equal(previewTextFor(edited.names, "fa"), method.names.fa);
  assert.equal(previewTextFor(edited.names, "en"), "Edited English only");

  const states = previewTranslationStates({ fa: "نام", en: "   " });
  assert.equal(states.find((state) => state.languageId === "fa")?.translated, true);
  assert.equal(states.find((state) => state.languageId === "en")?.translated, false);
  assert.deepEqual(previewTranslationSummary({ fa: "نام", en: "" }), { translated: 1, missing: 1, total: 2 });
});

test("PREVIEW-186: clone isolates the in-memory draft from the fixture", () => {
  const source = PREVIEW_METHOD_FIXTURES[0]!;
  const copy = clonePreviewMethod(source);
  (copy.names as Record<string, string>).fa = "changed";
  copy.regionGroups[0]!.rows[0]!.freight = 1;
  assert.notEqual(source.names.fa, "changed");
  assert.notEqual(source.regionGroups[0]!.rows[0]!.freight, 1);
});

test("PREVIEW-186: range shape errors are explicit and bounds stay inclusive/exclusive", () => {
  assert.equal(previewRangeError({ from: 2, to: 1 }), "inverted");
  assert.equal(previewRangeError({ from: 2, to: 2 }), "inverted");
  assert.equal(previewRangeError({ from: -1, to: null }), "negative");
  assert.equal(previewRangeError({ from: 1, to: 2 }), null);
  assert.equal(previewRangeError({ from: null, to: null }), null);
  assert.equal(previewRangeError({ from: 1.5, to: 2 }, { integer: true }), "notInteger");
});

test("PREVIEW-186: locale-aware money, weight and range wording", () => {
  assert.equal(formatPreviewMoney(850000, "fa"), `${new Intl.NumberFormat("fa-IR").format(850000)} ریال`);
  assert.equal(formatPreviewMoney(850000, "en"), "IRR 850,000");
  assert.equal(describePreviewRange({ from: 1000, to: 2000 }, "en", "weight"), "1 kg to under 2 kg");
  assert.equal(describePreviewRange(null, "en", "weight"), previewMessagesEn.noCondition);
  assert.equal(describePreviewRange(null, "fa", "days"), previewMessagesFa.dash);
  assert.equal(previewFormat(previewMessagesFa.translationSummary, { translated: 1, total: 2 }).includes("2"), true);
});

test("PREVIEW-186: every fixture group uses existing places and non-overlapping row examples", () => {
  for (const method of PREVIEW_METHOD_FIXTURES) {
    for (const group of method.regionGroups) {
      assert.ok(group.places.length > 0, `${method.id}/${group.id} must show at least one region`);
      for (const placeId of group.places) {
        assert.ok(PREVIEW_PLACES.some((place) => place.id === placeId), `${placeId} must exist in the fixture`);
      }
      // ردیف‌های نمایشی نباید ترکیب کامل شرط یکسانی داشته باشند.
      const keys = group.rows.map((row) => JSON.stringify([row.weightGrams, row.itemCount, row.merchandiseAmount]));
      assert.equal(new Set(keys).size, keys.length, `${method.id}/${group.id} has identical row conditions`);
      assert.equal(group.rows.length >= 1, true);
    }
    assert.equal(previewMethodSelectedPlaces(method).length, method.regionGroups.reduce((total, group) => total + group.places.length, 0));
  }
});

test("PREVIEW-186: both dictionaries are complete and no Persian fallback leaks into English", () => {
  assert.deepEqual(Object.keys(previewMessagesEn).sort(), Object.keys(previewMessagesFa).sort());
  for (const [key, value] of Object.entries(previewMessagesEn)) {
    assert.ok(value.trim().length > 0, `${key} must not be empty in en`);
    assert.equal(/[\u0600-\u06FF]/.test(value), false, `${key} must not contain Persian text in en`);
  }
  assert.equal(previewMessages("fa").tabGeneral, previewMessagesFa.tabGeneral);
});

test("PREVIEW-186: preview code performs no network, storage or business writes", () => {
  const files = fs.readdirSync(here).filter((name) => /\.(ts|tsx)$/.test(name) && !name.endsWith(".test.ts"));
  const forbidden = [
    /\bfetch\s*\(/,
    /XMLHttpRequest/,
    /localStorage/,
    /sessionStorage/,
    /document\.cookie/,
    /method:\s*["'](POST|PUT|PATCH|DELETE)["']/i,
    /hostBaseUrl|buildUpstreamAuthHeaders/,
  ];
  const stripComments = (source: string): string =>
    source.replace(/\/\*[\s\S]*?\*\//g, "").replace(/(^|[^:])\/\/.*$/gm, "$1");
  const violations: string[] = [];
  for (const name of files) {
    const source = stripComments(fs.readFileSync(path.join(here, name), "utf8"));
    for (const pattern of forbidden) {
      if (pattern.test(source)) violations.push(`${name} -> ${pattern}`);
    }
  }
  assert.deepEqual(violations, []);
});
