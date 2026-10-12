// TB-FE-LEGACY-GRID-FIELD-CONTRACT-001-R1 runtime verification (reproducible evidence).
//
// Establishes, with the repository's real bridge and real query engine plus a real AG Grid
// (headless in jsdom), whether removing `ColDef.field` from legacy-grid-bridge.ts changes the
// visible row order and sort direction for:
//   A. a sortable column whose id is NOT a property on the row (orders amount / customer);
//   B. a sortable column whose id IS a real property and whose accessor returns it (orders reference);
//   C. a sortable column whose id IS a real property but whose accessor returns something else
//      (landing pages slug -> pathLabel) - the only such sortable column found in the suite;
//   D. a `row.<id> ?? ""` accessor, where the engine key is "" and the old AG Grid key was undefined.
//
// Run from the repository root:
//   node --experimental-strip-types docs/architecture/evidence/TB-FE-LEGACY-GRID-FIELD-CONTRACT-001-R1/runtime-verification.ts

import { createGrid, AllCommunityModule, ModuleRegistry } from "../../../../src/frontend/node_modules/ag-grid-community/dist/package/main.esm.mjs";
import { JSDOM } from "../../../../src/frontend/node_modules/jsdom/lib/api.js";
import { buildLegacyGridBridge } from "../../../../src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts";
import { executeGridQuery } from "../../../../src/frontend/design-system/data-grid/query-engine.ts";

ModuleRegistry.registerModules([AllCommunityModule]);

const dom = new JSDOM("<!doctype html><html><body></body></html>", { pretendToBeVisual: true });
globalThis.window = dom.window;
globalThis.document = dom.window.document;
Object.defineProperty(globalThis, "navigator", { value: dom.window.navigator, configurable: true });
for (const name of [
  "HTMLElement", "HTMLDivElement", "HTMLInputElement", "Element", "Node", "NodeList", "DocumentFragment",
  "Event", "CustomEvent", "MouseEvent", "KeyboardEvent", "FocusEvent", "TouchEvent", "WheelEvent",
  "MutationObserver", "ResizeObserver", "IntersectionObserver", "DOMParser", "XMLHttpRequest",
]) {
  const value = dom.window[name];
  if (value !== undefined) globalThis[name] = value;
}
globalThis.requestAnimationFrame = dom.window.requestAnimationFrame?.bind(dom.window)
  ?? ((callback) => setTimeout(() => callback(Date.now()), 0));
globalThis.cancelAnimationFrame = dom.window.cancelAnimationFrame?.bind(dom.window) ?? ((handle) => clearTimeout(handle));
globalThis.matchMedia = dom.window.matchMedia?.bind(dom.window)
  ?? (() => ({ matches: false, addEventListener() {}, removeEventListener() {}, addListener() {}, removeListener() {} }));
globalThis.getComputedStyle = dom.window.getComputedStyle.bind(dom.window);

const query = (columnId, direction) => ({
  page: 1,
  pageSize: 20,
  sorts: [{ columnId, direction }],
  filters: {},
  advancedFilter: { conditions: [], connectors: [] },
});

/** Column defs exactly as the bridge produces them today, plus the pre-repair `field` mirrored back. */
function columnDefsFor(columns) {
  const withField = buildLegacyGridBridge(columns).columnDefs.map((colDef) =>
    colDef.colId === "actions" ? colDef : { ...colDef, field: colDef.colId },
  );
  const withFieldAbsent = buildLegacyGridBridge(columns).columnDefs;
  return { withField, withFieldAbsent };
}

function displayedOrder(columnDefs, rows, sort) {
  const host = document.createElement("div");
  document.body.appendChild(host);
  const agApi = createGrid(host, {
    getRowId: (params) => params.data.id,
    columnDefs,
    rowData: rows.map((row) => ({ ...row })),
  });
  // A header click sets this sort state; the app then re-queries and replaces rowData.
  agApi.applyColumnState({ state: [{ colId: sort.columnId, sort: sort.direction }], defaultState: { sort: null } });
  agApi.setGridOption("rowData", rows.map((row) => ({ ...row })));
  const order = [];
  agApi.forEachNodeAfterFilterAndSort((node) => order.push(node.data.id));
  const sortState = agApi.getColumnState().filter((col) => col.sort).map((col) => `${col.colId}:${col.sort}`);
  agApi.destroy();
  return { order, sortState };
}

