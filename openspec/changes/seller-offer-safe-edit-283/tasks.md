# Tasks

## 1. Owner contracts and tests

- [x] 1.1 Add named inventory positions to the seller detail read and optional target `StockItemId` to the seller stock command; verify focused Offer/Inventory tests.
- [x] 1.2 Bind stock writes to the owned offer/seller and named position, refusing ambiguous multi-position callers; verify legacy single-position and negative cases.

## 2. Safe save semantics

- [x] 2.1 Save only changed sections and add explicit clearing of both limits; verify Draft SKU and no-change tests.
- [x] 2.2 Validate response identity/effect and handle wrong/stale/disconnected responses without false success; verify focused client tests.
- [x] 2.3 Redesign the existing edit form for position identity, aggregate stock, dirty/cancel/error feedback and 360px usability; verify mounted tests.

## 3. Evidence

- [x] 3.1 Run Playwright on the real Chromium served page for save/reload, cancel, uncertain response and mobile geometry; record each REQ separately.
- [x] 3.2 Run PostgreSQL rollback-only fixture tests for REQ-01/03/04 and record before/after values and adjustment counts.
- [x] 3.3 Run affected tests, tsc/build, OpenSpec validation and review gate; commit/push and report honestly.
