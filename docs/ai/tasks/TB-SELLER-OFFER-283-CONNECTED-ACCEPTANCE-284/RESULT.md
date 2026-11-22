# RESULT — TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284

Status: **BLOCKED**
Readiness: **NOT_READY**
Blocking cause: **TASK_SCHEMA_GATE_UNEXECUTABLE**
Worker self-review only; not Architect ACCEPT or owner acceptance.

## Outcome

The two missing TB-283 proofs were executed on one connected chain: real Chromium -> served isolated Next page -> real owning Host -> isolated real PostgreSQL. No production source was changed.

| Original requirement | Connected result | Evidence |
|---|---|---|
| REQ-01 SKU-only save preserves two positions | PASS. Real page saved only SKU. Exactly one Offer PATCH; zero price writes; zero inventory writes. Independent GET and SQL show new SKU, same 2,500,000 price, positions 3 and 7, reservations 1 and 2, and unchanged inventory outbox count. | `evidence/connected-284.json` `REQ-01` checks |
| REQ-03 concurrent price and stale SKU | PASS. B loaded the old price. A saved 2,750,000 through `/v1/seller/offers/{id}/price`. B then saved SKU only from its stale form. Independent GET and SQL show 2,750,000 and the new SKU together; B emitted zero price writes; positions and reservations stayed unchanged. | `evidence/connected-284.json` `REQ-03` checks |
| REQ-02 Draft and both-limit clear | PASS connected. Draft remained Draft; both limits became null after explicit clear. | `evidence/connected-284.json` `REQ-02` checks |
| REQ-04 named position and negatives | PASS connected. Second position 7 -> 8, first stayed 3, total 11. Ambiguous, foreign and below-reserved attempts were typed refusals with zero change. | `evidence/connected-284.json` `REQ-04` checks |
| REQ-05 wrong and lost response | PASS connected browser branch. Wrong identity showed no success and wrote nothing. A lost response followed a real write; independent GET showed the new SKU and no automatic second write occurred. | `evidence/connected-284.json` `REQ-05` checks |
| REQ-06 cancel and mobile 360x640 | PASS connected browser. Dirty cancel sent zero writes and reopening showed the old SKU. At 360x640 document overflow was 0 and Save, Cancel and a position were reachable. | `evidence/connected-284.json` `REQ-06` checks |
| REQ-07 no-change and independent sections | PASS connected browser. No-change Save sent zero writes. A forced first-section refusal kept the price draft; after the next Save both SKU and price persisted. | `evidence/connected-284.json` `REQ-07` checks |

Connected run summary: **7/7 scenarios PASS, 38/38 checks PASS**. The browser proof is `evidence/connected-284.json`; the console log is `evidence/connected-284.log`.

## Connected environment

- Disposable PostgreSQL 17.10 database `tooba_offer283_connected` inside the existing `postgres-db` container on 127.0.0.1:5432.
- Real Host from `artifacts/publish-primary/Tooba.Host.dll`, PID 40848, `127.0.0.1:5090`, Development, tenant `store-alpha` connection overridden to the disposable database, messaging disabled.
- Served isolated Next production build on `127.0.0.1:3100`, BUILD ID `oWTA29FFSAnndeeVyDFsx`, `TOOBA_HOST_ORIGIN=http://127.0.0.1:5090`.
- Real Chromium from Playwright 1.65.1, installed browser, no mocked owning backend for success paths.
- Legitimate Development seller actor `01a2041e-bca5-7000-87e7-f621a913c987` for seller party `01a2041e-bb65-7000-9b34-1d049b3a788a`.
- Fixture offer `01a2041e-bb6b-7000-9163-d736d71634d5`, positions `01a2041e-bbe0-7000-9897-006e14fe11c0` and `01a2041e-bbd3-7000-9fe5-0e7d2157003e`, price 2,500,000 IRR.
- Cleanup verified: Host 5090 and Next 3100 stopped, disposable database dropped, ports 5090 and 3100 no longer listening; canonical Host 5088 and Next 3000 returned 200.

## Structural gate blocker

The issued `TASK.md` has no markdown REQ table. Both gates use `TASK.md` table rows shaped `| REQ-... |` to derive the issued requirement scope. The task names CONNECTED-01/CONNECTED-03 in prose and relies on original 283 REQ-01..REQ-07, but the gate sees no issued IDs.

- Plan gate: `evidence/plan-gate.log`, exit 1, `TASK needs unique REQ table rows`, `requirement scope differs from issued TASK`.
- Review gate: `evidence/review-gate.log`, exit 1, same root errors plus every REQ reported as unissued.
- TASK bytes were not edited. The issued SHA remains `f658d3da7745d0c3297c17c6c713f371dc2fdcc72c9bde156a7388192f3f2a5c`.
- Because review gate cannot PASS, no `PROPOSED-ARCHITECT-DELIVERY-METHOD.md` candidate was created and no governance file was changed.

Smallest safe unblock: architect reissues this task with a byte-exact REQ table for the authoritative original 283 REQ-01..REQ-07, or explicitly authorizes the gate scope against the original 283 task bytes and records that waiver. No worker edit can satisfy the current task hash and the current gate simultaneously.

## Verification

- `node .../evidence/connected-284.cjs`: exit 0, 7/7 scenarios, 38/38 checks.
- OpenSpec 2.1.1 strict: `Change 'seller-offer-safe-edit-283' is valid`, exit 0.
- `npm run test:seller`: 12/12 pass.
- `npx tsc --noEmit`: 0.
- `next build` in isolated copy: 0, BUILD ID `oWTA29FFSAnndeeVyDFsx`.
- `dotnet build src/backend/Tooba.slnx -c Release`: 0 errors, 717 pre-existing warnings.
- `dotnet test` focused 283 relational filters: 2/2 pass.
- Plan gate and review gate: exit 1 for the task-schema cause above.
- Code SHA unchanged: `611f97a85ec7c2e464e4f649820992202ca3754c`.
- Plan commit: `78868597554dc920bc6a11b1d9b6ef96ba07d069`.
- Evidence-Commit: final docs/evidence commit; see BRIDGE envelope.

## Safety

No production source change, migration, DDL, grant, seed, reset, live business write, provider call, payment, order or notification. The only writes were to the disposable database and were removed with it. Canonical `tooba_alpha`, Host 5088, Next 3000 and user data were preserved. No screenshot was taken. The REQ-05 error branches used controlled response substitution; its success paths used the real owning backend.

## Remaining

Only the issued-task structural gate is open. The connected REQ-01 and REQ-03 proofs are no longer open. `STOP_FOR_ARCHITECT`.
