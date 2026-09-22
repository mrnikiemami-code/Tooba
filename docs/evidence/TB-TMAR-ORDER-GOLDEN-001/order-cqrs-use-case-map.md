# Order CQRS use-case map

Current Order HTTP use cases requiring Application commands/queries:

- inventory recovery audit/assessment
- supply status
- list/execute admin order operations
- return eligibility
- notes list/add/delete
- operational history
- invoice and receipt rendering inputs
- storefront/customer/seller order reads and writes identified by the HTTP audit

These routes currently lack complete MediatR 12.5 boundaries and Order Application is absent from `AddToobaCqrsFoundation`. Ceremonial wrappers were not added. R1 must implement real handlers and module endpoint dispatch.
