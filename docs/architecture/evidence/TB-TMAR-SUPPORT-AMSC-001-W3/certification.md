# TB-TMAR-SUPPORT-AMSC-001-W3 — Certification Evidence (ARCH-COMPLETE-002)

Wave 3 (Certify) of the AMSC re-standardization of `src/backend/Modules/Support`, executed by the
`tooba-architecture-certify` skill over the structure authority of W2.

- **Task**: `TB-TMAR-SUPPORT-AMSC-001-W3`
- **Parent**: `TB-TMAR-SUPPORT-AMSC-001-W2`
- **Skill**: `tooba-architecture-certify`
- **Target**: `src/backend/Modules/Support/Tooba.Support.*`
- **Starting head**: `aaa15b03e5cfa2ed1bef1c8244e0c621c3786980` (`HEAD == origin/main`)
- **Verdict**: `COMPLETE_REFERENCE_PATTERN`
- **Lock version**: `ARCH-COMPLETE-002`
- **Structure state**: `STRUCTURE_CERTIFIED`

## 1. Wave lineage

| Wave | Task | Commit | State |
| --- | --- | --- | --- |
| W0 | `TB-TMAR-SUPPORT-AMSC-001-W0` (analyze) | `567ac400` | `ANALYZE_COMPLETE` / `READY_TO_MIGRATE` |
| W1 | `TB-TMAR-SUPPORT-AMSC-001-W1` (migrate) | `5e8c86ef` | `MIGRATED_READY_FOR_STRUCTURE` |
| W2 | `TB-TMAR-SUPPORT-AMSC-001-W2` (structure) | `aaa15b03` | `STRUCTURE_READY_FOR_CERTIFY` |
| W3 | `TB-TMAR-SUPPORT-AMSC-001-W3` (certify) | this wave | `SUPPORT_AMSC_001_CERTIFIED` |

Each wave's recorded `startingHead` equals its parent wave's commit, so the chain is verifiable end to end.

## 2. HTTP ownership and route surface

`HTTP_OWNING` module owning exactly **17 module-owned routes**; **Host Support route count = 0**.

| Audience | Prefix | Routes |
| --- | --- | --- |
| Customer | `/v1/customer/support` | `GET /tickets`, `POST /tickets`, `GET /tickets/{ticketId:guid}`, `POST /tickets/{ticketId:guid}/replies`, `POST /tickets/{ticketId:guid}/close`, `POST /tickets/{ticketId:guid}/reopen` |
| Seller | `/v1/seller/support` | `GET /tickets`, `POST /tickets`, `GET /tickets/{ticketId:guid}`, `POST /tickets/{ticketId:guid}/replies`, `POST /tickets/{ticketId:guid}/close`, `POST /tickets/{ticketId:guid}/reopen` |
| Admin | `/v1/admin/support` | `GET /tickets`, `GET /tickets/{ticketId:guid}`, `POST /tickets/{ticketId:guid}/replies`, `PATCH /tickets/{ticketId:guid}`, `GET /demo-preview` |

All 17 routes dispatch through `ISender` (17/17 `sender.Send(` call sites), each to a real MediatR
`IRequest` with exactly one `IRequestHandler`. No endpoint reaches persistence or a directory directly,
and no Host folder, file or route owns Support HTTP.

## 3. Exhaustive request / validator matrix (certify §6a HARD BLOCKER)

Derived from the actual endpoint request construction on disk — not from prose or historical counts.

**17 shipped routes == 17 endpoint-reachable requests == 9 VALIDATOR_REQUIRED + 8 NO_VALIDATOR_REQUIRED**,
each classified exactly once, with no orphan, duplicate or unclassified dispatch.

