import assert from "node:assert/strict";
import test from "node:test";
import { DEFAULT_GRID_QUERY, toHostGridQuery } from "./grid-query-mapper.ts";

test("toHostGridQuery maps UI contract without AG Grid leakage", () => {
  const host = toHostGridQuery({
    ...DEFAULT_GRID_QUERY,
    search: "shirt",
    filters: {
      status: { kind: "status", values: ["Published"] },
    },
  });
  assert.equal(host.page, 1);
  assert.equal(host.search, "shirt");
  assert.equal(host.filters[0]?.field, "status");
  assert.equal(host.filters[0]?.operator, "in");
});
