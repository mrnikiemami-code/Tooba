import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const api = readFileSync(join(import.meta.dirname, "content-author-api.ts"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-author-admin-screen.tsx"), "utf8");
const list = readFileSync(join(import.meta.dirname, "content-author-list.tsx"), "utf8");

test("content author api targets content-owned endpoints", () => {
  assert.match(api, /\/v1\/admin\/content\/authors\/query/);
  assert.match(api, /\/v1\/admin\/content\/authors\/picker/);
  assert.match(api, /activeOnly=true/);
  assert.match(api, /fetchActiveContentAuthors/);
  assert.doesNotMatch(api, /\/v1\/admin\/content\/authors\/active/);
  assert.doesNotMatch(api, /\/v1\/admin\/catalog\//);
});

test("content author admin uses form mode, tabs, and media library", () => {
  assert.match(screen, /useAdminFormMode/);
  assert.match(screen, /MediaLibraryDialog/);
  assert.match(screen, /\/admin\/content\/authors/);
  assert.match(screen, /درباره نویسنده/);
  assert.match(screen, /شبکه‌های اجتماعی/);
  assert.match(screen, /content-author-tab-/);
});

test("content author list uses AppDataGrid without AgGridReact", () => {
  assert.match(list, /queryAdminContentAuthorsGrid/);
  assert.match(list, /AppDataGrid/);
  assert.match(list, /buildPinnedActionsColumnDef/);
  assert.doesNotMatch(list, /AgGridReact/);
});

test("content author edit href and screen honor mode=edit", () => {
  assert.match(list, /\?mode=edit/);
  assert.match(screen, /useSearchParams/);
  assert.match(screen, /requestedMode === "edit"/);
  assert.match(screen, /form\.onEdit\(\)/);
});