| # | Route + verb | Request | Class | Validator / reason |
| --- | --- | --- | --- | --- |
| 1 | GET `/v1/customer/support/tickets` | `ListCustomerTicketsQuery` | VALIDATOR_REQUIRED | `ListCustomerTicketsQueryValidator` (free-text `status`) |
| 2 | POST `/v1/customer/support/tickets` | `CreateCustomerTicketCommand` | VALIDATOR_REQUIRED | `CreateCustomerTicketCommandValidator` |
| 3 | GET `/v1/customer/support/tickets/{ticketId:guid}` | `GetCustomerTicketQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived actor |
| 4 | POST `/v1/customer/support/tickets/{ticketId:guid}/replies` | `ReplyCustomerTicketCommand` | VALIDATOR_REQUIRED | `ReplyCustomerTicketCommandValidator` (body + idempotency key) |
| 5 | POST `/v1/customer/support/tickets/{ticketId:guid}/close` | `CloseCustomerTicketCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived actor |
| 6 | POST `/v1/customer/support/tickets/{ticketId:guid}/reopen` | `ReopenCustomerTicketCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived actor |
| 7 | GET `/v1/seller/support/tickets` | `ListSellerTicketsQuery` | VALIDATOR_REQUIRED | `ListSellerTicketsQueryValidator` (free-text `status`) |
| 8 | POST `/v1/seller/support/tickets` | `CreateSellerTicketCommand` | VALIDATOR_REQUIRED | `CreateSellerTicketCommandValidator` |
| 9 | GET `/v1/seller/support/tickets/{ticketId:guid}` | `GetSellerTicketQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived seller party |
| 10 | POST `/v1/seller/support/tickets/{ticketId:guid}/replies` | `ReplySellerTicketCommand` | VALIDATOR_REQUIRED | `ReplySellerTicketCommandValidator` |
| 11 | POST `/v1/seller/support/tickets/{ticketId:guid}/close` | `CloseSellerTicketCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived seller party |
| 12 | POST `/v1/seller/support/tickets/{ticketId:guid}/reopen` | `ReopenSellerTicketCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived seller party |
| 13 | GET `/v1/admin/support/tickets` | `ListAdminTicketsQuery` | VALIDATOR_REQUIRED | `ListAdminTicketsQueryValidator` (5 free-text filters + search) |
| 14 | GET `/v1/admin/support/tickets/{ticketId:guid}` | `GetAdminTicketQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint only |
| 15 | POST `/v1/admin/support/tickets/{ticketId:guid}/replies` | `ReplyAdminTicketCommand` | VALIDATOR_REQUIRED | `ReplyAdminTicketCommandValidator` |
| 16 | PATCH `/v1/admin/support/tickets/{ticketId:guid}` | `PatchAdminTicketCommand` | VALIDATOR_REQUIRED | `PatchAdminTicketCommandValidator` (free-text status/priority) |
| 17 | GET `/v1/admin/support/demo-preview` | `GetSupportDemoPreviewQuery` | NO_VALIDATOR_REQUIRED | parameterless + `Development`-gated at the endpoint (`Results.NotFound()` outside Development) |

Optional input is **not** treated as evidence of safety: every exemption rests on a `:guid` route
constraint and/or a server-derived actor/seller party (`TryResolveActor` / `RequireAuthorizedAsync`).
Validators emit stable machine codes only (zero `WithMessage(`) and duplicate no business rule.

## 4. Canonical mechanisms

- **Typed-fault seam**: `SupportOperation.ExecuteAsync` maps `ContractOperationException` under
  `SupportErrorCodes.IsKnown` and `SemanticException` to `Result`; classification is by typed code only
  (zero `.Message.Contains(` / `StartsWith` / `==`, retired `SupportExceptionMapper` absent).
- **API results**: `ApiResponseFactory.From` / `FromFailure` on the error path; zero
  `Results.BadRequest` / `Results.Problem` / `ProblemDetails`. The two `Results.Json(..., 201)` create
  responses and the single Development-gated `Results.NotFound()` are the locked shipped shapes.
- **Stable codes**: the single canonical `Tooba.Support.Contracts.Errors.SupportErrorCodes` home declares
  **28** codes = **7** `HttpReachable` + **21** `DomainInvariants`; the contributor registers exactly the
  **7** HTTP-reachable descriptors once (Foundation owns the shared cross-cutting codes).
- **Localization**: `SupportErrors.resx` + `SupportErrors.fa.resx` carry **28 EN + 28 FA** entries, one per
  declared code, owned by `SupportErrorResourceSet` and registered once by `AddSupportEndpointPresentation`.
- **Validation codes**: `support.validation.*` transport identities only; never catalogued, never
  localized, surfaced through the canonical `validation.failed` envelope.
- **Logging/telemetry/correlation**: canonical (zero `Console.WriteLine`, `Debug.WriteLine`,
  `new ActivitySource(`, `traceparent`).

## 5. Structure and microservice extractability

- `PROFESSIONAL_SHALLOW` capability-first Application (`Tickets/{Commands,Queries,Models,Ports}` +
  `Composition/` + `Validation/`); no technical-axis-first roots, no single-file request leaves.
- `CANONICAL` solution grouping `/Modules/Support/` with all six projects; path↔namespace `EXACT`;
  physical copies `CLEAN`; root allowlists `ENFORCED`; alias workaround `NONE`.
- **Contracts-only boundary**: zero foreign Application / Infrastructure / Domain edge; the single legal
  foreign edge is `Tooba.Notification.Contracts` in `Tooba.Support.Infrastructure`; Endpoints never reach
  Infrastructure or Host; zero cross-module persistence, zero cross-module join, zero `TypeForwardedTo`.
- Own schema `support`; the three migrations are byte-identical to the W0 baseline.

## 6. Preserved Host closure

`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` and `HOST_ROOT_FINAL_CERTIFIED` are preserved:
`lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`, `automaticNextImplementationTask = NONE`,
`hostIllegalAuthorityState = ZERO`.

## 7. Durable guard

`SupportModuleAmsc001W3CertGuardTests` (13 facts) locks the SoT/manifest certification truth with the
chained wave lineage, the 17 module-owned routes with Host route count zero, the exact
route→dispatched-request map re-derived from endpoint source (fail-closed parser), the exhaustive
9+8 matrix and its DI discovery, the canonical result/localization/catalog mechanisms, the Contracts-only
microservice boundary with the unchanged schema, the four-wave evidence tree and the preserved Host
closure.

**Verdict**: `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` `STRUCTURE_CERTIFIED`.
