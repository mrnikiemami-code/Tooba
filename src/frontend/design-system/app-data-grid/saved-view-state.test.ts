import assert from "node:assert/strict";
import test from "node:test";
import { fromAgFilterModel, toAgFilterModel } from "./ag-filter-mapper.ts";
import {
  agFilterModelForSavedView,
  buildAgColumnApplyState,
  captureColumnLayoutFromApi,
  normalizeSavedViewForPersistence,
} from "./saved-view-state.ts";

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
  assert.equal(roundTrip.title && roundTrip.title.kind === "text" ? roundTrip.title.query : "", "phone");
  assert.equal(roundTrip.variantCount?.kind, "number");
  assert.equal(
    roundTrip.variantCount && roundTrip.variantCount.kind === "number" ? roundTrip.variantCount.operator : "",
    "greaterThanOrEqual",
  );
  assert.equal(roundTrip.updatedAt?.kind, "date");
});

test("toAgFilterModel excludes advanced enum/status fields from AG model", () => {
  const model = toAgFilterModel(
    {
      status: { kind: "status", values: ["Published"] },
      title: { kind: "text", operator: "equals", query: "shirt" },
    },
    { excludeFields: new Set(["status"]) },
  );

  assert.equal(model.status, undefined);
  assert.equal(model.title && "filter" in model.title ? model.title.filter : "", "shirt");
});

test("saved view column state captures widths and restores sort order", () => {
  const view = normalizeSavedViewForPersistence({
    id: "v1",
    name: "ops",
    filters: {},
    sorts: [{ columnId: "amount", direction: "desc" }],
    layout: {
      order: ["reference", "amount", "status"],
      visibility: { reference: true, amount: true, status: false },
      widths: { reference: 140, amount: 96 },
    },
    pageSize: 20,
  });

  const state = buildAgColumnApplyState(view);
  assert.equal(state[0]?.colId, "reference");
  assert.equal(state[0]?.width, 140);
  assert.equal(state[1]?.sort, "desc");
  assert.equal(state[2]?.hide, true);

  const cloned = normalizeSavedViewForPersistence(view);
  assert.deepEqual(cloned.layout.widths, { reference: 140, amount: 96 });
});

test("agFilterModelForSavedView returns null when only advanced filters exist", () => {
  const model = agFilterModelForSavedView(
    { status: { kind: "status", values: ["Draft"] } },
    new Set(["status"]),
  );
  assert.equal(model, null);
});

test("captureColumnLayoutFromApi falls back when grid api is unavailable", () => {
  const layout = captureColumnLayoutFromApi<{ id: string }>(null, ["a", "b"]);
  assert.deepEqual(layout.order, ["a", "b"]);
  assert.deepEqual(layout.widths, {});
});
