# Outbox compatibility

- No new production workflow Outbox event types in W1
- Process mutations share OrderDbContext SaveChanges with existing outbox interceptor when order rows persist
- Process Manager choreography not activated
