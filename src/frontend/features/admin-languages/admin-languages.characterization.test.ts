import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const featureRoot = path.join(feRoot, "features/admin-languages");

test("admin-languages public boundary exports screen and API", () => {
  const index = fs.readFileSync(path.join(featureRoot, "index.ts"), "utf8");
  assert.match(index, /AdminLanguagesScreen/);
  assert.match(index, /loadAdminLanguages/);
  assert.match(index, /updateAdminLanguage/);
  assert.match(index, /patchAdminLanguage/);
});

test("admin languages route stays thin composition", () => {
  const page = fs.readFileSync(path.join(feRoot, "app/admin/languages/page.tsx"), "utf8");
  assert.match(page, /features\/admin-languages/);
  assert.match(page, /AdminLanguagesScreen/);
  assert.ok(!page.includes("use client"));
  assert.ok(page.split("\n").length < 40);
});

test("language API uses shared admin-result and languages Host routes", () => {
  const api = fs.readFileSync(path.join(featureRoot, "api/language-api.ts"), "utf8");
  assert.match(api, /lib\/admin\/admin-result/);
  assert.match(api, /\/v1\/admin\/languages/);
  assert.doesNotMatch(api, /from ["'].*admin-api/);
  assert.match(api, /export async function loadAdminLanguages/);
  assert.match(api, /export async function updateAdminLanguage/);
  assert.match(api, /export async function patchAdminLanguage/);
});

test("language list keeps edit modal and grid markers", () => {
  const list = fs.readFileSync(path.join(featureRoot, "components/language-list.tsx"), "utf8");
  assert.match(list, /data-testid="admin-languages"/);
  assert.match(list, /data-testid="language-edit-modal"/);
  assert.match(list, /loadAdminLanguages/);
  assert.match(list, /updateAdminLanguage/);
  assert.match(list, /["']use client["']/);
});

test("flat admin no longer owns language implementation files", () => {
  assert.ok(!fs.existsSync(path.join(feRoot, "app/admin/language-api.ts")));
  assert.ok(!fs.existsSync(path.join(feRoot, "app/admin/language-list.tsx")));
});