function runCase(label, fixtureColumns, fixtureRows, sorts) {
  process.stdout.write(`\n=== ${label} ===\n`);
  const { withField, withFieldAbsent } = columnDefsFor(fixtureColumns);
  for (const sort of sorts) {
    const enginePage = executeGridQuery(fixtureRows, fixtureColumns, query(sort.columnId, sort.direction));
    const engineOrder = enginePage.rows.map((row) => row.id).join(",");
    const gridWithField = displayedOrder(withField, enginePage.rows, sort);
    const gridWithFieldAbsent = displayedOrder(withFieldAbsent, enginePage.rows, sort);
    const fieldOrder = gridWithField.order.join(",");
    const absentOrder = gridWithFieldAbsent.order.join(",");
    process.stdout.write(
      `sort ${sort.columnId} ${sort.direction}\n`
      + `  engine order            : ${engineOrder}\n`
      + `  grid visible (field set)     : ${fieldOrder}   ${fieldOrder === engineOrder ? "SAME AS ENGINE" : "DIFFERS FROM ENGINE"}\n`
      + `  grid visible (field absent)  : ${absentOrder}   ${absentOrder === engineOrder ? "SAME AS ENGINE" : "DIFFERS FROM ENGINE"}\n`
      + `  ag-grid sort state (with field)    : ${gridWithField.sortState.join("|")}\n`
      + `  ag-grid sort state (field absent)  : ${gridWithFieldAbsent.sortState.join("|")}\n`
      + `  visible delta field-set vs field-absent: ${fieldOrder === absentOrder ? "NONE" : "PRESENT"}\n`,
    );
  }
}

// A. sortable column whose id is NOT a property on the row (mirrors orders amount / customer).
runCase(
  "A. id is not a row property (orders-like customer/amount)",
  [
    { id: "customer", header: "customer", accessor: (row) => row.customerDisplayName, filterKind: "text", sortable: true, width: 100, minWidth: 80 },
    { id: "amount", header: "amount", accessor: (row) => row.payableAmount, filterKind: "money", sortable: true, width: 100, minWidth: 80 },
  ],
  [
    { id: "o1", customerDisplayName: "Zeta", payableAmount: 30 },
    { id: "o2", customerDisplayName: "Alpha", payableAmount: 10 },
    { id: "o3", customerDisplayName: "Mid", payableAmount: 20 },
  ],
  [{ columnId: "customer", direction: "asc" }, { columnId: "amount", direction: "desc" }],
);

// B. sortable column whose id IS a real property and the accessor returns it.
runCase(
  "B. id is a real row property, accessor returns it (orders-like reference)",
  [
    { id: "reference", header: "reference", accessor: (row) => row.reference, filterKind: "text", sortable: true, width: 100, minWidth: 80 },
  ],
  [
    { id: "o1", reference: "C" },
    { id: "o2", reference: "A" },
    { id: "o3", reference: "B" },
  ],
  [{ columnId: "reference", direction: "asc" }, { columnId: "reference", direction: "desc" }],
);

// C. sortable column whose id IS a real property but the accessor returns a different value
//    (landing pages: id "slug", accessor row.pathLabel, where Home's pathLabel is "").
runCase(
  "C. id is a real row property, accessor differs (landing pages slug -> pathLabel)",
  [
    { id: "slug", header: "slug", accessor: (row) => row.pathLabel, filterKind: "text", sortable: true, width: 100, minWidth: 80 },
  ],
  [
    { id: "z", slug: "zzz", pathLabel: "/landing/zzz" },
    { id: "h", slug: "home", pathLabel: "" },
    { id: "a", slug: "aaa", pathLabel: "/landing/aaa" },
  ],
  [{ columnId: "slug", direction: "asc" }],
);

// D. `row.<id> ?? ""` accessor where the property can be null (engine key "" vs old AG Grid key undefined).
runCase(
  "D. accessor row.<id> ?? \"\" with a null value",
  [
    { id: "reference", header: "reference", accessor: (row) => row.reference ?? "", filterKind: "text", sortable: true, width: 100, minWidth: 80 },
  ],
  [
    { id: "o1", reference: "C" },
    { id: "o2", reference: null },
    { id: "o3", reference: "A" },
  ],
  [{ columnId: "reference", direction: "asc" }],
);
