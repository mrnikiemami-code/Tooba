# Order microservice extraction proof

Target remains HTTP → Order.Endpoints → Order.Application → Domain/Infrastructure → Order DB, with foreign capabilities reached through Contracts/events.

Today, Order persistence is module-owned and several outbound seams are contract-based. Extraction still requires business restructuring because Host owns Order HTTP composition, Order.Endpoints is absent, real HTTP MediatR handlers are absent, and foreign Application dependencies remain.

Verdict: `NOT_READY_WITHOUT_BUSINESS_REWRITE`. R1 must close those concrete seams while preserving Checkout W1-W5.
