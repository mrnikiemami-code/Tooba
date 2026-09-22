# Behavior Preservation Audit

## Preserved
- Customer list/create/get/reply/close/reopen
- Seller list/create/get/reply/close/reopen
- Admin list/get/reply/patch
- Admin demo-preview: Development only; non-dev 404; not-ready 503
- Filters: page/pageSize/status/requester/category/priority/q
- Ticket ownership/scoping via directory
- Seller caps: support.view / create / reply
- Admin caps: support.view / manage + Unavailable fail-open
- Idempotency-Key on create + replies (header → command)
- Internal note (admin reply `IsInternalNote`)
- Related entity fields on create
- Notification.Contracts-only from Infrastructure
- Outbox registration unchanged

## Intentional golden deltas (not accidental)
- Failures now ProblemDetails via ApiResponseFactory (was ad-hoc JSON)
- Unknown `InvalidOperationException` no longer swallowed as generic rejected

## Validation
- Support.Tests: 12 passed (behavior + CQRS + semantics + routes + architecture)
- `dotnet build src/backend/Tooba.slnx`: succeeded

## Verdict
**Support-Behavior-Preservation: VERIFIED**
