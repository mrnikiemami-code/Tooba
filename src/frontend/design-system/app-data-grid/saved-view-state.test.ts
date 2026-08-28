import assert from "node:assert/strict";
import test from "node:test";
import { fromAgFilterModel, toAgFilterModel } from "./ag-filter-mapper.ts";
import { toHostGridQuery } from "./grid-query-mapper.ts";
import { formatJalaliDate, jalaliInputToIso } from "./jalali.ts";
import {
  advancedFilterFieldOrder,
  agFilterModelForSavedView,
  buildAgColumnApplyState,
  captureColumnLayoutFromApi,
  mergeSavedViewFilters,
  migrateSavedView,
  partitionFilters,
  prepareSavedViewForPersistence,
  sanitizeSavedView,
  SAVED_VIEW_SCHEMA_VERSION,
} from "./saved-view-state.ts";

const advancedIds = new Set(["status", "updatedAt", "title"]);
const ctx = {
  knownColumnIds: new Set(["title", "status", "updatedAt", "variantCount"]),
  knownFilterFields: new Set(["title", "status", "updatedAt", "variantCount"]),
  advancedFieldIds: advancedIds,
  enumOptionsByField: {
    status: new Set(["Published", "Draft", "Archived"]),
  },
};

test("prepareSavedViewForPersistence partitions advanced filters independently", () => {
  const view = prepareSavedViewForPersistence(
    {
      id: "v1",
      name: "ops",
      filters: {
        variantCount: { kind: "number", operator: "greaterThan", value: 1 },
        status: { kind: "status", operator: "in", values: ["Published", "Draft"] },
        updatedAt: { kind: "date", operator: "between", iso: "2026-01-01T00:00:00.000Z", isoTo: "2026-01-31T00:00:00.000Z" },
      },
      sorts: [],
      layout: { order: ["title"], visibility: {}, widths: {} },
      pageSize: 20,
      search: "shirt",
    },
    advancedIds,
  );

  assert.equal(view.schemaVersion, SAVED_VIEW_SCHEMA_VERSION);
  assert.equal(view.advancedFilters?.status?.kind, "status");
  assert.equal(view.filters.variantCount?.kind, "number");
  assert.deepEqual(mergeSavedViewFilters(view).status?.kind, "status");
});

test("sanitizeSavedView drops unknown columns filters and stale enum values safely", () => {
  const sanitized = sanitizeSavedView(
    {
      id: "legacy",
      name: "legacy",
      filters: {
        ghostColumn: { kind: "text", operator: "contains", query: "x" },
        variantCount: { kind: "number", operator: "equals", value: 2 },
      },
      advancedFilters: {
        status: { kind: "status", operator: "in", values: ["Published", "Retired"] },
      },
      sorts: [{ columnId: "ghostColumn", direction: "asc" }],
      layout: {
        order: ["ghostColumn", "title", "status"],
        visibility: { ghostColumn: false },
        widths: { ghostColumn: 50, title: 120 },
      },
      pageSize: 50,
    },
    ctx,
  );

  assert.equal(sanitized.filters.ghostColumn, undefined);
  assert.equal(sanitized.advancedFilters?.status?.kind, "status");
  assert.equal(
    sanitized.advancedFilters && sanitized.advancedFilters.status?.kind === "status"
      ? sanitized.advancedFilters.status.values.join(",")
      : "",
    "Published",
  );
  assert.equal(sanitized.sorts.length, 0);
  assert.equal(sanitized.layout.order.includes("ghostColumn"), false);
  assert.equal(sanitized.layout.order.includes("title"), true);
});

test("migrateSavedView upgrades schema version", () => {
  const migrated = migrateSavedView({
    id: "v0",
    name: "old",
    filters: {},
    sorts: [],
    layout: { order: [], visibility: {}, widths: {} },
    pageSize: 10,
  });
  assert.equal(migrated.schemaVersion, SAVED_VIEW_SCHEMA_VERSION);
});

test("advanced filters preserve implicit AND field order", () => {
  const order = advancedFilterFieldOrder(
    {
      updatedAt: { kind: "date", operator: "after", iso: "2026-01-01T00:00:00.000Z" },
      status: { kind: "status", operator: "equals", values: ["Draft"] },
      title: { kind: "text", operator: "contains", query: "a" },
    },
    ["title", "status", "updatedAt"],
  );
  assert.deepEqual(order, ["title", "status", "updatedAt"]);
});

