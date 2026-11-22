# Independent evidence review — TB-OCC-284

Decision: **PASS for OpenCode acceptance**
Readiness: **READY_FOR_ARCHITECT_REVIEW**
Scope: evidence and OpenCode acceptance only. The separate Cursor structural gate remains a known format incompatibility. This is not Architect ACCEPT or owner acceptance.

## Source authentication

The task was read from `origin/main` after `git fetch origin`; HEAD and origin/main were reconciled at `844c23237d19aeabc453f5fb04561657c4dd967a`. The expected base `f006fbdbdbfd4911ea439fe9a3443438af6cf786` is an ancestor. `fc40d412aa6d085d3db3f1deac461eb93a10f48d` is the recorded source-evidence commit and `611f97a85ec7c2e464e4f649820992202ca3754c` is the reviewed product code.

I recomputed the following SHA-256 values from the current working tree and, for 284 evidence, from the Git object bytes at the source-evidence commit. They match.

| Artifact | SHA-256 |
|---|---|
| 284 TASK.md | `f658d3da7745d0c3297c17c6c713f371dc2fdcc72c9bde156a7388192f3f2a5c` |
| 284 ARCHITECTURE-PLAN.md | `de9cbd3358b70258f2923359732a700cbec8bfa4f0e9911e02196694e8b72654` |
| 284 DELIVERY.json | `3ffc758338f1c9ff9637ecf53f787bf89a129da792952a32141d518d7d37b7d2` |
| 284 RESULT.md | `0ff8b8c3049caa7267b1291fb962c019dd67b3851aa81c3ead40ad0619037505` |
| 284 RECEIPTS.md | `08747548b30f0573922b9bce619a861e577d637bc3b9f55fd34797ad444ac672` |
| connected-284.cjs | `ebf1f993594684a08bed26d8be9aab87420d2522465cf3f4434597f5a535dc36` |
| connected-284.json | `0a155bcc2fa5396b8d981e3246036523f70800a3bdf2e02db5c08fb89528afc1` |
| connected-284.log | `29454955b7d09a3999d18a0bae42a4c4ba35b38c82eb66c887e32cfb595acf3a` |
| plan-gate.log | `a54d062d9f53df4254e5399297eae27e6afbcc3ab5051ae0adb6a0815eda029d` |
| review-gate.log | `c64c231f70e1a63a622a337a2dbaeb2047b47e86112e0ba06ff9f2d1961a4fc8` |
| OpenSpec strict log | `84a220d56bbb1540b9f95c898932fe9b6f675b55480da9b1fbc36d6e1f62f77e` |
| Playwright identity | `5915408c216027d1929d56137c629767c6b0db4a0266cb70102283810febadcf` |

`git diff 611f97a85ec7c2e464e4f649820992202ca3754c..HEAD -- src/backend src/frontend` is empty. `git diff 844c23237d19aeabc453f5fb04561657c4dd967a..HEAD` contains only this OpenCode task folder. The legacy Cursor paths and the frozen 283/284 task paths have no post-844 change. The 844 task-issuance commit changed only `docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/TASK.md`; the earlier architect review is a pre-existing base record and is reported, not rewritten. Cursor's gate failure remains in the original gate logs.

## Harness review

`connected-284.cjs` is a real browser harness, not a mock-only view:

- It launches installed Chromium through Playwright 1.65.1.
- It opens the isolated served page on 3100; `next.config.ts` rewrites `/v1` to `TOOBA_HOST_ORIGIN` 5090.
- The Host is the real `Tooba.Host.dll` with the `tenant-alpha` connection reference overridden to the disposable database `tooba_offer283_connected`. Messaging is disabled.
- The seller actor and party are the real Development pair returned by `/v1/seller/dev-contexts`; the browser sends the exact actor and seller party headers.
- Success paths call the owning Host endpoints. Independent reads use a direct Host GET with the same seller headers, not React state.
- State checks use `docker exec postgres-db psql` against that same disposable database. The JSON records offer, seller, positions, reservations, price, outbox and SQL values.
- The connected JSON has exactly 7 REQ scenarios and 38 concrete assertions. The checker validates each assertion name and the specific facts, not the top-level `7/7` claim alone.

