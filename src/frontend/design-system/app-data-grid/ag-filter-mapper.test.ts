import assert from "node:assert/strict";
import test from "node:test";
import { fromAgFilterModel } from "./ag-filter-mapper.ts";

test("fromAgFilterModel maps text/number/date/set filters without AG leakage to host", () => {
  const filters = fromAgFilterModel({
    title: { filterType: "text", type: "contains", filter: "phone" },
    variantCount: { filterType: "number", type: "greaterThanOrEqual", filter: 2 },
    updatedAt: { filterType: "date", type: "equals", dateFrom: "2026-01-15T00:00:00.000Z" },
    status: { filterType: "set", values: ["Published", "Draft"] },
  });

  assert.equal(filters.title?.kind, "text");
  assert.equal(filters.title && filters.title.kind === "text" ? filters.title.query : "", "phone");
  assert.equal(filters.variantCount?.kind, "number");
  assert.equal(
    filters.variantCount && filters.variantCount.kind === "number" ? filters.variantCount.operator : "",
    "greaterThanOrEqual",
  );
  assert.equal(filters.updatedAt?.kind, "date");
  assert.equal(filters.status?.kind, "status");
  assert.deepEqual(filters.status && filters.status.kind === "status" ? filters.status.values : [], ["Published", "Draft"]);
});

test("fromAgFilterModel ignores empty filters", () => {
  const filters = fromAgFilterModel({
    title: { filterType: "text", type: "contains", filter: "   " },
    offerCount: { filterType: "set", values: [] },
  });
  assert.deepEqual(filters, {});
});
