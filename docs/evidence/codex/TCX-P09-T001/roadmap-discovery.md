# TCX-P09-T001 roadmap discovery

Observed `origin/main`: `872248a8792c2f71141f1e87ebbdfb759db3a998`.

The repository state records P08 Content as the active primary-worker area. Earlier commerce foundations are live, while several explicit gaps remain: `NOTIFICATION_PREFERENCES = DEFERRED`, `REALTIME_NOTIFICATIONS = DEFERRED`, `WALLET_MIXED_TENDER = DEFERRED`, `FULL_VARIANT_MATRIX = DEFERRED`, `FACETED_SEARCH_INTEGRATION = DEFERRED`, `REAL_BANK_PAYMENT_PROVEN = NO`, and `PRODUCTION_GO_LIVE_READY = NO`.

Existing module status relevant to parallel work:

- Notification backend and customer/seller inboxes are live; preferences and realtime delivery are deferred.
- Returns/refunds/restock and customer/seller/admin return surfaces are live foundations.
- Wallet ledger, gift cards, full-wallet checkout, and refund-to-wallet are live; mixed tender is deferred.
- Catalog variant-axis and combination foundations are live; full matrix and faceted-search integration are deferred.

Discovery conclusion: prefer an extension of an already-composed, non-Content module whose first slice is domain/application/persistence-only. Do not start a new cross-cutting module while P08 is active because new module composition would require Host and solution-wide edits.

Protocol boundaries confirmed: Codex executes only `TCX-*`; task artifacts use `.codex.md`; `TB-*` tasks and global TB recovery state are not Codex work; evidence is isolated below `docs/evidence/codex/`.
