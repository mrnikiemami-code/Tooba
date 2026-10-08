# Persisted claim artifact — TB-TMAR-PROMOTION-AMSC-001-W3-R1

Persisted claim artifact — full Architect body received via Bridge `GET /api/tasks/next?channelId=tooba-main` at claim row `9c6ddefb-632f-418a-aa15-85ef62402a03` (createdAtUtc 2026-10-08T16:13:48.2519267Z).

- Parent-Task: `TB-TMAR-PROMOTION-AMSC-001-W3`
- Skill: `tooba-architecture-certify`
- Mode: `INDEPENDENT_CERTIFICATION_EVIDENCE_AUDIT_ONLY`
- Starting HEAD (required): `6bf747455b0ab0d8468595ecf42fb73a4d15a52b` (authoritative remote `main`)
- Channel: tooba-main, WorkerId: tooba-worker-01, AgentType: cursor

## Scope

Bounded independent audit of the prior W3 PASS for `Modules/Promotion`. Verify the real Production tree,
actual request → route → validator relationships, dependency edges and focused test evidence; report
concrete gaps with file/line evidence. No production modification and no premature SoT/manifest
recertification.

## Verify-from-disk checklist

- Enumerate exactly 21 route registrations; show verb/path/request/send mapping; reject duplicate routes,
  non-`ISender` dispatch or unexpected Host-owned Promotion routes.
- Enumerate all endpoint-reachable `IRequest` types and handlers; construct a durable 21-row
  route/request/handler/validator table. Confirm the 18/3 split only if supported by each request's real
  transport inputs and pipeline registration; check optional inputs independently; verify validator
  registration and stable code use; check business validation stays out of FluentValidation.
- Independently enumerate each Production project and all `ProjectReference` edges; check source-level
  foreign `Application`/`Infrastructure`/`Domain` references for all Tooba modules; foreign
  persistence/`DbContext`/`DbSet`, SQL cross-schema joins, foreign contract types in public contract
  signatures, cross-module transaction assumptions.
- Verify 40 canonical Promotion error identities; 13 HTTP descriptors registered once; 27 domain
  invariants; bilingual coverage; no message-based classification or ad-hoc endpoint response mapping;
  `ApiResponseFactory.Created` preserves 201 Location/DTO semantics.
- Independently check physical folder depth and granularity, all 6 on-disk projects and `.slnx` grouping,
  exact path ↔ namespace, root allowlists, copies, file cohesion, structure manifest membership, Host
  final closure.
- Verify Promotion schema/migrations unchanged against W0; classify `ARCHITECTURAL_EXTRACTABILITY` vs
  `RUNTIME_DEPLOYABILITY_NOT_PROVEN`.
- Compare recorded full Host failure-name lists for the clean W2 baseline and W3 evidence by exact
  identity, not totals.
- Audit existing Promotion W3 guard blind spots (Endpoints omission in generic source enumeration; regex
  count vs actual complete request mapping).

## Decision gates

- All findings substantiate W3 and no blocking gap: `AUDIT_PASS` (no Production/SoT/manifest change).
- Architectural violation, incomplete guard/evidence proof, invalid 18+3 classification, or direct
  mandatory-cert blocker: `CERTIFICATION_REVIEW_BLOCKED`; record minimal reproduction, implicated lines,
  narrowly scoped next repair; STOP; do not implement a fix in this audit task.
- No violation but test infrastructure prevents proof: `EVIDENCE_INSUFFICIENT`; STOP.
- Bounded effort ~10-12 minutes (hard stop 15); otherwise report `INCOMPLETE`.

## Allowed

Read/inspect sources and evidence; focused commands/test runs. Add task file exactly at
`docs/ai/tasks/TB-TMAR-PROMOTION-AMSC-001-W3-R1.task.md` and audit evidence under
`docs/architecture/evidence/TB-TMAR-PROMOTION-AMSC-001-W3-R1/`. Commit and push only the exact task +
audit evidence if safe; no code/guard/SoT/manifest changes.

## Forbidden

Production changes, test/guard modification or weakening, baseline changes, schema/migrations, new
projects, structure changes, changing certification truth, unrelated modules, moving Host files,
frontend, next task/module, speculative repairs, force push.

## Outcome

Verdict `CERTIFICATION_REVIEW_BLOCKED`. Blocking finding A: `ListMerchandisingCampaignTypesQuery` binds a
client-supplied `locale` and is therefore `VALIDATOR_REQUIRED`, not `NO_VALIDATOR_REQUIRED`; the true
matrix is 19 + 2, not 18 + 3. Non-blocking findings B (undocumented W1→W3 reclassification), C (guard
uses a hardcoded `+ 3`), D (W3 guard omits Endpoints in generic source enumeration). All other gates
verified independently: routes 21/21 distinct/`ISender`-only, Contracts-only boundary, 40-code catalog
with 13 HTTP descriptors and 40/40 bilingual, `ARCH-COMPLETE-002` structure, migrations unchanged,
Host checkpoint preserved, Host failure name set byte-identical to the W2 baseline.

## Result contract

Per Bridge Task body — `BEGIN_TOOBA_WORKER_RESULT … END_TOOBA_WORKER_RESULT` posted to `POST /api/results`
only after validate + commit + push + `HEAD == origin/main`, then complete the Bridge task lifecycle and
stop.

## Stop rule

Do not issue another task, automatically accept W3, or start a new module. Await Architect review of the
evidence and final SHA.

END_TOOBA_TASK
