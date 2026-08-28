import assert from "node:assert/strict";
import test from "node:test";
import { assertCommunityColumnFilter, COMMUNITY_AG_FILTERS, FORBIDDEN_AG_FILTERS } from "./filter-column-def.ts";

test("Community AG filters whitelist excludes Enterprise set/multi filters", () => {
  assert.ok(COMMUNITY_AG_FILTERS.has("agTextColumnFilter"));
  assert.ok(COMMUNITY_AG_FILTERS.has("agNumberColumnFilter"));
  assert.ok(COMMUNITY_AG_FILTERS.has("agDateColumnFilter"));
  assert.ok(FORBIDDEN_AG_FILTERS.has("agSetColumnFilter"));
  assert.throws(() => assertCommunityColumnFilter("agSetColumnFilter"));
});
