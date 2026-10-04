# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Manifest Structure

## 1. Current manifest entry

`docs/architecture/tmar-module-structure-manifests.json` (AddressBook entry begins at line 1401):

```json
{
  "module": "AddressBook",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002",
  "certificationNote": "Certified by TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001; physical folder
    layout then realigned to the Offer COMPLETE_REFERENCE_PATTERN capability layout (Contracts Dtos/Ports/Errors,
    Domain Aggregates, Application Commands/Queries/Ports/Models/Validators, Infrastructure
    Adapters/DependencyInjection/Outbox/Persistence). Host residue ZERO, HTTP ownership ZERO, 6 module-owned
    routes, validator coverage 5/5 + 1 NO_VALIDATOR_REQUIRED, path<->namespace EXACT, cross-module boundary
    Order.Contracts-only. TB-TMAR-ADDRESSBOOK-POST-REALIGN-REPAIR-001 then attached AddressBook to the canonical
    presentation stack (Errors contributor + Resources resx + IErrorResourceSet, Goals: API-Result-Pattern-State
    CANONICAL, Parallel-Problem-Pipeline ZERO) and grouped the five projects under one /Modules/AddressBook/
    Solution Folder; Offer kept as REFERENCE_NOT_CLONE.",
  "projects": [ … 5 entries … ]
}
```

| Check | Result |
| --- | --- |
| Exactly one certified entry for AddressBook | ✅ (`"module": "AddressBook"` appears once) |
| `structureCertified: true` | ✅ |
| `lockVersion: ARCH-COMPLETE-002` | ✅ |
| One `projects[]` entry per production project | ✅ 5 |
| Solution grouping `/Modules/AddressBook/` matches disk | ✅ |
| Duplicate AddressBook entry in `preCertModules` | ✅ none (`preCertModules` holds only `ProductWorkspace`) |

## 2. Certification-note accuracy audit

| Claim in the note | Verified against disk |
| --- | --- |
| Contracts `Dtos/Ports/Errors` | ✅ |
| Domain `Aggregates` | ✅ |
| Application `Commands/Queries/Ports/Models/Validators` | ✅ present — **but** the note presents them as the canonical *capability* layout, which is the drift (see §3) |
| Infrastructure `Adapters/DependencyInjection/Outbox/Persistence` | ✅ |
| Host residue ZERO | ✅ |
| HTTP ownership ZERO | ✅ |
| 6 module-owned routes | ✅ |
| Validator coverage 5/5 + 1 `NO_VALIDATOR_REQUIRED` | ✅ |
| path↔namespace EXACT | ✅ |
| cross-module boundary `Order.Contracts`-only | ✅ |
| **"Goals: API-Result-Pattern-State CANONICAL"** | ❌ **NOT satisfied** — see §4 |
| **"Parallel-Problem-Pipeline ZERO"** | ✅ satisfied (no parallel pipeline exists; the gap is incomplete adoption, not a parallel one) |

## 3. Drift D1 — Application shape described as canonical

`projects[].rootAllowlistJustification` for `Tooba.AddressBook.Application` (line 1425) states:

> `"AddressBook.Application intentionally has no root .cs file: Commands/<UseCase>, Queries/<UseCase>, Models, Ports and the shared Validators capability carry every type, and namespace alignment is exact path-derived equality."`

This records the `TECHNICAL_AXIS_FIRST` tree (with 11 single-file use-case leaves) as the accepted
structure. It must be replaced in W2 with the flattened capability-first justification, and the
durable guard must be updated honestly (adding `Composition` and the capability folder).

## 4. Drift D2 — `API-Result-Pattern-State: CANONICAL` is false

The `certificationNote` claims `API-Result-Pattern-State CANONICAL`. Current disk state is `AD_HOC`
(see `canonical-pattern-gaps.md` §2): all six handler return types are raw DTO/`Unit` and all six
success paths use raw `Results.*`.

The pre-cert repair that produced that claim (`TB-TMAR-ADDRESSBOOK-POST-REALIGN-REPAIR-001`)
attached the contributor + resource set and used `api.FromFailure` for the two guard paths — a real
improvement, but it did not convert the success paths. The claim therefore overstates the achieved
state, and the existing durable guard only asserts the presence of `ApiResponseFactory` /
`FromFailure` / `SemanticError` plus the *absence* of `Results.Problem` — it never asserts that
success paths use the factory.

`AddressBookCanonicalPresentationGuardTests.Endpoints_use_the_canonical_presentation_stack_not_a_parallel_problem_pipeline`
must be strengthened in W3 to assert `api.From(` / `api.Created(` and the absence of raw
`Results.Json(` in endpoint success paths.

## 5. Required manifest changes (W2/W3)

| Change | Wave |
| --- | --- |
| Replace the `Tooba.AddressBook.Application` justification with the flattened capability-first description | W2 |
| Add `Composition/` to the Application allowlist set (durable guard + manifest) | W2 |
| Keep `rootAllowlist` empty for Application (no root `.cs`) | W2 |
| Refresh `certificationNote` to the accurate post-W1/W2 state | W3 |
| Confirm exactly one certified entry, no `preCertModules` duplicate | W3 |

## 6. `preCertModules` observation (related to F8)

`preCertModules` (line 1555) still contains `ProductWorkspace` with `structureCertified: false`.
That is correct and unrelated to AddressBook. The stale
`AddressBookValidatorCoverageGuardTests.AddressBook_is_certified_with_exactly_one_manifest_entry`
assertion forbids the property's existence at all and is therefore RED at this HEAD; it must be
repaired to its documented intent (no AddressBook duplicate in `preCertModules`) in W1/W3.
