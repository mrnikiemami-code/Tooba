# Transaction preservation

- `CheckoutSubmitExecutor` retains `TransactionScope` ReadCommitted AsyncFlow
- Order + Inventory + Cart still complete inside one scope
- Conflict recovery retries winner read; inventory.reservation.conflict rethrows if no checkout winner
- Characterization: `CheckoutProcessFoundationTests.Submit_path_still_uses_TransactionScope`
