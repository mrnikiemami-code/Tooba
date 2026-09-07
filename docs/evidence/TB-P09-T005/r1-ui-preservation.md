# R1 UI Preservation

Verified still present (no redesign in R1):

- `AppDataGrid.tsx`: no `defaultColDef.flex:1` — horizontal scroll comment retained
- `legacy-grid-bridge.ts`: actions maxWidth open when unset
- `admin-screens.tsx` orders: actions `width:120` / `minWidth:100`; `filterKind` on lines/amount/created
- Items shipping: `overflow-x-auto` + `data-testid=admin-order-seller-lines-scroll-*`
- Return deadline column consumes `returnDeadlineDisplay` / `returnRemainingDisplay`

FE `/admin/orders` and order detail routes → HTTP 200.
USER_VISUAL_ACCEPTED=NO.
