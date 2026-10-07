# TB-TMAR-PARTY-AMSC-001-W3-R3 — Authoritative W3 truth reconciliation

One tiny documentation/recovery truth reconciliation; zero production change, zero manifest change, zero schema change, zero guard weakening.

## Stale W3 Master Recovery text (before)

In `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`, the current Party W3 certification checkpoint (`Recorded by TB-TMAR-PARTY-AMSC-001-W3 (Certify)`), canonical-mechanisms bullet, said:

```
single stable-code owner `Contracts/Errors/PartyErrorCodes.cs` (11 declared codes with the certified `IsKnown(string?)` declared-code guard → 10 registered descriptors; `seller.authorization.denied` stays Foundation-owned)
```

## Corrected 11/11 meaning (after)

```
single stable-code owner `Contracts/Errors/PartyErrorCodes.cs` (11 declared Party-owned stable codes with the certified `IsKnown(string?)` declared-code guard → 11 registered Party-owned descriptors; descriptor-count wording reconciled from a stale single-digit descriptor analysis note by `TB-TMAR-PARTY-AMSC-001-W3-R3`; `seller.authorization.denied` stays Foundation-owned and is not duplicated by Party)
```

Only that one bullet was edited. All other W3 checkpoint facts (lineage `2477bbb3 → 29012df0 → ffff7100 → d1cc2f48`, verdict `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` `STRUCTURE_CERTIFIED`, structure, cross-module boundary, R1/R2 recovery sub-checkpoints) are untouched.

## Direct counts from source (repository truth)

- `src/backend/Modules/Party/Tooba.Party.Contracts/Errors/PartyErrorCodes.cs` — `public const string` declarations = **11**.
- `src/backend/Modules/Party/Tooba.Party.Contracts/Errors/PartyErrorCatalogContributor.cs` — `D(PartyErrorCodes.` descriptor registrations = **11**.
- Authoritative count = **11 declared / 11 registered**. The 10-descriptor wording was a stale analysis remnant; the Foundation-owned `seller.authorization.denied` is consumed from the Foundation owner and **not duplicated** by Party (Party's 11 descriptors are all Party-owned).

## Preservation proofs

- **Historical W0 truth preserved**: `partyAmsc001W0.stableErrorCodeState = CATALOGUED_10_OF_10_MISSING_ISKNOWN_DECLARED_CODE_GUARD` unchanged verbatim in `tmar-current-state.json`, together with the R2 additive field `stableErrorCodeStateReconciledByR2` marking it stale W0 analysis metadata. Not rewritten.
- **R2 lineage preserved**: `partyAmsc001W3R2` block unchanged (state `PARTY_AMSC_001_RECOVERY_CLOSED_RECONCILED`, recovery commit `3eb6e722179d50e308a986b20a4040b45a15555d`); the R2 Master Recovery sub-checkpoint unchanged.
- **W3 certification authority unchanged**: `TB-TMAR-PARTY-AMSC-001-W3` at `d1cc2f480619ce1ec68cd730650018883684e6c7`, verdict/lock/authority untouched.
- **Production/manifest/schema unchanged**: zero bytes under `src/`, `tmar-module-structure-manifests.json`, and any migration in this wave (git diff scope proof in `validation.md`).
- **Host checkpoint unchanged**: repository-global Host root checkpoint fields preserved exactly (global recovery lock respected).

## Post-result evidence commit recognition

`fc2005eb528c5506aec69f157530e218e0eb3561` (R2 starting head) is a post-result evidence-only commit (parent `3eb6e722…`; adds only `post-result.js`). It is not classified as a recovery implementation wave or certification authority. It is recorded in the SoT R3 closure block as `postResultEvidenceCommit`.
