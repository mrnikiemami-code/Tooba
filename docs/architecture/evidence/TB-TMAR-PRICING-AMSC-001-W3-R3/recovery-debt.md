# TB-TMAR-PRICING-AMSC-001-W3-R3 — recovery debt disclosure

- Module: `Pricing`
- Wave: `TB-TMAR-PRICING-AMSC-001-W3-R3` (`FRESH_CERTIFY_AFTER_INTERNAL_ONLY_STRUCTURE_REPAIR`)
- Disclosed state: `POST_CERT_RECOVERY_RECONCILIATION_REQUIRED`
- Repaired in this wave: **NO** (explicitly forbidden by the task `FORBIDDEN` list)

## 1. Why this file exists

The W3-R3 task requires that known historical recovery debt is **not hidden**. This wave is a Certify
wave only: it must record the debt honestly and must **not** attempt historical lineage repair
(`FORBIDDEN: historical lineage repair in this Certify wave`). No historical block was edited, no
placeholder was rewritten, and no self-referential placeholder was introduced for W3-R3 itself.

## 2. Disclosed historical debt (verbatim, unmodified)

| # | Block in `docs/architecture/tmar-current-state.json` | Debt |
| --- | --- | --- |
| 1 | `pricingAmsc001W0` | still carries unresolved historical self-commit placeholders — `commit` / `commitFull` hold `PENDING_THIS_COMMIT`-style values instead of a real SHA |
| 2 | `pricingAmsc001W1` | records only `parentCommit`; it has **no** final `commit` / `commitFull` field of its own |
| 3 | `pricingAmsc001W2` | records only `parentCommit`; it has **no** final `commit` / `commitFull` field of its own |
| 4 | `pricingAmsc001W3R1` | has **no** final commit SHA of its own recorded in the block |

These are **lineage-bookkeeping gaps only**. They do not affect current architecture authority: the
current Pricing structure authority is `TB-TMAR-PRICING-AMSC-001-W3-R2` @ `7159c8f7`, which is itself
superseded as a certification claim by this W3-R3 wave.

## 3. What W3-R3 deliberately did NOT do

- did **not** back-fill the `pricingAmsc001W0` placeholders with a real SHA;
- did **not** add synthetic `commitFull` fields to `pricingAmsc001W1` / `pricingAmsc001W2`;
- did **not** invent a commit SHA for `pricingAmsc001W3R1`;
- did **not** rewrite, reorder or delete any historical Pricing block;
- did **not** widen any guard or baseline to mask the debt.

## 4. Self-referential placeholder avoidance for this wave

No `PENDING_THIS_COMMIT` placeholder was written for W3-R3. The W3-R3 certification commit SHA is
reported through the Bridge Result contract only, and the SoT block records:

```text
certificationCommitState = REPORTED_IN_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_SOT_SHA
```

`PricingModuleAmsc001W3R3CertGuardTests.Pricing_is_freshly_certified_in_manifest_and_sot` asserts this
state and asserts that the W3-R3 block contains **no** `commit` field
(`Assert.False(w3.TryGetProperty("commit", out _))`), so a future self-referential placeholder cannot be
silently introduced.

## 5. Required follow-up

```text
POST_CERT_RECOVERY_RECONCILIATION_REQUIRED
```

A dedicated (non-Certify) recovery-reconciliation wave must, when authorized by the Architect:

1. resolve the `pricingAmsc001W0` historical self-commit placeholders to real SHAs;
2. add the missing final `commit` / `commitFull` lineage fields for `pricingAmsc001W1` and
   `pricingAmsc001W2`;
3. record the historical `pricingAmsc001W3R1` final commit SHA.

Until then the debt stays **open and disclosed**. This wave does not self-authorize that follow-up:
`automaticNextImplementationTask = NONE`, `workflowStop = USER_REVIEW_PRICING_AMSC_001_W3_R3`.
