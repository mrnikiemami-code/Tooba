# Financial History Projection

`AdminPanelComposer.BuildFinancialEvents` composes Payment + RefundAttempt + SettlementEntry snapshots in memory with idempotent keys (`receipt:`, `refund:`, `settlement:`). Deterministic sort: OccurredAt desc, then EventType, then Reference.
