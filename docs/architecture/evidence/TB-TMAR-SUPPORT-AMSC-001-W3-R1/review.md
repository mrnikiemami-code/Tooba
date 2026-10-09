# TB-TMAR-SUPPORT-AMSC-001-W3-R1 — Bounded Support 201 Response-Contract Review

Wave: `TB-TMAR-SUPPORT-AMSC-001-W3-R1` (`tooba-architecture-certify`,
`Mode = BOUNDED_SUPPORT_201_RESPONSE_CONTRACT_REVIEW`).
Parent: `TB-TMAR-SUPPORT-AMSC-001-W3`.
Channel: `tooba-main`. Starting head: `c632da653d4a34cc6172313a9ef3ef61346e99c0`.

**Verdict: `ARCHITECT_DECISION_REQUIRED`** — no production change, no guard loosening,
no exception self-authorization.

---

## 0. Preflight (STRICT STOP gate)

| Precondition | Result |
| --- | --- |
| Branch | `main` |
| `HEAD` | `c632da653d4a34cc6172313a9ef3ef61346e99c0` |
| `origin/main` | `c632da653d4a34cc6172313a9ef3ef61346e99c0` (`HEAD == origin/main`) |
| Tracked working tree | clean (0 modified/deleted tracked files; only pre-existing untracked evidence artifacts) |
| W0 `567ac400` ancestor | YES (`567ac4004eb464f489695f078e794fe7645716ab`) |
| W1 `5e8c86ef` ancestor | YES (`5e8c86efacb0e4f45ee10a066d6cb869a21c1aec`) |
| W2 `aaa15b03` ancestor | YES (`aaa15b03e5cfa2ed1bef1c8244e0c621c3786980`) |
| W3 `c632da65` ancestor | YES (== `HEAD`) |

Governance read: `AGENTS.md`, `.cursor/skills/tooba-architecture-certify/SKILL.md`
(§8 canonical API result mapping, §0 ownership ≠ quality, Certification Result list),
`docs/architecture/TMAR-architecture-locks.md`,
`docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md`,
`docs/architecture/tmar-current-state.json`, `tmar-module-structure-manifests.json`,
`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`, and the Support W0–W3 evidence.

---

## 1. The exact endpoints under review (verified facts)

Two shipped Support routes return HTTP 201 with a raw DTO body and **no `Location` header**:

| Route | Verb | Handler | Success mapping (on disk) |
| --- | --- | --- | --- |
| `/v1/customer/support/tickets` | POST | `SupportCustomerEndpoints.CreateAsync` | `Results.Json(result.Value, statusCode: StatusCodes.Status201Created)` |
| `/v1/seller/support/tickets` | POST | `SupportSellerEndpoints.CreateAsync` | `Results.Json(result.Value, statusCode: StatusCodes.Status201Created)` |

Source locations:

- `src/backend/Modules/Support/Tooba.Support.Endpoints/Customer/SupportCustomerEndpoints.cs:71-73`
- `src/backend/Modules/Support/Tooba.Support.Endpoints/Seller/SupportSellerEndpoints.cs:55-57`

Verified observable contract of each:

| Aspect | Value |
| --- | --- |
| Status | `201 Created` |
| Body | raw `TicketSnapshotDto` JSON (no `{data,meta}` envelope) |
| `Content-Type` | `application/json` |
| `Location` | **absent** |
| Failure path | `api.From(result)` → catalog ProblemDetails (`support.rejected` 400, `customer.session.required` / `seller.authorization.denied` 401/403, `support.authorization.unavailable` 503) — canonical, unchanged |
| Return type | `Result<TicketSnapshotDto>` (`CreateCustomerTicketCommand` / `CreateSellerTicketCommand`) |

`TicketSnapshotDto` (`Tooba.Support.Application.Tickets.Models.SupportDtos.cs`) carries a
`TicketId` field, so a resource URI is derivable without inventing one.

---

## 2. Canonical factory behavior (`ApiResponseFactory`)

`src/backend/BuildingBlocks/Tooba.BuildingBlocks/Presentation/ApiResponseFactory.cs`:

| Member | Success behavior | Location |
| --- | --- | --- |
| `From(Result)` | `Results.NoContent()` (204) | none |
| `From<T>(Result<T>)` | `Results.Json(result.Value)` (200, raw DTO) | none |
| `Created<T>(string location, Result<T>)` | `Results.Created(location, result.Value)` (201 + raw DTO JSON) | **sets `Location`** |
| `FromFailure(...)` / `FromException(...)` | catalog ProblemDetails | none |

`Created<T>` throws `ArgumentException("created_location_required")` when `location` is blank
(`ApiResponseFactory.cs:74-84`). There is therefore **no existing canonical member** that yields
`201 + raw DTO + no Location`.

