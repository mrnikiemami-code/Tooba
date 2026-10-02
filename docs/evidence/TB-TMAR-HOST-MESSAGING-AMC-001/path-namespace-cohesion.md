# path-namespace-cohesion — TB-TMAR-HOST-MESSAGING-AMC-001

| File | Namespace now | Target |
|---|---|---|
| all 6 under `/Messaging` | `Tooba.Host` | `Tooba.Host.Messaging` |

State: **VIOLATION** (path↔namespace).

## Cohesion

| File | Types | Cohesion |
|---|---|---|
| MessagingHostOptions.cs | Options + Validator | **MUST_SPLIT** |
| others | 1 type each | OK |

Impact of namespace repair: Program usings, Health (same Host root today — may need `using Tooba.Host.Messaging` for options), Transport registration references, Host.Tests, HostTransportAmcGuardTests string paths unchanged.
