import assert from "node:assert/strict";
import test from "node:test";
import { mapSupportedLocale } from "./supported-locales.ts";
import {
  codeLockExplanation,
  isIdentityFieldLocked,
  urlPrefixLockExplanation,
} from "./language-identity-lock.ts";

test("referenced language maps locked identity capability flags", () => {
  const row = mapSupportedLocale({
    code: "fa-IR",
    urlPrefix: "fa",
    isReferenced: true,
    canEditCode: false,
    canEditUrlPrefix: false,
  });
  assert.ok(row);
  assert.equal(row!.isReferenced, true);
  assert.equal(row!.canEditCode, false);
  assert.equal(row!.canEditUrlPrefix, false);
  assert.equal(isIdentityFieldLocked(row!.canEditCode), true);
  assert.equal(isIdentityFieldLocked(row!.canEditUrlPrefix), true);
});

test("unreferenced language remains editable for identity fields", () => {
  const row = mapSupportedLocale({
    code: "de-DE",
    urlPrefix: "de",
    isReferenced: false,
    canEditCode: true,
    canEditUrlPrefix: true,
  });
  assert.ok(row);
  assert.equal(isIdentityFieldLocked(row!.canEditCode), false);
  assert.equal(isIdentityFieldLocked(row!.canEditUrlPrefix), false);
});

test("lock explanations include required fa and en copy", () => {
  const code = codeLockExplanation();
  const prefix = urlPrefixLockExplanation();
  assert.match(code.fa, /محتوا/);
  assert.match(code.en, /content/i);
  assert.match(prefix.fa, /پیشوند مسیر/);
  assert.match(prefix.en, /route prefix/i);
});
