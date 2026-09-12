# Recovery Command
`OrderInventoryRecoveryComposer.RecoverAsync` via Admin op `recover_inventory_reservation`.
Outcomes: Recovered, AlreadyHealthy, NotEligible, RequiresManualReview, InsufficientInventory (`inventory.recovery.insufficient`).
No Released→Held mutation; new Reserve + ReplaceReservation + Fulfillment rebind.
