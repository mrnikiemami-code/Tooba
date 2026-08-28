import assert from "node:assert/strict";
import test from "node:test";
import {
  evaluateAdvancedFilterLeftToRight,
  migrateAdvancedFiltersRecord,
  normalizeAdvancedFilterExpression,
  serializeAdvancedFilterExpression,
  deserializeAdvancedFilterExpression,
  validateAdvancedFilterExpression,
} from "./advanced-filter-expression.ts";

test("A AND B left-to-right", () => {
  assert.equal(evaluateAdvancedFilterLeftToRight([true, false], ["and"]), false);
  assert.equal(evaluateAdvancedFilterLeftToRight([true, true], ["and"]), true);
});

test("A OR B left-to-right", () => {
  assert.equal(evaluateAdvancedFilterLeftToRight([false, true], ["or"]), true);
  assert.equal(evaluateAdvancedFilterLeftToRight([false, false], ["or"]), false);
});

test("A AND B OR C left-to-right", () => {
  assert.equal(evaluateAdvancedFilterLeftToRight([true, false, true], ["and", "or"]), true);
  assert.equal(evaluateAdvancedFilterLeftToRight([true, true, false], ["and", "or"]), true);
  assert.equal(evaluateAdvancedFilterLeftToRight([true, false, false], ["and", "or"]), false);
});

test("A OR B AND C left-to-right", () => {
  assert.equal(evaluateAdvancedFilterLeftToRight([false, true, false], ["or", "and"]), false);
  assert.equal(evaluateAdvancedFilterLeftToRight([false, true, true], ["or", "and"]), true);
  assert.equal(evaluateAdvancedFilterLeftToRight([true, false, true], ["or", "and"]), true);
});

test("normalizeAdvancedFilterExpression pads connectors with AND", () => {
  const normalized = normalizeAdvancedFilterExpression({
    conditions: [
      { id: "1", field: "status", value: { kind: "status", operator: "in", values: ["Draft"] } },
      { id: "2", field: "title", value: { kind: "text", operator: "contains", query: "phone" } },
    ],
    connectors: [],
  });
  assert.deepEqual(normalized.connectors, ["and"]);
});

test("validateAdvancedFilterExpression rejects invalid connector", () => {
  assert.equal(
    validateAdvancedFilterExpression({
      conditions: [
        { id: "1", field: "a", value: { kind: "text", operator: "contains", query: "x" } },
        { id: "2", field: "b", value: { kind: "text", operator: "contains", query: "y" } },
      ],
      connectors: ["xor" as "and"],
    }),
    "invalid-connector",
  );
});

test("validateAdvancedFilterExpression rejects connector count mismatch", () => {
  assert.equal(
    validateAdvancedFilterExpression({
      conditions: [{ id: "1", field: "a", value: { kind: "text", operator: "contains", query: "x" } }],
      connectors: ["or"],
    }),
    "connector-count-mismatch",
  );
});

test("advanced filter serialization round-trip preserves connectors", () => {
  const expr = normalizeAdvancedFilterExpression({
    conditions: [
      { id: "1", field: "status", value: { kind: "status", operator: "equals", values: ["Published"] } },
      { id: "2", field: "title", value: { kind: "text", operator: "contains", query: "phone" } },
      { id: "3", field: "updatedAt", value: { kind: "date", operator: "after", iso: "2026-01-01T00:00:00.000Z" } },
    ],
    connectors: ["and", "or"],
  });
  const roundTrip = deserializeAdvancedFilterExpression(serializeAdvancedFilterExpression(expr));
  assert.deepEqual(roundTrip.connectors, ["and", "or"]);
  assert.equal(roundTrip.conditions.length, 3);
});

test("migrateAdvancedFiltersRecord defaults legacy views to AND connectors", () => {
  const migrated = migrateAdvancedFiltersRecord(
    {
      status: { kind: "status", operator: "in", values: ["Draft"] },
      title: { kind: "text", operator: "contains", query: "x" },
    },
    ["status", "title"],
  );
  assert.deepEqual(migrated.connectors, ["and"]);
  assert.equal(migrated.conditions.length, 2);
});
