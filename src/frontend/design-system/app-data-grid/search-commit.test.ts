import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { commitSearchQuery } from "./search-commit.ts";
import { DEFAULT_GRID_QUERY } from "./grid-query-mapper.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("commitSearchQuery returns null when search unchanged", () => {
  const current = { ...DEFAULT_GRID_QUERY, search: "foo" };
  assert.equal(commitSearchQuery(current, "foo"), null);
  assert.equal(commitSearchQuery(current, "  foo  "), null);
});

test("commitSearchQuery returns next query when search changes", () => {
  const next = commitSearchQuery(DEFAULT_GRID_QUERY, "abc");
  assert.ok(next);
  assert.equal(next.search, "abc");
  assert.equal(next.page, 1);
});

test("commitSearchQuery clears search on empty draft", () => {
  const current = { ...DEFAULT_GRID_QUERY, search: "x" };
  const next = commitSearchQuery(current, "   ");
  assert.ok(next);
  assert.equal(next.search, undefined);
});

test("AppDataGrid search uses draft commit not debounced load", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /commitSearchQuery/);
  assert.match(source, /onSearchKeyDown/);
  assert.match(source, /event\.key === "Enter"/);
  assert.doesNotMatch(source, /searchTimerRef/);
  assert.doesNotMatch(source, /setTimeout.*search/s);
});
