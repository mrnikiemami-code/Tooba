# TB-TMAR-PRICING-AMSC-001-W3-R4 — lineage reconciliation

All facts below were derived from the repository with `git rev-list --parents -n 1` / `git show --stat`
at starting head `127c596a`; none is asserted from memory.

## 1. Exact parent chain

| Commit | Parent | Subject |
| --- | --- | --- |
| `08d47b6a` | `a1d9ecfb` | `TB-TMAR-PRICING-AMSC-001-W0: analyze Pricing module architecture …` |
| `069f77d2` | `08d47b6a` | `TB-TMAR-PRICING-AMSC-001-W1: migrate Pricing …` |
| `f7f6abfe` | `069f77d2` | `TB-TMAR-PRICING-AMSC-001-W2: structure Pricing …` |
| `3c2cc61e` | `f7f6abfe` | `TB-TMAR-PRICING-AMSC-001-W3: certify Pricing module under ARCH-COMPLETE-002` |
| `2e664bb3` | `3c2cc61e` | `TB-TMAR-PRICING-AMSC-001-W3-R1: reconcile Pricing AMSC lineage …` |
| `7159c8f7` | `2e664bb3` | `TB-TMAR-PRICING-AMSC-001-W3-R2: repair Pricing to INTERNAL_ONLY structure …` |
| `a1ca9b5a` | `7159c8f7` | `feat(pricing): certify Pricing module under ARCH-COMPLETE-002 (AMSC W3-R3)` |
| `127c596a` | `a1ca9b5a` | `docs(pricing): add required W3-R3 recovery-debt disclosure evidence` |
| `549f1ac5` | `127c596a` | `docs(pricing): record W3-R3 Bridge result artifact` |

```text
actualParentChainState = RECONCILED
```

`08d47b6a`'s parent is `a1d9ecfb` — the Payment W3-R3 recovery commit that immediately preceded the
Pricing AMSC run; it is outside the Pricing lineage and is not part of the reconciliation.

## 2. Architecture sequence vs evidence-only tail

```text
Architecture sequence:
08d47b6a -> 069f77d2 -> f7f6abfe -> superseded 3c2cc61e -> historical 2e664bb3
         -> Structure 7159c8f7 -> fresh Certify a1ca9b5a

Evidence-only hop:
a1ca9b5a -> 127c596a -> 549f1ac5
```

`127c596a` is **not** certification authority. Current certification authority is
`TB-TMAR-PRICING-AMSC-001-W3-R3` @ `a1ca9b5a`; current structure authority is
`TB-TMAR-PRICING-AMSC-001-W3-R2` @ `7159c8f7`; original `TB-TMAR-PRICING-AMSC-001-W3` @ `3c2cc61e`
is historical/superseded.

## 3. Scope verification of the certification commit (`a1ca9b5a`)

`git show --stat a1ca9b5a` — 22 files changed, +2845/−95:

- adds `src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3R3CertGuardTests.cs` (+544);
- promotes the `Pricing` entry inside `docs/architecture/tmar-module-structure-manifests.json`;
- updates `docs/architecture/tmar-current-state.json` (adds the `pricingAmsc001W3R3` block and re-adds
  `"Pricing"` to `structureLock.certifiedModules`);
- appends the W3-R3 checkpoint to `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`;
- repoints `PricingModuleAmsc001W2StructureGuardTests.cs`, `PricingModuleAmsc001W3R2RepairGuardTests.cs`
  and `TmarCompleteReferenceStructureGateTests.cs`;
- adds the W3-R3 task + evidence package.

It is therefore exactly the W3-R3 certification commit, and it is the certification authority.

## 4. Scope verification of the evidence-only hops

| Commit | Files changed | Classification |
| --- | --- | --- |
| `127c596a` | `docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/recovery-debt.md` (+64) | `EVIDENCE_ONLY_NOT_AUTHORITY` |
| `549f1ac5` | `.../TB-TMAR-PRICING-AMSC-001-W3-R3/RESULT.bridge.txt` (+49), `.../post-result.js` (+52) | `EVIDENCE_ONLY_NOT_AUTHORITY` |

Neither touches `src/`, the manifest, the SoT, any guard, any schema or any migration. Neither is
classified as a recovery implementation wave.

## 5. SHA fields written by this wave

| SoT block | Field | Value |
| --- | --- | --- |
| `pricingAmsc001W0` | `commit`, `commitFull` | `08d47b6a4af3e4e2102712b93f27185ac15a23ac` |
| `pricingAmsc001W1` | `commit`, `commitFull` | `069f77d2fa5b2c2cec3bb078147c3559a571bd64` |
| `pricingAmsc001W2` | `commit`, `commitFull` | `f7f6abfec455b771952852e8627df4c57b698caf` |
| `pricingAmsc001W3R1` | `commit`, `commitFull` | `2e664bb336f45b8304818b7e5754f9b0fc364f20` |
| `pricingAmsc001W3R2` | `commit`, `commitFull` | `7159c8f773c1faa9b4b6d425b19067f50ca27572` |
| `pricingAmsc001W3R3` | `commit`, `commitFull` | `a1ca9b5afab181da74fe6db0efbb38f43a3a9721` |
| `pricingAmsc001W3R3` | `postCertificationEvidenceCommit` | `127c596aba7abefe6bdd0a1edb7c69c064267762` |
| `pricingAmsc001W3R3` | `postResultEvidenceCommit` | `549f1ac535e37e4e8c9e3c44b88768c575dfeb5e` |
| `pricingAmsc001W3R4` | `w0Commit` … `w3R3PostResultEvidenceCommit` | mirror of the above |

```text
evidenceOnlyCommitClassification = EVIDENCE_ONLY_NOT_AUTHORITY
```

## 6. Preserved historical fields (spot-checked by the apply script)

- `pricingAmsc001W0.state` / `.verdict` / `.startingHead` (`a1d9ecfb`);
- `pricingAmsc001W1.parentCommit` (`08d47b6a`) and `pricingAmsc001W2.parentCommit` (`069f77d2`);
- `pricingAmsc001W3R1.certifiedCommit` (`3c2cc61e…`), `.masterRecoveryW3ShaBefore`
  (`PENDING_THIS_COMMIT` — the historical pre-reconciliation marker, deliberately kept) and
  `.masterRecoveryW3ShaState` (`RECORDED_3C2CC61E`);
- `pricingAmsc001W3R2.supersededCertificationCommit` (`3c2cc61e…`) and `.state`;
- `pricingAmsc001W3R3.verdict` / `.structureCertified` / `.declaredCodeCount` / `.currentStructureCommit`;
- the `acceptedLineage` blocks, the manifest, and `structureLock.certifiedModules`.

## 7. Stop rule

No R5. No next module. `workflowStop = USER_REVIEW_PRICING_AMSC_001_W3_R4`;
`automaticNextImplementationTask = NONE`.
