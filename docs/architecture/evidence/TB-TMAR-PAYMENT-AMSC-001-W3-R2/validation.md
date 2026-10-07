# TB-TMAR-PAYMENT-AMSC-001-W3-R2 — Validation

Bounded validation only (no full solution suite, no unrelated repair).

## 1. Bounded reconciliation checks (`validate.js`) — 29/29 PASS

```
PASS SoT JSON parse
PASS starting HEAD == affdfba4 :: affdfba4fc5fce402d05e68131bdb4a01899b93c
PASS parent 2d69d828 -> 6839bb4a :: 6839bb4a5f75a954817719386de04e36ff96c305
PASS parent a138ec61 -> 2d69d828 :: 2d69d82808178e786f0673dc725baeb238987829
PASS parent 502d73e0 -> a138ec61 :: a138ec61bcbbb719fed2949c60e21fa77104cfd1
PASS parent affdfba4 -> 502d73e0 :: 502d73e0e9ccfb277a06ad397b1f0a511f586921
PASS declarations == 28 :: 28
PASS descriptors == 24 :: 24
PASS KnownCodes members == 27 :: 27
PASS foreign not registered: ReservationRetryLimit
PASS foreign not registered: SupplyUnavailable
PASS foreign not registered: AdminAuthorizationDenied
PASS foreign not registered: CheckoutAuthenticationRequired
PASS KnownCodes has ReservationRetryLimit
PASS KnownCodes has SupplyUnavailable
PASS KnownCodes has CheckoutAuthenticationRequired
PASS KnownCodes EXCLUDES AdminAuthorizationDenied
PASS W3-R1 block recorded
PASS W3-R1 commitFull exact
PASS W3-R2 block closed
PASS W3-R2 truth counts
PASS W3-R2 admin auth excluded
PASS W3 authority preserved
PASS W3 verdict preserved
PASS W3 structure fields preserved
PASS W0/W1/W2 commit truth durable
PASS global host checkpoint preserved :: HOST_ROOT_FINAL_CERTIFIED
PASS no unexpected changed files :: []
PASS no Payment production file changed
---
TOTAL 29 FAILED 0
```

Covers: JSON parse, exact SHA assertions, exact parent-chain assertions, exact 28/27/24 count
assertions, exact foreign-code set assertions, W3-R1/W3-R2 SoT truth, W3 authority preservation,
global Host checkpoint preservation, and git diff scope proof.

## 2. Focused Payment W3 cert guard (modified) — 10/10 PASS

```
Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10 - Tooba.Host.Tests.dll (net8.0)
```

`PaymentModuleAmsc001W3CertGuardTests` gained one truth-lock test
(`Recovery_closure_pins_exact_semantic_lineage_and_error_code_truth`) pinning the exact
semantic lineage, the W3-R1/W3-R2 recovery blocks, the 28/27/24 counts, the 4-declared /
3-Known foreign split with `admin.authorization.denied` excluded, and
`PaymentErrorCodes.IsKnown` behaviour for all four foreign codes. No existing assertion was
weakened.

## 3. Composed catalog uniqueness guard — 3/3 PASS

```
Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3 - Tooba.Host.Tests.dll (net8.0)
```

No descriptor duplication; the four foreign codes remain unregistered by Payment.

## 4. Shallow structure spot check

W3 cert guard's structure assertions (capability-first root, zero per-use-case leaf,
`/Modules/Payment/` 6-project solution grouping, root allowlist) remained green inside the
10/10 run above. No structure file was touched by this wave.

## 5. Diff scope proof

Only the ALLOWED files changed. The pre-existing unrelated working-tree artifacts (22 modified
non-Payment files + 28 untracked evidence files from other modules) were recorded as the
baseline in `preexisting-unrelated-artifacts.txt` and are preserved untouched;
`no unexpected changed files = []`.

## 6. Verdict

`PASS` — bounded validation green; zero production/manifest/schema change; W3 remains
certification authority; global Host checkpoint preserved; automatic next `NONE`.
