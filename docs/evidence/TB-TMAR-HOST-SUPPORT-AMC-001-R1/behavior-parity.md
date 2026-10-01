# Behavior parity — Support AMC R1

Preserved order:

1. Create scoped provider
2. Require active store-alpha + assign CommerceContext
3. Ensure AdminDevActorBootstrap
4. AccessControl prelude: seller-dev Ensure + EnsureBootstrap + SyncUserCapabilityTuples (ActorA seller scope)
5. SupportDevelopmentSeedBootstrap.ApplyAsync(guest, sellerPartyId, sellerActorUserId, adminActorUserId)

Cancellation/idempotency semantics unchanged (same early-returns when admin/seller unavailable).
