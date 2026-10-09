# Request → handler → validator matrix (certify §5 + §6a)

Independently re-derived from the shipped route inventory (`group.Map*` call sites) and the actual
`ISender.Send(...)` call sites, then followed to the authoritative `IRequest`/`IRequestHandler`. Not
adopted from the W0/W1 reported matrix.

## Route inventory (10 shipped routes)

| # | Verb + route | Audience file | Dispatch |
|---|---|---|---|
| 1 | `GET /v1/seller/settlements/settlement/balance` | `Endpoints/Seller/SettlementSellerEndpoints.cs` | `sender.Send(GetSellerSettlementBalanceQuery)` |
| 2 | `GET /v1/seller/settlements/settlement/entries` | same | `sender.Send(ListSellerSettlementEntriesQuery)` |
| 3 | `GET /v1/seller/settlements/settlement/statements` | same | `sender.Send(ListSellerSettlementStatementsQuery)` |
| 4 | `GET /v1/seller/settlements/settlement/payout-requests` | same | `sender.Send(ListSellerPayoutRequestsQuery)` |
| 5 | `POST /v1/seller/settlements/settlement/payout-requests` | same | `sender.Send(RequestSellerPayoutCommand)` |
| 6 | `GET /v1/admin/settlements/settlement/balances` | `Endpoints/Admin/SettlementAdminEndpoints.cs` | `sender.Send(ListAdminSettlementBalancesQuery)` |
| 7 | `GET /v1/admin/settlements/settlement/payout-queue` | same | `sender.Send(ListAdminPayoutQueueQuery)` |
| 8 | `POST /v1/admin/settlements/settlement/payout-queue/query` | same | `sender.Send(QueryAdminPayoutGridQuery)` |
| 9 | `POST /v1/admin/settlements/settlement/payout-requests/{payoutRequestId:guid}/process` | same | `sender.Send(ProcessAdminPayoutCommand)` |
| 10 | `POST /v1/admin/settlements/settlement/payout-requests/{payoutRequestId:guid}/retry` | same | `sender.Send(RetryAdminPayoutCommand)` |

`sender.Send(` call sites: **10** (= route count). `DbContext` / `ISettlementDirectory` occurrences in
the endpoint files: **0**.

## Per-request matrix

| Request | Route(s) | Input provenance | Transport-shape risk | Classification | Validator / policy | Stable code path |
|---|---|---|---|---|---|---|
| `RequestSellerPayoutCommand(Guid SellerPartyId, decimal Amount, string IdempotencyKey, …)` | 5 | seller party **server-derived** by `ISettlementSellerAuthorizer`; `Amount` + `IdempotencyKey` **client body** | negative/zero amount; blank idempotency key | `VALIDATOR_REQUIRED` | `RequestSellerPayoutCommandValidator` (`Amount > 0`; `IdempotencyKey` non-blank) | `settlement.validation.*` → canonical `SafeErrorMapper` validation path |
| `ProcessAdminPayoutCommand(Guid PayoutRequestId, Guid ActorUserId)` | 9 | `PayoutRequestId` **route** (`:guid`); `ActorUserId` **server-derived** admin principal | empty `Guid` | `VALIDATOR_REQUIRED` | `ProcessAdminPayoutCommandValidator` (`PayoutRequestId != Guid.Empty`) | `settlement.validation.*` |
| `RetryAdminPayoutCommand(Guid PayoutRequestId, Guid ActorUserId)` | 10 | `PayoutRequestId` **route** (`:guid`); `ActorUserId` **server-derived** admin principal | empty `Guid` | `VALIDATOR_REQUIRED` | `RetryAdminPayoutCommandValidator` (`PayoutRequestId != Guid.Empty`) | `settlement.validation.*` |
| `QueryAdminPayoutGridQuery(GridQueryRequest Request)` | 8 | **client body** (grid request object) | null body; malformed grid envelope | `VALIDATOR_REQUIRED` | `QueryAdminPayoutGridQueryValidator` (body non-null) **plus** the module-owned `AdminPayoutGridQueryPolicy` executed inside the handler (whitelist/normalization authority) | `settlement.validation.*` / grid policy → `SemanticError(ex.ErrorCode)` |
| `GetSellerSettlementBalanceQuery(Guid SellerPartyId)` | 1 | seller party **server-derived** by the authorizer seam; no client-shaped input | none (no client-supplied value reaches a rule) | `NO_VALIDATOR_REQUIRED` — `AUTH_SCOPED_QUERY` | — | — |
| `ListSellerSettlementEntriesQuery(Guid SellerPartyId)` | 2 | same | none | `NO_VALIDATOR_REQUIRED` — `AUTH_SCOPED_QUERY` | — | — |
| `ListSellerSettlementStatementsQuery(Guid SellerPartyId)` | 3 | same | none | `NO_VALIDATOR_REQUIRED` — `AUTH_SCOPED_QUERY` | — | — |
| `ListSellerPayoutRequestsQuery(Guid SellerPartyId)` | 4 | same | none | `NO_VALIDATOR_REQUIRED` — `AUTH_SCOPED_QUERY` | — | — |
| `ListAdminSettlementBalancesQuery` | 6 | **no bound input** (parameterless) | none | `NO_VALIDATOR_REQUIRED` — `NO_INPUT` | — | — |
| `ListAdminPayoutQueueQuery` | 7 | **no bound input** (parameterless) | none | `NO_VALIDATOR_REQUIRED` — `NO_INPUT` | — | — |

