import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import {
  COLUMN_FILTER_APPLY_PARAMS,
  filtersEqual,
  gridQueryCommitKey,
  shouldCommitGridQuery,
} from "./filter-commit.ts";
import type { GridServerQuery } from "../data-grid/types.ts";

const baseQuery: GridServerQuery = {
  page: 1,
  pageSize: 20,
  sorts: [],
  filters: {},
};

test("COLUMN_FILTER_APPLY_PARAMS requires explicit apply", () => {
  assert.deepEqual(COLUMN_FILTER_APPLY_PARAMS.buttons, ["apply", "reset"]);
  assert.equal(COLUMN_FILTER_APPLY_PARAMS.closeOnApply, true);
  assert.equal(COLUMN_FILTER_APPLY_PARAMS.debounceMs, 0);
});

test("filtersEqual suppresses duplicate commits for same filter", () => {
  const left = { title: { kind: "text" as const, operator: "contains" as const, query: "phone" } };
  const right = { title: { kind: "text" as const, operator: "contains" as const, query: "phone" } };
  assert.equal(filtersEqual(left, right), true);
});

test("shouldCommitGridQuery returns false when only draft would match applied", () => {
  const current: GridServerQuery = {
    ...baseQuery,
    filters: { title: { kind: "text", operator: "contains", query: "samsung" } },
  };
  const same: GridServerQuery = { ...current };
  assert.equal(shouldCommitGridQuery(current, same), false);
});

test("shouldCommitGridQuery returns true when filter value changes", () => {
  const current: GridServerQuery = {
    ...baseQuery,
    filters: { title: { kind: "text", operator: "contains", query: "a" } },
  };
  const next: GridServerQuery = {
    ...baseQuery,
    filters: { title: { kind: "text", operator: "contains", query: "ab" } },
  };
  assert.equal(shouldCommitGridQuery(current, next), true);
});

test("gridQueryCommitKey includes page for pagination dedup", () => {
  const page1 = gridQueryCommitKey({ ...baseQuery, page: 1 });
  const page2 = gridQueryCommitKey({ ...baseQuery, page: 2 });
  assert.notEqual(page1, page2);
});

const dir = dirname(fileURLToPath(import.meta.url));

test("theme uses opaque header surface and popover layering tokens", () => {
  const css = readFileSync(join(dir, "theme.css"), "utf8");
  assert.match(css, /--ag-header-background-color:\s*hsl\(var\(--surface-elevated\)\)/);
  assert.match(css, /--z-popover/);
  assert.match(css, /--app-grid-body-height/);
  assert.doesNotMatch(css, /isolation:\s*isolate/);
});

test("AppDataGrid portals filter popups and registers Jalali column filter", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /popupParent/);
  assert.match(source, /jalaliDateColumnFilter:\s*JalaliDateColumnFilter/);
  assert.match(source, /AdvancedFilterDrawer/);
  assert.match(source, /filterParams:\s*COLUMN_FILTER_APPLY_PARAMS/);
  assert.match(source, /commitColumnFilters/);
  assert.doesNotMatch(source, /exportScopeNote/);
  assert.doesNotMatch(source, /pageSelectionNote/);
});

test("product list exposes Jalali column filter on updatedAt", () => {
  const source = readFileSync(join(dir, "..", "..", "app", "admin", "product-list.tsx"), "utf8");
  assert.match(source, /field:\s*"updatedAt"/);
  assert.match(source, /filter:\s*"jalaliDateColumnFilter"/);
});
