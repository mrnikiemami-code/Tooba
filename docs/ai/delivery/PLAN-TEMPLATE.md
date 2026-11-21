# ARCHITECTURE-PLAN — <Task-ID>

Protocol: TOOBA-DELIVERY-001
Base-SHA: <actual>
Task-SHA256: <issued exact bytes>
Prepared-before-production-edit: <commit/reference/UTC timestamp; no retroactive claim>

## Outcome and fixed requirements

| REQ | Actor / entry | Trigger / expected effect | Owner and source | Invariants | Acceptance scenarios |
|---|---|---|---|---|---|
| REQ-01 | <New/Edit/list/detail/role> | <observable before/after> | <actual method/path> | <identity/version/media/data> | <scenario IDs> |

## Architecture and real route

<UI state -> transport/BFF -> endpoint/auth -> validator/CQRS -> domain/persistence -> DTO -> mapper/verifier -> UI -> independent reread>
<Use actual source methods and routes. Name optional presentation vs mandatory validation. Reuse existing DS/contracts/registry.>

## Contract and lifecycle

<Request+response fields, authoritative refusal vs uncertainty, completion receipt, storage/replay, request-owned mutex, epoch/context boundaries, valid transitions, permissions and locale authority.>

## Scenario matrix

| ID | Requirement | Entry/role/provider | Preconditions and action | Expected result + side effects | Proof level | Before-fix observation |
|---|---|---|---|---|---|---|
| S-01 | REQ-01 | <actual> | <repro> | <effect + independent read> | <actual required type> | <FAIL reason; or justified N/A before work> |

## Internal waves, same task

| Wave | Depends on | REQs/scenarios | Small connected change | Focused checks | Exit result |
|---|---|---|---|---|---|
| W1 | none | REQ-01 / S-01 | <one observable behaviour> | <specific> | <measurable> |

Do not replace architecture planning with file-count TODOs. No new permission flow for ordinary decisions. Record real blockers and continue independent work.

## Controlled data, regression and served handoff

<Existing harness/provider, rollback/committedRows, forbidden writes, exact test commands, affected shared consumers, final build/served identity, outstanding risks.>

## Checkpoint log

<Wave -> reproduced failure -> fix -> final green -> diff/self-review -> remaining work. Internal self-review is not independent Architect ACCEPT.>
