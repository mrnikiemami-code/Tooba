# ARCHITECT-REVIEW — TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4

## Decision

**ACCEPT WITHIN THE ISSUED FOUR-DEFECT REPAIR SOURCE/EVIDENCE SCOPE** at
`main c501fe9bcdb2c42a26d05e23d44af288931612ed`.

This is not a claim of user visual/functional acceptance, a live eligible-parcel click-through, a true two-writer race,
or any whole-shipping / provider / financial / consent acceptance.

## Reviewed

Source, as changed by the R4 repair commit and read against base `1ff02ab8a18c20999b92791f2b51520726bfa2d1`:

- `admin-shipment-plan-attempt.ts` / the plan modal: the exact request body and occurrence key are written **and** read
  back **before** the executor runs, so an unmount / reload / process kill with no `catch` recovers the identical
  intent; a TB-270-R3 `uncertain:false` record reads as unresolved, a failed durable write refuses to send, and only an
  authoritative success/replay or a proven no-write refusal resolves it.
- Close **and** unmount/cleanup bump the presentation epoch; every stale callback (including `finally`) is fenced;
  storage is compare-and-cleared/updated on the exact occurrence + body rather than by scope.
- `plan_shipment_dispatch`: `RequireNewPlanGatesAsync` (checkout-not-cancelled **and** the async active-package
  membership check) is shared by the new confirmation and the new-calendar preview, and runs **after** a stored plan
  has already answered replay — so history survives cancellation/dispatch while a direct new POST inserts nothing.
- Handoff identity: resend needs the exact source **and** a non-empty reviewed key on the child; replacement needs the
  reviewed decision on the REQUESTED source, its key, and its own `Created` child. The "only remaining candidate"
  fallback and the cross-source decision scan are gone.

Focused evidence read: `first-send-and-lifecycle-proof.md`, `command-gates-and-exact-handoff-proof.md`,
`RESULT.md`, `RECEIPTS.md`, `result-contract.txt`.

## Evidence status

`AdminShipmentPlan270R2Tests` **28/28** (25 retained + 3 Repair-B), `created-parcel.test.ts` **13/13** run explicitly,
plan mount **14/14**, delivery mount **75/75**, `tsc --noEmit` 0, `npm run build` 0
(`BUILD_ID cz4aReIm9Nx33hKljkmfr`), `dotnet build Tooba.slnx -c Release` 0 errors, Host publish 0 errors. Runtime:
Host 5088 PID 48388 (`/health` + `/health/ready` 200, SingleStore), Next 3000 PID 31184, served chunk
`9424-c99e9070dc020045.js` 200 with the plan-modal copy.

These are **worker** runs and observations, not independent Architect runtime execution. No new broad test replay was
required or performed by this review.

## Retained history (not rewritten)

- R1 remains ACCEPT within its additive read-only captured-input seam.
- R2 and R3 remain **PARTIAL** as originally reviewed; R4 addresses their findings for this bounded continuation. The
  R2/R3 review text and the R2/R3 declared limits stay as written.
- `270` stays PARTIAL as the parent design task. `271` (employee queue job stages / time scopes) was issued as the
  separate follow-up and is not part of this acceptance.

## Limits carried forward (unchanged)

- R2's pre-apply backup was never taken; only the post-apply dump exists and it is not retroactive protection. R3/R4
  added no schema change.
- True two-writer concurrency remains **UNPROVEN**; arbitration rests on the unique index plus independent contexts,
  not a stress harness.
- The review URL is completed history only — no eligible live `Created`/unplanned linked parcel existed, so mounted and
  owning-application evidence is worker evidence, not a browser session. No live plan row or click-through was produced.
- Untracked `scripts/tb266r1-*.mjs` / `scripts/tb270r2-*.mjs` scratch files are not part of any change set.

## Next

No implementation task is issued by this review. `automaticNextImplementationTask: NONE`.
