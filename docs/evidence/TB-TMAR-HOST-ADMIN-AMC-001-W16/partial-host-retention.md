# Partial Host retention — W16

| File | State |
|---|---|
| ProductWorkspaceEndpoints.cs | RETAINED_PARTIAL — publish/readiness map + method removed; lifecycle + other routes kept |
| ProductWorkspaceComposer.cs | RETAINED_PARTIAL — public GetPublishReadinessAsync removed; MapPublishReadiness kept for aggregate |
| ProductWorkspaceModels.cs | RETAINED_PARTIAL — ProductPublishReadinessView / MissingRequirementView kept for aggregate publication |
| Host/Admin `*.cs` count | 52 → 52 |
| StoreAppearance | Deferred (not started) |
| W17 / next Host folder | Not started |
