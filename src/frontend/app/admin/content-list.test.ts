import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const source = readFileSync(join(import.meta.dirname, "content-list.tsx"), "utf8");

test("content list pins actions like product list", () => {
  assert.match(source, /buildPinnedActionsColumnDef/);
  assert.match(source, /direction:\s*"rtl"/);
  assert.match(source, /AppGridRowActionsCell/);
  assert.match(source, /Pencil/);
  assert.match(source, /formatArticleLocaleLabel/);
  assert.doesNotMatch(source, /sticky:\s*"start"/);
  assert.doesNotMatch(source, /AgGridReact/);
});

test("content list uses language tabs and server locale filter", () => {
  assert.match(source, /loadAdminLanguages/);
  assert.match(source, /useSearchParams/);
  assert.match(source, /admin-content-language-tabs/);
  assert.match(source, /kind:\s*"text"/);
  assert.match(source, /operator:\s*"equals"/);
  assert.match(source, /language=\$\{/);
  assert.doesNotMatch(source, /field:\s*"locale"/);
  assert.doesNotMatch(source, /id:\s*"locale"/);
});
