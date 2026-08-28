import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import {
  hasActiveTransientFilters,
  isSelectedViewDirty,
  resolveViewApplyQuery,
} from "./saved-view-dirty.ts";
import { DEFAULT_GRID_QUERY } from "./grid-query-mapper.ts";
import type { SavedGridView } from "../data-grid/types.ts";

const dir = dirname(fileURLToPath(import.meta.url));

const baseView: SavedGridView = {
  id: "v1",
  name: "Mobile",
  filters: {},
  sorts: DEFAULT_GRID_QUERY.sorts,
  layout: { order: ["title"], visibility: { title: true }, widths: {} },
  pageSize: DEFAULT_GRID_QUERY.pageSize,
  advancedFilterExpression: DEFAULT_GRID_QUERY.advancedFilter,
};

test("hasActiveTransientFilters detects search and column filters", () => {
  assert.equal(hasActiveTransientFilters(DEFAULT_GRID_QUERY), false);
  assert.equal(hasActiveTransientFilters({ ...DEFAULT_GRID_QUERY, search: "x" }), true);
  assert.equal(
    hasActiveTransientFilters({
      ...DEFAULT_GRID_QUERY,
      filters: { title: { kind: "text", operator: "contains", query: "a" } },
    }),
    true,
  );
});

test("isSelectedViewDirty when filters diverge from saved view", () => {
  assert.equal(isSelectedViewDirty(baseView, DEFAULT_GRID_QUERY, baseView.layout), false);
  const dirty = isSelectedViewDirty(
    baseView,
    {
      ...DEFAULT_GRID_QUERY,
      filters: { title: { kind: "text", operator: "contains", query: "z" } },
    },
    baseView.layout,
  );
  assert.equal(dirty, true);
});

test("resolveViewApplyQuery preserves active filters when switching views", () => {
  const current = {
    ...DEFAULT_GRID_QUERY,
    search: "phone",
    filters: { title: { kind: "text", operator: "contains", value: "a" } },
  };
  const other: SavedGridView = { ...baseView, id: "v2", name: "Desktop", pageSize: 50 };
  const resolved = resolveViewApplyQuery(other, current, DEFAULT_GRID_QUERY.sorts, "phone");
  assert.equal(resolved.restoreSavedFilters, false);
  assert.equal(resolved.query.search, "phone");
  assert.equal(resolved.query.pageSize, 50);
  assert.equal(resolved.searchDraft, "phone");
});

test("resolveViewApplyQuery restores saved filters when none active", () => {
  const saved: SavedGridView = {
    ...baseView,
    search: "saved",
    filters: { status: { kind: "enum", operator: "equals", value: "Published" } },
  };
  const resolved = resolveViewApplyQuery(saved, DEFAULT_GRID_QUERY, DEFAULT_GRID_QUERY.sorts, "");
  assert.equal(resolved.restoreSavedFilters, true);
  assert.equal(resolved.query.search, "saved");
  assert.equal(resolved.searchDraft, "saved");
});

test("AppDataGrid does not deselect saved view on filter/search commit", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.doesNotMatch(source, /onExternalFilterApply[\s\S]{0,500}setActiveViewId\(null\)/);
  assert.doesNotMatch(source, /const commitColumnFilters = useCallback\([\s\S]{0,400}setActiveViewId\(null\)/);
  assert.doesNotMatch(source, /function commitSearch[\s\S]{0,300}setActiveViewId\(null\)/);
  assert.doesNotMatch(source, /function applyDraftAdvancedFilter[\s\S]{0,300}setActiveViewId\(null\)/);
});
