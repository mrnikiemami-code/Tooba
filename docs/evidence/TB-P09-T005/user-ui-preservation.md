# User UI Preservation

Preserved uncommitted user visual refinements folded into T005:

- AppDataGrid: no defaultColDef flex:1 (horizontal scroll when content exceeds width)
- legacy-grid-bridge: actions maxWidth not forced to 156
- admin-screens: orders actions width/minWidth; filterKind on lines/amount/created
- Matching FE tests updated

Items-shipping seller table uses overflow-x-auto + min-w-[920px]; financial tabs untouched.
