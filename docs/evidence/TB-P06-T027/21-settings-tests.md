# Settings tests

- File: `Tooba.Host.Tests/SettingsFoundationTests.cs`
- Covers: catalog permissions, HTTP route wiring, preference isolation, operator profile persistence, Party org profile + Person reject, seller capability allow/deny, foreign seller deny, seed idempotency.
- Patterns: Testcontainers + SkippableFact (Docker), FakeAccessControlDirectory selective grants, InMemory authorization for foreign deny.
- Run: `dotnet test src/backend/Tooba.slnx --filter FullyQualifiedName~SettingsFoundationTests`
