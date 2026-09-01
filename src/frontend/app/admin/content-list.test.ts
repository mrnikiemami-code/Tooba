import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const source = readFileSync(join(import.meta.dirname, "content-list.tsx"), "utf8");

test("content list pins actions like product list", () => {
  assert.match(source, /buildPinnedActionsColumnDef/);
  assert.match(source, /direction:\s*"rtl"/);
  assert.match(source, /AppGridRowActionsCell/);
  assert.doesNotMatch(source, /sticky:\s*"start"/);
  assert.doesNotMatch(source, /AgGridReact/);
});
