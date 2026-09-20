# Transaction preservation
- Shared TransactionScope remains in CheckoutProcessManager.SubmitAsync
- Order + Inventory + Cart still ambient participants
- Cart conversion failure before Complete rolls back Order/Inventory effects
- Process milestones not falsely completed on rollback (Complete only after convert success or raced winner path)
- Adapters do not open nested independent commits
