# options-validator-split — TB-TMAR-HOST-MESSAGING-AMC-001-W1

Pre-W1: `MessagingHostOptions.cs` contained both `MessagingHostOptions` and `MessagingOptionsValidator`.

Post-W1:

- `MessagingHostOptions.cs` → options type only
- `MessagingOptionsValidator.cs` → validator only
- Both under `namespace Tooba.Host.Messaging`

Preserved semantics:

- Enabled + UseInProcessTestDouble conflict rejection
- disabled → Success
- Transport must be PostgreSql / PostgreSQL
- ConnectionReference required when Enabled
- dedicated schema (not catalog/identity/pricing/platform_probe)

Bounded cleanup:

- Removed redundant Production-empty ConnectionReference branch (exact semantic equivalence: Enabled path already rejects empty ConnectionReference before environment branch could run).
- DI-compatible `MessagingOptionsValidator(IHostEnvironment)` ctor retained for Program registration.
