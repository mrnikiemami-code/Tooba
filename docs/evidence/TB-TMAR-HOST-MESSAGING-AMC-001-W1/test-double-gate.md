# test-double-gate — TB-TMAR-HOST-MESSAGING-AMC-001-W1

`InProcessIntegrationEventPublisher` remains `KEEP_AS_EXPLICIT_TEST_ONLY_HOST_DOUBLE`.

Gate in `MessagingRegistration.AddToobaIntegrationPublisher`:

- `UseInProcessTestDouble` requires `IHostEnvironment.IsEnvironment("Testing")`
- non-Testing throws `InvalidOperationException` before registration
- IServiceProvider / GetServices / MakeGenericType / reflection Invoke remain only in `InProcessIntegrationEventPublisher.cs`

Composition callbacks in `MessagingRegistration` (`sp.GetRequiredService`, MassTransit `context.GetRequiredService`) remain `FRAMEWORK_COMPOSITION_CALLBACK_ALLOWED` and are not treated as the Testing-only service-locator exception.

Durable guard: `HostMessagingAmcW1GuardTests`.
