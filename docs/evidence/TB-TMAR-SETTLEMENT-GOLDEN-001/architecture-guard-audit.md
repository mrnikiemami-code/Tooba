# Architecture guard audit

Upgraded SettlementArchitectureGuardTests:

1. Settlement_golden_boundaries_and_physical_layout_remain_clean — Endpoints project, refs, Host/Settlement absent, no Host Payouts policy, physical/path↔namespace, no TypeForwardedTo/clock bypass/silent catch.
2. Settlement_endpoints_cqrs_and_host_ownership_are_enforced — ISender + ApiResponseFactory, exact routes, use-case Commands/Queries, Program MapSettlementEndpoints + authorizer DI.
3. Settlement_exception_mapper_is_stable_codes_only_without_prose_heuristics — no Contains/StartsWith heuristics; unknown IOE not swallowed.

Plus focused EndpointOwnershipTests and SettlementErrorAndGridTests.

Settlement-Architecture-Guards: ENFORCED
Settlement-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Settlement-CrossModule-Boundary: CONTRACTS_ONLY