## Set equality (6a)

| Set | Count | Equality |
|---|---|---|
| Shipped routes | 10 | = |
| `ISender.Send` call sites | 10 | = |
| Endpoint-reachable request types | 10 | = |
| Classified request types | 10 (4 + 6) | each exactly once |

## Handler proof

Every request has exactly one `IRequestHandler<TRequest, Result<…>>` declaration in
`Application/Payouts/{Commands,Queries}`:

```text
ProcessAdminPayoutCommand            : IRequestHandler<ProcessAdminPayoutCommand, Result<PayoutRequestSnapshot>>
RequestSellerPayoutCommand           : IRequestHandler<RequestSellerPayoutCommand, Result<PayoutRequestSnapshot>>
RetryAdminPayoutCommand              : IRequestHandler<RetryAdminPayoutCommand, Result<PayoutRequestSnapshot>>
GetSellerSettlementBalanceQuery      : IRequestHandler<GetSellerSettlementBalanceQuery, Result<SettlementBalanceSnapshot>>
ListAdminPayoutQueueQuery            : IRequestHandler<ListAdminPayoutQueueQuery, Result<IReadOnlyList<PayoutRequestSnapshot>>>
ListAdminSettlementBalancesQuery     : IRequestHandler<ListAdminSettlementBalancesQuery, Result<IReadOnlyList<AdminSettlementBalanceListItem>>>
ListSellerPayoutRequestsQuery        : IRequestHandler<ListSellerPayoutRequestsQuery, Result<IReadOnlyList<PayoutRequestSnapshot>>>
ListSellerSettlementEntriesQuery     : IRequestHandler<ListSellerSettlementEntriesQuery, Result<IReadOnlyList<SettlementEntrySnapshot>>>
ListSellerSettlementStatementsQuery  : IRequestHandler<ListSellerSettlementStatementsQuery, Result<IReadOnlyList<SettlementStatementSnapshot>>>
QueryAdminPayoutGridQuery            : IRequestHandler<QueryAdminPayoutGridQuery, Result<GridPageResponse<AdminPayoutListItem>>>
```

## Validator discovery / execution proof

```text
Host Program.cs:
  AddToobaCqrsFoundation(
      typeof(Tooba.Settlement.Application.Payouts.Queries.GetSellerSettlementBalanceQuery).Assembly, …)

TmarFoundation.cs:
  services.AddValidatorsFromAssembly(assembly);
  services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

The four validators live in one flat capability leaf
(`Application/Validation/SettlementRequestValidators.cs`), are discoverable through the canonical
`AddValidatorsFromAssembly` over the Settlement Application assembly, and are executed by the canonical
`ValidationBehavior<,>` before the handler.

## Exemption reasoning (non-circular)

- The four `AUTH_SCOPED_QUERY` requests carry a single `Guid SellerPartyId` that is **produced by the
  server** from the authenticated seller principal (`ISettlementSellerAuthorizer`), never bound from a
  client body; there is no other bound input to validate.
- The two `NO_INPUT` requests are parameterless records; no client value is bound at all.
- Route-constrained `:guid` segments cannot deliver a non-guid value to a handler.
- Optional input was **not** treated as evidence of safety: the only requests carrying client-shaped
  body input are exactly the four `VALIDATOR_REQUIRED` ones.
