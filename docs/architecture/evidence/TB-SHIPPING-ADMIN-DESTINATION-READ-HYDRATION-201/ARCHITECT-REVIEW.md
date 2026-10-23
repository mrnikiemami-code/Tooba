# Architect review — TB201 / 2026-10-23
Disposition: REPAIR_REQUIRED; read bridge not closed.
Reviewed e0c2a2651e413aa7b5b0af3f5011eb7cd9955828 against b1c89e20f495e326a932e9c0b497105b0d89bec6. Source/patch review only.
Direction retained: minimal Geography Contracts bulk-key reader with own EF implementation; Shipping typed-key projection/shared ancestry; additive response; default-content-language label lookup; real GET owner-first and PUT pre-write facts/pure post-save mapping. Schema/transactions/frontend unchanged. Raw selection retention, nullable city admin, requested-id verification and per-operation parent closure are appropriate.

Concrete runtime defect:
ShippingAuthoringDestinationFactsReader.Resolve ServiceArea branch never checks serviceArea.IsActive, unlike Country/AdministrativeArea/City. An inactive service area with active city/country yields Resolved and a write shape. Existing test's "inactive" case covers other kinds, not this terminal. Add an executed pre-fix regression and the one canonical guard; inactive service area must remain raw key + Unresolved/Area null.

Required proof omissions:
- Geography own EF reader executed behavior absent; no Geography.Tests project is not evidence that no integration test can run. Authorize ONE test-only ProjectReference to Geography.Infrastructure from existing Shipping.Tests, then use existing EFInMemory to seed/query real reader (no external database or new project/package). This is test integration only; no production cross-module implementation dependency.
- Resolve both registrations through actual module AddServices + canonical foundations with only DbContext/data boundaries controlled; source/build cannot establish lifetimes/resolution.
- New foreign-read ActivityListener parent/correlation/count/error/token proof and actual new editor facts pre-write/poisoned-second-call handler proof were required and missing. Prior197/199 tests with a new fake port returning onlyUnresolved cannot substitute.
- No need run Postgres storage: newfeature is read-only and transaction code unchanged. Prior storage proof remains narrower reused evidence. Four connection-refused tests are observed blocked validation, not green, and do not establish regression absence. Correct filter to exact nonrelational class names; don't hide required relational proof in a claim.

Worker6 adapterfacts and10 editorfacts passed,4 storage failed connection refused as reported; no Architect rerun. Deadline exceeded, exact elapsed not supplied; preserve deviation and recover receipt times if readily available, never invent. No confidence loop for labels.

Only next task TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R1: one service-area activity guard + minimal existing-test-project EF/DI/tracing/new-handler proof; no read API redesign, schema, UI or further production fix. Then review and continue UI202.
