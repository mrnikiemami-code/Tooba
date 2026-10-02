# testing-double — TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT

`TESTING_ONLY_SERVICE_LOCATION_EXCEPTION_CERTIFIED`

- Type is `internal sealed`
- IServiceProvider / GetServices / MakeGenericType / MethodInfo Invoke only in InProcessIntegrationEventPublisher
- Registration only after `IsEnvironment("Testing")`
- Non-Testing selection throws
- Does NOT establish a general service-locator exemption
- Framework `GetRequiredService` in MessagingRegistration remains `FRAMEWORK_COMPOSITION_CALLBACK_ALLOWED`

Reflection dispatch: handler interface from runtime event type; all registered handlers invoked; zero handlers = successful no-op + metric; cancellation forwarded; does not prove transport/inbox/retry parity.
