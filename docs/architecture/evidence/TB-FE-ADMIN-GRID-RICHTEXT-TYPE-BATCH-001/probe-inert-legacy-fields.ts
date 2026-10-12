/**
 * TB-FE-ADMIN-GRID-RICHTEXT-TYPE-BATCH-001 — runtime proof that the removed legacy
 * `flex`, `pinned` and `filter` fields never reached AG Grid.
 *
 * Run:  cd src/frontend && node --experimental-strip-types ../../docs/architecture/evidence/<Task-ID>/probe-inert-legacy-fields.ts
 * (evidence only; not part of the frontend tsconfig include)
 */
import assert from "node:assert/strict";
import { buildLegacyGridBridge } from "../../../../src/frontend/design-system/app-data-grid/legacy-grid-bridge.ts";

const withoutLegacyFields = [
    { id: "name", header: "نام", accessor: () => "n", width: 140, minWidth: 120 },
    { id: "code", header: "کد", accessor: () => "c", width: 110, minWidth: 100 },
    {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        width: 88,
        minWidth: 72,
        maxWidth: 96,
        sortable: false,
    },
];

// Exactly the pre-repair shape, including the removed legacy extras.
const withLegacyFields = [
    { id: "name", header: "نام", accessor: () => "n", width: 140, minWidth: 120, flex: 1.2 },
    { id: "code", header: "کد", accessor: () => "c", width: 110, minWidth: 100, flex: 1.4 },
    {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        pinned: "left",
        width: 88,
        minWidth: 72,
        maxWidth: 96,
        sortable: false,
        filter: false,
    },
];

for (const direction of ["rtl", "ltr"] as const) {
    const before = buildLegacyGridBridge(withLegacyFields, direction);
    const after = buildLegacyGridBridge(withoutLegacyFields, direction);

    console.log(`\n[${direction}] columnDefs (post-repair input):`);
    for (const def of after.columnDefs) {
        console.log("  ", JSON.stringify(def, (_k, v) => (typeof v === "function" ? "[fn]" : v)));
    }

    // 1. flex is never read: the produced ColDefs are identical with and without it.
    // (Compared as serialized projections because cellRenderer is a fresh closure each call.)
    const shape = (defs: unknown[]) => JSON.stringify(defs, (_k, v) => (typeof v === "function" ? "[fn]" : v));
    assert.equal(
        shape(after.columnDefs),
        shape(before.columnDefs),
        `${direction}: flex/pinned/filter changed output`,
    );
    assert.equal(
        after.columnDefs.some((def) => Object.prototype.hasOwnProperty.call(def, "flex")),
        false,
        `${direction}: no produced ColDef carries flex`,
    );

    // 2. actions pinning is decided by the bridge from the direction only.
    const actions = after.columnDefs.at(-1);
    assert.equal(actions?.colId, "actions");
    assert.equal(actions?.pinned, direction === "rtl" ? "left" : "right");
    assert.equal(actions?.lockPinned, true);

    // 3. actions filtering is hard-coded false by the bridge.
    assert.equal(actions?.filter, false);

    // 4. explicit widths survive verbatim.
    assert.deepEqual(
        after.columnDefs.map((def) => [def.colId, def.width, def.minWidth, def.maxWidth]),
        [
            ["name", 140, 120, undefined],
            ["code", 110, 100, undefined],
            ["actions", 88, 72, 96],
        ],
    );
}

console.log("\nPROBE OK — flex/pinned/filter are inert; pinning, filtering and widths unchanged.");
