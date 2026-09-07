# R1 grid widths

Orders `orderColumns` in `admin-screens.tsx`: removed all `maxWidth` constraints; kept `width`/`minWidth` and resizable defaults from AppDataGrid (`resizable: true`, no Orders-specific max in defaultColDef).

Acceptance: columns can be dragged wider than initial width; design/order unchanged.
