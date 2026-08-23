import assert from "node:assert/strict";
import { test } from "node:test";
import { executeGridQuery, rowsToCsv } from "./query-engine.ts";
import {
  clampWidth,
  cycleSort,
  defaultLayout,
  deserializeGridQuery,
  deserializeSavedView,
  isFilterActive,
  moveColumn,
  normalizeIsoDate,
  normalizeMoney,
  selectPage,
  serializeGridQuery,
  serializeSavedView,
  stickyLogicalSide,
  toggleSelection,
  visibleExportColumns,
} from "./serialize.ts";

const rows = Array.from({ length: 47 }, (_, index) => ({
  id: `row-${index + 1}`,
  reference: `OPS-${String(1000 + index)}`,
  amount: 120000 + index * 1500,
  status: index % 3 === 0 ? "pending" : index % 3 === 1 ? "open" : "closed",
}));

const columns = [
  { id: "reference", header: "Reference", accessor: (row: (typeof rows)[number]) => row.reference, width: 120, minWidth: 80, maxWidth: 200 },
  { id: "amount", header: "Amount", accessor: (row: (typeof rows)[number]) => row.amount, width: 120, minWidth: 80, maxWidth: 200 },
  { id: "status", header: "Status", accessor: (row: (typeof rows)[number]) => row.status, width: 120, minWidth: 80, maxWidth: 200 },
];

test("filter and sort serialization round-trips", () => {
  const raw = serializeGridQuery({
    page: 2,
    pageSize: 20,
    sorts: [{ columnId: "amount", direction: "desc" }],
    filters: { reference: { kind: "text", operator: "contains", query: "OPS" } },
    search: "north",
  });
  const parsed = deserializeGridQuery(raw);
  assert.equal(parsed.page, 2);
  assert.equal(parsed.sorts[0]?.columnId, "amount");
});

test("saved view round-trips", () => {
  const view = {
    id: "v1",
    name: "ops",
    filters: {},
    sorts: [],
    layout: defaultLayout(["a"], { a: 120 }),
    pageSize: 10,
  };
  assert.equal(deserializeSavedView(serializeSavedView(view)).name, "ops");
});

test("column order and visibility persist", () => {
  const layout = defaultLayout(["a", "b"], { a: 100, b: 120 });
  const moved = { ...layout, order: moveColumn(layout.order, "b", "a") };
  moved.visibility.a = false;
  assert.deepEqual(moved.order, ["b", "a"]);
  assert.equal(moved.visibility.a, false);
});

test("page state and money/date normalization", () => {
  const money = normalizeMoney(12.5, "irr");
  assert.equal(money.currency, "IRR");
  assert.equal(normalizeIsoDate("2026-04-03T00:00:00Z"), "2026-04-03");
  assert.equal(clampWidth(10, 80, 200), 80);
});

test("rtl sticky uses logical inline start", () => {
  assert.equal(stickyLogicalSide("start"), "inline-start");
  assert.equal(stickyLogicalSide("end"), "inline-end");
});

test("selection and export columns", () => {
  const selected = toggleSelection(new Set(), "1");
  assert.equal(selected.has("1"), true);
  const page = selectPage(["1", "2"]);
  assert.equal(page.size, 2);
  const layout = defaultLayout(["a", "b"], { a: 80, b: 80 });
  layout.visibility.b = false;
  assert.deepEqual(visibleExportColumns(layout, ["a", "b"]), ["a"]);
});

test("server query adapter pages and filters", () => {
  const page = executeGridQuery(rows, columns, {
    page: 1,
    pageSize: 10,
    sorts: [{ columnId: "amount", direction: "asc" }],
    filters: { status: { kind: "status", values: ["open"] } },
  });
  assert.equal(page.rows.length, 10);
  assert.ok(page.total < rows.length);
  const csv = rowsToCsv(page.rows, columns, ["reference", "amount"]);
  assert.match(csv, /Reference/);
});

test("sort cycle and filter activity", () => {
  assert.equal(cycleSort([], "reference")[0]?.direction, "asc");
  assert.equal(isFilterActive({ kind: "boolean", state: "all" }), false);
  assert.equal(isFilterActive({ kind: "text", operator: "contains", query: "x" }), true);
});
