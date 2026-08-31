import assert from "node:assert/strict";
import test from "node:test";
import { buildLegacyGridBridge } from "./legacy-grid-bridge.ts";
import type { GridColumnDef } from "../data-grid/types.ts";

type Row = { id: string; name: string; status: string };

const columns: GridColumnDef<Row>[] = [
  {
    id: "name",
    header: "نام",
    accessor: (row) => row.name,
    width: 120,
    minWidth: 80,
    maxWidth: 200,
    filterKind: "text",
    sortable: true,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    width: 100,
    minWidth: 80,
    maxWidth: 140,
    filterKind: "status",
    enumOptions: [{ value: "Active", label: "فعال" }],
  },
];

test("buildLegacyGridBridge maps legacy columns to ColDef with external filters", () => {
  const bridge = buildLegacyGridBridge(columns);
  assert.equal(bridge.columnDefs.length, 2);
  assert.equal(bridge.externalFilterFields.length, 2);
  assert.deepEqual(bridge.exportHeaders, ["نام", "وضعیت"]);
  assert.equal(bridge.columnDefs[0]?.headerComponent, "appColumnHeader");
});

test("buildLegacyGridBridge export row uses accessors", () => {
  const bridge = buildLegacyGridBridge(columns);
  assert.deepEqual(bridge.getExportRow({ id: "1", name: "a", status: "Active" }), ["a", "Active"]);
});