test("status and Jalali filters map to host query with operators", () => {
  const host = toHostGridQuery({
    page: 1,
    pageSize: 20,
    sorts: [],
    filters: {
      status: { kind: "status", operator: "notIn", values: ["Archived"] },
      updatedAt: { kind: "date", operator: "before", iso: "2026-01-15T12:00:00.000Z" },
    },
  });
  assert.equal(host.filters.find((f) => f.field === "status")?.operator, "notIn");
  assert.equal(host.filters.find((f) => f.field === "updatedAt")?.operator, "before");
});

test("Jalali input converts to ISO and displays Persian date", () => {
  const iso = jalaliInputToIso("1404/06/07");
  assert.ok(iso);
  assert.match(formatJalaliDate(iso!, "fa"), /1404/);
});

test("Jalali between boundary round-trip keeps canonical ISO pair", () => {
  const from = jalaliInputToIso("1404/01/01");
  const to = jalaliInputToIso("1404/01/31");
  assert.ok(from && to);
  const filter = { kind: "date" as const, operator: "between" as const, iso: from!, isoTo: to! };
  const host = toHostGridQuery({ page: 1, pageSize: 20, sorts: [], filters: { updatedAt: filter } });
  const mapped = host.filters[0];
  assert.equal(mapped?.operator, "between");
  assert.ok(mapped?.value);
  assert.ok(mapped?.valueTo);
});

test("toAgFilterModel round-trips text/number/date filters through fromAgFilterModel", () => {
  const original = {
    title: { kind: "text" as const, operator: "contains" as const, query: "phone" },
    variantCount: {
      kind: "number" as const,
      operator: "greaterThanOrEqual" as const,
      value: 2,
    },
    updatedAt: {
      kind: "date" as const,
      operator: "on" as const,
      iso: "2026-01-15T00:00:00.000Z",
    },
  };

  const model = toAgFilterModel(original);
  const roundTrip = fromAgFilterModel(model);

  assert.equal(roundTrip.title?.kind, "text");
  assert.equal(roundTrip.variantCount?.kind, "number");
  assert.equal(roundTrip.updatedAt?.kind, "date");
});

test("agFilterModelForSavedView keeps advanced filters out of AG while merge restores server state", () => {
  const merged = {
    status: { kind: "status" as const, operator: "in" as const, values: ["Draft"] },
    variantCount: { kind: "number" as const, operator: "equals" as const, value: 3 },
  };
  const model = agFilterModelForSavedView(merged, new Set(["status"]));
  assert.equal(model?.status, undefined);
  assert.equal(model && "variantCount" in model ? true : false, true);
});

test("saved view column state captures widths and restores sort order", () => {
  const view = prepareSavedViewForPersistence(
    {
      id: "v1",
      name: "ops",
      filters: {},
      sorts: [{ columnId: "variantCount", direction: "desc" }],
      layout: {
        order: ["title", "variantCount", "status"],
        visibility: { title: true, variantCount: true, status: false },
        widths: { title: 140, variantCount: 96 },
      },
      pageSize: 20,
    },
    advancedIds,
  );

  const state = buildAgColumnApplyState(view, ctx.knownColumnIds);
  assert.equal(state[0]?.colId, "title");
  assert.equal(state[1]?.sort, "desc");
  assert.equal(state[2]?.hide, true);
});

test("captureColumnLayoutFromApi falls back when grid api is unavailable", () => {
  const layout = captureColumnLayoutFromApi<{ id: string }>(null, ["a", "b"]);
  assert.deepEqual(layout.order, ["a", "b"]);
});

test("partitionFilters splits advanced and native column filters", () => {
  const parts = partitionFilters(
    {
      title: { kind: "text", operator: "contains", query: "x" },
      variantCount: { kind: "number", operator: "equals", value: 1 },
    },
    advancedIds,
  );
  assert.equal(parts.advancedFilters.title?.kind, "text");
  assert.equal(parts.columnFilters.variantCount?.kind, "number");
});