Source cross-check supports the recorded route: `OfferSellerEndpoints.cs` maps `/price` and `/inventory`; `InventoryDirectory.SellerWrite.cs` resolves an explicit `StockItemId`, refuses multi-position callers without one, rejects unknown positions and below-reserved values; `UpdateOfferCommand.cs` accepts Draft as a no-op and clears both limits; `seller-api.ts` checks offer/seller/effect identity; `page.tsx` sends only changed sections and uses `reconcileVersion` for an independent read after an uncertain result.

## Requirement review

| REQ | Independent finding |
|---|---|
| REQ-01 | PASS. One PATCH to the exact offer, body is SKU-only commercial data. No price or inventory mutation. Independent GET and SQL show ARM-LN-01-C01, price 2,500,000, positions 3/7, reservations 1/2 and outbox 27 → 27. |
| REQ-02 | PASS. Draft stays Draft; SKU persists; both limits become null after explicit clear. |
| REQ-03 | PASS. B opened before A and saw the old 2,500,000. A performed one owning price write of 2,750,000. B then performed one commercial PATCH and no price write. GET/SQL show 2,750,000 and ARM-LN-01-C03 together; stock/reservations unchanged. |
| REQ-04 | PASS with one recorded wording discrepancy, below. Named position 7 changed to 8, first stayed 3, total 11. The real Host returned 400 for ambiguous, 409 for foreign, 409 for below-reserved; all were typed refusals with zero state change. |
| REQ-05 | PASS for the tested branches. Wrong-identity response showed no success and wrote nothing. A response loss followed a real write, an independent GET showed the new SKU, and no automatic second write occurred. These are controlled error branches, not live-provider proof. |
| REQ-06 | PASS. 360x640, innerWidth 360 = scrollWidth 360, overflow 0. Save, Cancel and a position were present. Dirty Cancel emitted zero writes; reopening in a new page showed ARM-LN-01. Cancel navigates to the product list; reopening is a new page, not the same-page reload. |
| REQ-07 | PASS. No-change Save emitted zero writes. A forced first-section 409 retained the 2,800,000 price draft; the subsequent unforced Save persisted SKU ARM-LN-01-C07 and 2,800,000. |

## Discrepancy and limitations

1. **REQ-04 prose mismatch.** `284/RECEIPTS.md` says "foreign 400"; the connected JSON records 409 `inventory.position.id_unknown`. The requirement only requires a typed refusal with zero change and the code returns 409. The JSON is the primary evidence. The manifest and checker use the actual 400/409/409 values. This is a documentation correction, not a product failure.
2. **No runtime rerun in this task.** The original isolated database and processes were disposed. This task is no-write acceptance and was not authorized to recreate them. The checker validates recorded bytes and facts; it cannot independently prove that an external log was not fabricated. The architect review already inspected the harness and this review inspected the actual source and harness.
3. **Cleanup is partly self-attested.** The checker requires the cleanup claims and the text evidence. A separate read-only check in this session found no `tooba_offer283_connected` database row, no listeners on 5090/3100, and canonical 5088 `/health` 200 plus 3000 `/fa` 200. That is current-state evidence, not a re-execution of the original cleanup.
4. **Controlled error branches.** REQ-05 wrong/lost responses and the first failure in REQ-07 use Playwright response substitution; their success state and the lost-response write are real. This is disclosed in the manifest and is allowed by the task for error branches.
5. **Cursor gate.** The original plan/review gate exit 1 is retained byte-for-byte and is classified as `KNOWN_FORMAT_INCOMPATIBILITY`. It is not a product FAIL and is not a product PASS. No Cursor file was changed.

## Checker and negative controls

`VERIFY-OPEN-CODE.mjs` is dependency-free and read-only. It:

- verifies the exact protocol, keys, unique REQ IDs, PASS status, evidence paths, check names and result;
- recomputes SHA-256 for all referenced artifacts and compares them with the manifest and the source-evidence Git blobs;
- requires 38 assertions and validates the specific REQ-01/03/04/05 facts, plus REQ-02/06/07;
- checks source identity at 611, no source diff, frozen Cursor paths, expected task-folder diff, OpenSpec strict log and cleanup claims;
- runs its own negative self-test: deleting REQ-07, changing the REQ-01 price fact, and tampering with evidence bytes must each fail.

## Decision

The evidence is sufficient for **OpenCode acceptance** of Stage 1 and for architect review. The Cursor structural failure is a task-document schema defect, not a feature failure. The candidate method is provided as **CANDIDATE — NOT ADOPTED** and must not be promoted without explicit architect/user approval.
