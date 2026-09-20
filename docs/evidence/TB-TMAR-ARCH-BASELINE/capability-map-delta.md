# Capability Map Delta (apply to TOOBA-CAPABILITY-MAP.md)

Add section TMAR / Architecture Recovery:

- Strengths: per-module schema/DbContext/migrations; no cross-schema FK/JOIN; ArchitectureBoundaryTests; Directory/Gateway write boundaries; Outbox/events; ICache foundation
- Debt: Host writes/decisions; no MediatR; Contracts in Application; Catalog convenience types; IMemoryCache bypass; Domain localized errors
- Target: Modular Monolith → low-friction microservices; CQRS+MediatR 12.5.0; per-module Contracts; Host transport-only
- Last Verified Task: TB-TMAR-ARCH-BASELINE
