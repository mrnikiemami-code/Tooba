# worker-scope — TB-TMAR-HOST-OUTBOX-AMC-001

## Pattern

`OutboxDispatcher` is **singleton**. Per message:

```text
await using var scope = _scopes.CreateAsyncScope();
scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>();
scope.ServiceProvider.GetRequiredService<IStoreCommerceContextAssigner>();
scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
```

## Classification

`LEGITIMATE_PER_MESSAGE_WORKER_SCOPE_COMPOSITION`

- Scoped assigners/publisher require a scope; constructor injection on singleton would be wrong.
- Not generic service-locator anti-pattern; typed resolve of known platform seams.
- Typed `WorkerScopeRunner` abstraction = optional later; **overengineering for W1**.

Do not mechanically reject `scope.ServiceProvider` here.