Behavioral comparison for the two create paths:

| Candidate | Status | Body | `Content-Type` | `Location` |
| --- | --- | --- | --- | --- |
| Current raw `Results.Json(..., 201)` | 201 | raw DTO | `application/json` | absent |
| `api.Created(location, result)` | 201 | raw DTO | `application/json` | **present (additive delta)** |
| `api.From(result)` | 200 | raw DTO | `application/json` | absent (**status change — forbidden**) |

Conclusion: the canonical `Created<T>` is a **near-identical** mapping — status, body and
`Content-Type` are preserved byte-for-byte — and the **only** wire-visible delta is the additive
`Location` header, which is the documented contract of that factory. Obtaining `201 + raw DTO`
**without** `Location` would require a **new** factory capability, which this task explicitly forbids
implementing.

---

## 3. Consumer / test evidence

| Consumer | Location | Depends on `Location`? | Depends on 201? |
| --- | --- | --- | --- |
| Customer BFF proxy | `src/frontend/app/api/customer/support/tickets/route.ts` | No — forwards only status + `Content-Type`; body passthrough | status passthrough only |
| Seller client | `src/frontend/app/support/support-api.ts` (`createSellerTicket`, `createCustomerTicket`) | No — reads `response.ok` + JSON body only | `response.ok` only |
| Customer client | `src/frontend/app/support/support-api.ts` (`createCustomerTicket`) | No | `response.ok` only |
| Demo e2e evidence | `docs/evidence/TB-P06-T029/07-support-e2e.md`, `commercial-demo.json` | No — records `status: 201` only | records `201` as the observed status |
| Module tests | `Tooba.Support.Tests` | No test asserts `201` or `Location` | — |
| Cert guard | `SupportModuleAmsc001W3CertGuardTests` | Asserts the raw-201 shape exactly (2 matches) | yes |

No in-repo consumer reads the `Location` header on these routes, so adding it is additive and
non-breaking for shipped clients. However, the absence of `Location` is part of the currently
**locked, shipped** shape asserted by the W3 guard, so a change is a wire-visible contract change
that must be an explicit Architect decision — it cannot be "silently" applied (the task forbids
silently altering headers).

---

## 4. In-repo precedent and exception-authority audit

| Precedent | State | Explicit exception lock? |
| --- | --- | --- |
| Identity register `201` raw DTO (`IdentityAuthEndpoints.cs:58-60`) | certified as "intentional locked 201 register DTO" | **No explicit lock** — it is a certification note, not an architecture lock |
| Reviews submit review `201` raw anonymous DTO | present, not certified under AMSC | **No explicit lock** |
| ProductWorkspace W3-R1 | raw `201` **repaired** to `api.Created` | Architect approved the additive `Location` delta explicitly |
| Promotion W1 | raw `201` **repaired** to `api.Created` | same pattern |
| Content R2 / Offer / AddressBook | raw `201` **repaired** to `api.Created` | same pattern |

