import assert from "node:assert/strict";
import test from "node:test";
import { fromAgFilterModel, toAgFilterModel } from "./ag-filter-mapper.ts";
import { toHostGridQuery } from "./grid-query-mapper.ts";
import {
  migrateSavedView,
  prepareSavedViewForPersistence,
  sanitizeSavedView,
  SAVED_VIEW_SCHEMA_VERSION,
} from "./saved-view-state.ts";
import { normalizeAdvancedFilterExpression } from "./advanced-filter-expression.ts";

const advancedIds = new Set(["status", "updatedAt", "title"]);
const ctx = {
  knownColumnIds: new Set(["title", "status", "updatedAt", "variantCount"]),
  knownFilterFields: new Set(["title", "status", "updatedAt", "variantCount"]),
  advancedFieldIds: advancedIds,
  enumOptionsByField: {
    status: new Set(["Published", "Draft", "Archived"]),
  },
  advancedFieldOrder: ["status", "title", "updatedAt"],
};

test("prepareSavedViewForPersistence stores advancedFilterExpression with connectors", () => {
  const view = prepareSavedViewForPersistence(
    {
      id: "v1",
      name: "ops",
      filters: { variantCount: { kind: "number", operator: "greaterThan", value: 1 } },
      advancedFilterExpression: normalizeAdvancedFilterExpression({
        conditions: [
          { id: "c1", field: "status", value: { kind: "status", operator: "in", values: ["Published"] } },
          { id: "c2", field: "title", value: { kind: "text", operator: "contains", query: "phone" } },
        ],
        connectors: ["or"],
      }),
      sorts: [],
      layout: { order: ["title"], visibility: {}, widths: {} },
      pageSize: 20,
    },
    advancedIds,
  );

  assert.equal(view.schemaVersion, SAVED_VIEW_SCHEMA_VERSION);
  assert.deepEqual(view.advancedFilterExpression?.connectors, ["or"]);
});

test("migrateSavedView upgrades legacy advancedFilters to AND expression", () => {
  const migrated = migrateSavedView(
    {
      id: "legacy",
      name: "legacy",
      filters: {},
      advancedFilters: {
        status: { kind: "status", operator: "in", values: ["Draft"] },
        title: { kind: "text", operator: "contains", query: "x" },
      },
      sorts: [],
      layout: { order: [], visibility: {}, widths: {} },
      pageSize: 10,
    },
    ctx.advancedFieldOrder,
  );
  assert.equal(migrated.schemaVersion, 3);
  assert.deepEqual(migrated.advancedFilterExpression?.connectors, ["and"]);
});

test("toHostGridQuery maps advancedFilter without AG leakage", () => {
  const host = toHostGridQuery({
    page: 1,
    pageSize: 20,
    sorts: [],
    filters: {},
    advancedFilter: normalizeAdvancedFilterExpression({
      conditions: [
        { id: "1", field: "status", value: { kind: "status", operator: "equals", values: ["Published"] } },
        { id: "2", field: "title", value: { kind: "text", operator: "contains", query: "phone" } },
      ],
      connectors: ["and"],
    }),
  });
  assert.equal(host.advancedFilter?.conditions.length, 2);
  assert.deepEqual(host.advancedFilter?.connectors, ["and"]);
});

test("sanitizeSavedView drops unknown fields from advanced expression", () => {
  const sanitized = sanitizeSavedView(
    {
      id: "v",
      name: "v",
      filters: {},
      advancedFilterExpression: {
        conditions: [
          { id: "1", field: "ghost", value: { kind: "text", operator: "contains", query: "x" } },
          { id: "2", field: "status", value: { kind: "status", operator: "in", values: ["Published", "Retired"] } },
        ],
        connectors: ["and"],
      },
      sorts: [],
      layout: { order: ["ghost"], visibility: {}, widths: {} },
      pageSize: 20,
    },
    ctx,
  );
  assert.equal(sanitized.advancedFilterExpression?.conditions.length, 1);
  assert.equal(sanitized.advancedFilterExpression?.conditions[0]?.field, "status");
});

test("toAgFilterModel round-trips text filters", () => {
  const original = {
    title: { kind: "text" as const, operator: "contains" as const, query: "phone" },
  };
  const roundTrip = fromAgFilterModel(toAgFilterModel(original));
  assert.equal(roundTrip.title?.kind, "text");
});
