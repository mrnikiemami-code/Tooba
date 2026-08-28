import assert from "node:assert/strict";
import test from "node:test";
import { TEXT_OPERATORS, textFilterNeedsValue } from "./text-filter-operators.ts";
import { isFilterActive } from "../data-grid/serialize.ts";
import { toHostGridQuery } from "./grid-query-mapper.ts";
import { fromAgFilterModel, toAgFilterModel } from "./ag-filter-mapper.ts";
import { DEFAULT_GRID_QUERY } from "./grid-query-mapper.ts";

test("TEXT_OPERATORS includes all backend-supported text operators", () => {
  assert.deepEqual(TEXT_OPERATORS, [
    "contains",
    "notContains",
    "equals",
    "notEqual",
    "startsWith",
    "endsWith",
    "blank",
    "notBlank",
  ]);
});

test("textFilterNeedsValue excludes blank operators", () => {
  assert.equal(textFilterNeedsValue("contains"), true);
  assert.equal(textFilterNeedsValue("blank"), false);
  assert.equal(textFilterNeedsValue("notBlank"), false);
});

test("isFilterActive treats text blank/notBlank as active", () => {
  assert.equal(isFilterActive({ kind: "text", operator: "blank", query: "" }), true);
  assert.equal(isFilterActive({ kind: "text", operator: "notBlank", query: "" }), true);
  assert.equal(isFilterActive({ kind: "text", operator: "contains", query: "" }), false);
});

test("toHostGridQuery maps text blank/notBlank without value", () => {
  const host = toHostGridQuery({
    ...DEFAULT_GRID_QUERY,
    filters: {
      title: { kind: "text", operator: "blank", query: "" },
      categorySummary: { kind: "text", operator: "endsWith", query: "کفش" },
    },
  });
  assert.equal(host.filters[0]?.field, "title");
  assert.equal(host.filters[0]?.operator, "blank");
  assert.equal(host.filters[0]?.value, undefined);
  assert.equal(host.filters[1]?.operator, "endsWith");
  assert.equal(host.filters[1]?.value, "کفش");
});

test("ag text filter round-trips all standard operators", () => {
  const filters = {
    title: { kind: "text" as const, operator: "endsWith" as const, query: "abc" },
    categorySummary: { kind: "text" as const, operator: "notBlank" as const, query: "" },
  };
  const model = toAgFilterModel(filters);
  const restored = fromAgFilterModel(model);
  assert.equal(restored.title?.kind, "text");
  assert.equal(restored.title && restored.title.kind === "text" ? restored.title.operator : "", "endsWith");
  assert.equal(restored.categorySummary?.kind, "text");
  assert.equal(
    restored.categorySummary && restored.categorySummary.kind === "text" ? restored.categorySummary.operator : "",
    "notBlank",
  );
});

test("fromAgFilterModel maps text blank filters", () => {
  const filters = fromAgFilterModel({
    title: { filterType: "text", type: "blank" },
    categorySummary: { filterType: "text", type: "notContains", filter: "x" },
  });
  assert.deepEqual(filters.title, { kind: "text", operator: "blank", query: "" });
  assert.equal(filters.categorySummary && filters.categorySummary.kind === "text" ? filters.categorySummary.operator : "", "notContains");
});
