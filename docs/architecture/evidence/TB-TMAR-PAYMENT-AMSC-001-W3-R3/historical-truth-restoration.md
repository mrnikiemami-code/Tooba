# TB-TMAR-PAYMENT-AMSC-001-W3-R3 — Historical Truth Preservation

- **Task**: `TB-TMAR-PAYMENT-AMSC-001-W3-R3`
- **Parent-Task**: `TB-TMAR-PAYMENT-AMSC-001-W3-R2`
- **Channel**: `tooba-main` · **WorkerId**: `tooba-worker-01` · **AgentType**: `cursor`
- **Mode**: `RECOVERY_HISTORICAL_TRUTH_PRESERVATION_ONLY`
- **Starting HEAD**: `83bc22175d92e53b838c3039cc09ee3e790c509c` (`83bc2217`)
- **Protocol**: `BRIDGE-WAKE-V1`

Recovery / history-integrity repair only. **Zero production, manifest, or schema change.**
R2 authoritative error-code truth and W3 certification authority are unchanged.

## 1. The recovery integrity defect

R2 was instructed to preserve the historical W1 text and reconcile it **additively**.
Instead it rewrote the historical field in place:

| Stage | `paymentAmsc001W1.stableErrorCodeState` |
|---|---|
| Before R2 (historical W1 record) | `CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED` |
| After R2 (rewritten) | `CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_4_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED` |
| After R3 (restored) | `CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED` |

Because R2 simultaneously added
`stableErrorCodeStateReconciliation = "HISTORICAL_W1_TEXT_SAID_3_CONSUMED; AUTHORITATIVE_COUNT_IS_4_FOREIGN_DECLARED_CONSUMED ..."`,
the SoT contradicted its own claim that the W1 historical text was preserved. R3 restores the
historical value so the record matches the actual W1 commit.

## 2. Additive reconciliation preserved

`paymentAmsc001W1.stableErrorCodeStateReconciliation` is **not** removed or weakened. It remains:

```
HISTORICAL_W1_TEXT_SAID_3_CONSUMED; AUTHORITATIVE_COUNT_IS_4_FOREIGN_DECLARED_CONSUMED
(W3-R2 reconciliation; see paymentAmsc001W3R2.stableErrorTruthState)
```

It continues to point to the R2 authoritative truth.

## 3. R2 authoritative truth preserved (unchanged)

| Field | Value |
|---|---|
| `declaredStableCodeCount` | `28` |
| `knownCodeGuardMemberCount` | `27` |
| `paymentOwnedDescriptorCount` | `24` |
| `foreignOwnedDeclaredConsumedCount` | `4` |
| `foreignOwnedKnownGuardCount` | `3` |
| `foreignOwnedNotInKnownGuard` | `admin.authorization.denied` |
| `stableErrorTruthState` | `AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS` |
| `currentCertificationAuthority` | `TB-TMAR-PAYMENT-AMSC-001-W3` |
| `currentCertifiedCommit` | `502d73e0e9ccfb277a06ad397b1f0a511f586921` |

## 4. Final R3 closure block

New `paymentAmsc001W3R3` block:

| Field | Value |
|---|---|
| `state` | `PAYMENT_AMSC_001_RECOVERY_FINAL_CLOSED` |
| `mode` | `RECOVERY_HISTORICAL_TRUTH_PRESERVATION_ONLY` |
| `startingHead` | `83bc2217` |
| `productionCodeChanged` | `false` |
| `currentCertificationAuthority` / `currentCertifiedCommit` | `TB-TMAR-PAYMENT-AMSC-001-W3` / `502d73e0...` |
| `recoveryAuthority` / `recoveryCommit` | `TB-TMAR-PAYMENT-AMSC-001-W3-R2` / `83bc22175d92e53b838c3039cc09ee3e790c509c` |
| `historicalW1StableErrorCodeState` | `CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED` |
| `historicalW1FieldState` | `RESTORED_AND_PRESERVED` |
| `additiveReconciliationState` | `PRESERVED` |
| `authoritativeStableErrorTruth` | `AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS` |
| `manifestStructuralState` / `schemaMigrationState` | `NOT_TOUCHED` / `UNCHANGED` |
| `globalHostCheckpointState` | `PRESERVED` |
| `guardsWeakened` / `baselinesWidened` | `NONE` / `NONE` |
| `workflowStop` / `automaticNextImplementationTask` | `USER_REVIEW_PAYMENT_AMSC_001_W3_R3` / `NONE` |

No self-referential R3 commit placeholder is recorded.

## 5. Lineage & authority preserved

- W3 certification authority: `TB-TMAR-PAYMENT-AMSC-001-W3` @ `502d73e0...` — `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` / `STRUCTURE_CERTIFIED`.
- R1 block `paymentAmsc001W3R1` @ `affdfba4...` — unchanged (`PAYMENT_AMSC_001_RECOVERY_RECONCILED`).
- R2 block `paymentAmsc001W3R2` @ `83bc2217...` — unchanged (`PAYMENT_AMSC_001_RECOVERY_CLOSED_RECONCILED`).
- Semantic wave SHAs `W0 6839bb4a` / `W1 2d69d828` / `W2 a138ec61` — unchanged.
- Global Host checkpoint `HOST_ROOT_FINAL_CERTIFIED` — preserved.

## 6. Scope proof

Changed files (exactly the ALLOWED set):

- `docs/architecture/tmar-current-state.json` (W1 historical field restored + R3 closure block)
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` (one short R3 closure note)
- `docs/architecture/evidence/TB-TMAR-PAYMENT-AMSC-001-W3-R3/*`
- `docs/ai/tasks/TB-TMAR-PAYMENT-AMSC-001-W3-R3.task.md`

Forbidden surfaces untouched: production files, guards, manifest, csproj, Host production,
frontend, resources/descriptors/error-code files, schema/migrations.

## 7. Verdict

`PASS` — historical W1 field restored exactly; R2 additive reconciliation and authoritative
28/27/24 truth preserved; W3 authority unchanged; production/manifest/schema untouched; global
Host checkpoint preserved; automatic next `NONE`.
