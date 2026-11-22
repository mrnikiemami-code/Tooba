# Proposed OpenCode delivery method

Status: **CANDIDATE — NOT ADOPTED**
Prepared: 2026-11-22
Evidence basis: TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283 and TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284
Authority: none until explicit architect and user approval of a separate governance action.

## Purpose

Give OpenCode a deterministic, evidence-first task lifecycle that is independent of Cursor's REQ-table gate without weakening Cursor governance or inventing a second product truth. Cursor remains byte-exact and untouched. OpenCode maintains its own contract and checker.

## A. Architect-owned input contract

Before issue, the architect records:

- exact source base and allowed paths;
- fixed REQ IDs with one observable customer/operator outcome per REQ;
- entry points, domain ownership, response authority and data boundary;
- known risks and explicit out-of-scope items;
- expected served build, provider and test-chain needs;
- the required stop state and whether a candidate governance document is allowed only after PASS.

The task must be schema-checkable before dispatch. An OpenCode task without fixed REQ rows is invalid and must not be issued.

## B. Mandatory preflight before code

The worker must actually prove the test chain before production edits:

- real git remote/branch/ancestry and no user-work overwrite;
- installed browser, served UI, real Host/build, legitimate actor, isolated database, fixture creation, independent read and cleanup;
- exact commands, ports, provider, fixture IDs and failure behavior;
- a no-write or disposable write boundary with proof;
- `BLOCKED_PREFLIGHT` and one concrete cause if the chain cannot be made safe.

Intentions, `/health` alone, mocked HTTP and synthetic fixtures are not preflight proof.

## C. Design gate before code

- OpenSpec change exists and passes strict validation before production edit.
- REQ and Given/When/Then scenarios are locked before code.
- For actual UI changes, Impeccable must be used as a documented critique → design decision → implementation → post-check. Installing or reading it is not evidence of use.
- A no-change UI acceptable patch may skip Impeccable only when the task explicitly says no design change is required.

## D. Connected implementation and debug

- Use the actual owning Host/backend/provider for persistence claims.
- Use real Playwright Chromium on the served page.
- Use an isolated database and reversible cleanup; no live business writes.
- Debug and fix setup problems before the first complete acceptance matrix.
- After the first complete matrix, allow at most one consolidated repair round and one final full rerun.
- Stop as PASS/READY or BLOCKED. Do not publish a cosmetic PARTIAL or a new speculative task.

## E. Evidence contract

Every mandatory REQ records:

- assertion name and observed value;
- HTTP method/path/body count and identity;
- independent GET/SQL/reread;
- source SHA, served build/host identity, database identity;
- provider, fixture and cleanup;
- before/after state where persistence is claimed;
- negative controls for refusal/forgery/uncertainty.

Evidence must be a repository-relative file with a real SHA-256. Self-reported Markdown cannot replace the evidence file. A separate checker must validate schema, hashes, assertions and facts, and must include negative self-tests for tampered evidence, a deleted REQ and a changed expected fact.

## F. OpenCode acceptance boundary

- Worker PASS is not Architect ACCEPT.
- Architect ACCEPT is not owner acceptance.
- OpenCode PASS must not relabel a Cursor structural failure as product PASS.
- Cursor gate failures remain historical truth.
- The architect performs independent review and issues the decision.
- Always stop at `STOP_FOR_ARCHITECT`; no automatic next task.

## G. Scope discipline

- Preserve exact issued REQs.
- Do not create follow-on features or a roadmap change inside an acceptance task.
- Unrelated findings go to a non-blocking backlog.
- A failed task must not publish the candidate method as adopted policy.
- A candidate may become canonical only by a separate explicit governance revision after PASS and independent acceptance.

## H. Deterministic decision table

| Check | Required |
|---|---|
| Task is issued by architect | Yes |
| Exact REQ IDs and observable outcomes | Yes |
| Preflight chain proved before code | Yes |
| OpenSpec strict before code | Yes |
| UI critique workflow for UI change | Yes |
| Real browser plus owning backend for persistence | Yes |
| At least one independent GET/SQL/reread per persistence claim | Yes |
| Negative controls for error/refusal/forgery | Yes |
| Evidence hashes and source/build identities | Yes |
| Checker self-test rejects tampering | Yes |
| Cursor artifacts byte-exact | Yes |
| Worker/Architect/owner distinctions explicit | Yes |
| Candidate marked noncanonical until separate approval | Yes |

## I. Lessons from 283/284

1. A task can be functionally complete while a format-specific gate rejects its artifact. Keep the gate failure and add a separate schema-checked acceptance path; do not rewrite the failure.
2. The most valuable proof is one connected chain: served UI → owning Host → isolated database → independent read. Splitting the evidence loses causality.
3. A deliberately disposable database is safer than conditional writes to a live database.
4. Evidence summaries can contain small factual drift; the checker must read the primary JSON and source, not only prose.
5. A deterministic checker with negative self-tests is not a substitute for independent review, but it prevents obvious unsupported claims.

## Adoption

This document is **CANDIDATE — NOT ADOPTED**. It must not be copied into `AGENTS.md`, `docs/architecture/TOOBA-DELIVERY-ASSURANCE.md`, the pipeline or any canonical roadmap. A later governance-only action may promote it only after the architect/user explicitly accept this task and preserve Cursor governance.