Searches for an explicit raw-`Results.Json` success-payload exception policy/registry in
`TMAR-architecture-locks.md`, `TOOBA-LOCKS.md`, the certify/analyze/migrate/structure skills and the
SoT/manifest returned **no explicit lock** that exempts a raw-201 success mapping. The certify skill
requires such a lock ("unless a canonical architecture lock explicitly exempts that exact quality
concern"), and states that an ownership exception is not a quality exception.

The task's own instruction applies directly: **"Do not assume that an Identity precedent is
automatically an architectural exemption"** and **"Precedent alone does not override the certify
rule."** So the Identity/Reviews precedents are evidence of a *pattern*, not a grant of authority.

---

## 5. Risk table

| # | Risk | Assessment |
| --- | --- | --- |
| R1 | Adding `Location` breaks a shipped consumer | LOW — no in-repo consumer reads `Location` on these routes; BFF forwards only status + `Content-Type` |
| R2 | Adding `Location` breaks the W3 cert guard | KNOWN — the guard asserts exactly 2 raw-201 matches; a repair must update the guard deliberately (as ProductWorkspace W3-R1 did), never weaken it |
| R3 | Changing 201 → 200/204 to use `api.From` | FORBIDDEN — status change; `api.From` returns 200, `From(Result)` returns 204 |
| R4 | Inventing a `Location` URI not backed by the returned DTO | AVOIDABLE — `TicketSnapshotDto.TicketId` is returned, so the URI is derivable from the existing route + id |
| R5 | Keeping raw 201 without explicit authority | BLOCKER — certify §8 forbids `RAW_RESULTS`/non-canonical API mapping absent an explicit lock; no lock exists |
| R6 | Adding a new `ApiResponseFactory` capability in this task | FORBIDDEN by the task's ALLOWED CHANGES (no shared factory edits, no new framework capability) |
| R7 | Behavioral regression in the failure path | NONE — the failure path already uses `api.From(result)` and is unchanged |

No evidence is missing and behavioral equivalence is fully characterized (not uncertain): the delta
is precisely the additive `Location` header. The unresolved question is **authority**, not facts.

---

## 6. Decision

The raw 201 is **not** a *necessary* compatibility exception — an existing canonical member
(`ApiResponseFactory.Created<T>`) reproduces the status, body and `Content-Type` exactly. But that
member also adds a `Location` header, and no canonical member yields `201 + raw DTO` without a
`Location`. Migrating therefore **does** alter the HTTP contract (additively), which the objective
explicitly scopes out ("without altering the HTTP contract") and which the task forbids doing
silently. Symmetrically, keeping the raw 201 requires an explicit exception that does not exist in
the repository, and the task forbids self-authorizing one.

Both paths therefore require an Architect decision:

- **Option A (canonical migration, recommended precedent):** replace both mappings with
  `api.Created($"/v1/customer/support/tickets/{result.Value.TicketId}", result)` and
  `api.Created($"/v1/seller/support/tickets/{result.Value.TicketId}", result)`; status 201, raw DTO
  body and `Content-Type` are preserved exactly; the additive `Location` header is the documented
  contract of `ApiResponseFactory.Created` (identical in shape to the Architect-approved
  ProductWorkspace W3-R1 / Promotion W1 / Content R2 repairs). Requires a deliberate cert-guard
  update (assert `api.Created(` on exactly the two create paths and `Results.Json`/`Status201Created`
  absent), never a weakening.
- **Option B (explicit narrow exception):** record an explicit canonical lock permitting raw
  `Results.Json(..., 201)` for exactly these two successful create responses (same class as the
  Identity register DTO), with negative constraints (no other route, no failure path, no envelope
  change) and durable guard evidence. This path requires the Architect to author the lock; it must
  not be self-authorized.

Exact minimal proposed repair (Option A) and test plan are recorded in `proposed-repair.md` so the
approved path can be implemented as a bounded, single-purpose follow-up task.

---

## 7. Preservation proof (this wave)

| Item | State |
| --- | --- |
| Production `.cs` files changed | **0** |
| Shared `ApiResponseFactory` edits | **0** |
| Schema / migration / endpoint / contract changes | **0** |
| Tests weakened / baselines widened / global pins changed | **0** |
| Support 17-route / 9-validator + 8-exemption proof | intact (re-verified) |
| Manifest structural state | NOT_TOUCHED |
| Global Host checkpoint | `HOST_ROOT_FINAL_CERTIFIED` / `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001` PRESERVED |
| `automaticNextImplementationTask` | `NONE` |
| Workflow stop | `USER_REVIEW_SUPPORT_AMSC_001_W3_R1` |

Support AMSC W3 certification truth (`supportAmsc001W3`, `structureCertified: true`,
`SUPPORT_AMSC_001_CERTIFIED`) is **not** altered by this review: this wave records a blocker/decision
request only and does not mark final certification ACCEPTED.

---

## 8. Focused validation (exact counts)

| Command | Result |
| --- | --- |
| `dotnet test ...Tooba.Host.Tests.csproj --filter "FullyQualifiedName~SupportModuleAmsc001"` | **30 passed / 0 failed / 0 skipped** (W1 9 + W2 8 + W3 13) |
| `dotnet test ... --filter "FullyQualifiedName~TmarCompleteReferenceStructureGateTests|FullyQualifiedName~TmarDurableGuardTests"` | **7 passed / 3 failed / 0 skipped** (10 total) |

The 3 failures are the **pre-existing, unrelated** repository-global pins recorded by the Support W3
baseline and by the preceding certified modules (Returns/Promotion/Pricing) — not regressions:

| Failing guard | Cause | Relation to this task |
| --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | pre-existing `Tooba.Catalog.Contracts.Cart` namespace deviation (documented in the gate source as out of scope for a module-local wave) | NONE |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | stale repository-global `certifiedModules` literal pin | NONE |
| `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` | same stale repository-global recovery pin | NONE |

Baseline grounding: identical failure names and counts were recorded by the immediately preceding
Support W3 wave and by the Returns/Promotion/Pricing AMSC waves; this review changes no production
file and no SoT field, so the set is unchanged. No result is invented.

---

## 9. What remains unknown

- Which of Option A / Option B the Architect authorizes. The facts, the exact delta and the
  preservation constraints are fully determined; only the authority choice is open.
- Whether the Architect wants the same decision extended to the other in-repo raw-201 success
  mappings (Identity register, Reviews submit). Out of scope here: this task is bounded to Support's
  two create responses and does not reopen unrelated modules.

**Stop:** `USER_REVIEW_SUPPORT_AMSC_001_W3_R1`.
