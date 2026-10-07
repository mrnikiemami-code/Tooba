# TB-TMAR-PARTY-AMSC-001-W3-R2 — Historical W0 error-count reconciliation

## Finding

The historical W0 SoT field recorded:

```
"stableErrorCodeState": "CATALOGUED_10_OF_10_MISSING_ISKNOWN_DECLARED_CODE_GUARD"
```

This count is **stale analysis metadata**. The actual catalog at the W0/W1 tree already carried 11 stable codes.

## Proof (independent repository verification)

1. **Current `PartyErrorCodes.cs` declares exactly 11 constants** — `rg -c "public const string"` over `src/backend/Modules/Party/Tooba.Party.Contracts/Errors/PartyErrorCodes.cs` = `11` (SellerSettingsMissing, SellerSettingsRejected, OperationRejected, DisplayNameRequired, DisplayNameLength, LegalNameShape, DescriptionShape, SupportPhoneShape, SupportEmailShape, AddressLineShape, AdminSellersGridRequestRequired).
2. **Current `PartyErrorCatalogContributor.cs` registers exactly 11 descriptors** — `rg -c "D\(PartyErrorCodes\."` = `11`.
3. **W1 did not add or remove error constants** — `git show 29012df0 -- src/backend/Modules/Party/Tooba.Party.Contracts/Errors/PartyErrorCodes.cs` filtered for added `public const string` lines = **0 added constants**. The W1 diff (+26/−1) added only the `KnownCodes` HashSet, the `IsKnown(string?)` guard, and the expanded doc-comment; all 11 `const` declarations predate W1 (they existed in the pre-W1 file, which the W0 analysis undercounted as 10).
4. **W3 certification truth already uses 11** — `PartyModuleAmsc001W3CertGuardTests.Canonical_seams_are_present_and_single_owned` asserts `Assert.Equal(11, Regex.Matches(codes, "public const string ").Count)` and the W3 SoT block records `localizationState: CANONICAL_BILINGUAL_11_KEY_RESX_PAIR…` with 11 bilingual resx keys. W0's 10/10 was the only stale value in the lineage.

## Reconciliation (additive, no history rewrite)

- The original W0 field `CATALOGUED_10_OF_10_MISSING_ISKNOWN_DECLARED_CODE_GUARD` is **preserved verbatim** as historical truth (the analysis text was genuinely written that way at `2477bbb3`).
- A new additive field was appended to `partyAmsc001W0`:

```
"stableErrorCodeStateReconciledByR2": "HISTORICAL_ANALYSIS_METADATA_STALE — the W0 analysis text recorded a 10/10 catalogued count, but the actual catalog at the W0/W1 tree already declared 11 stable constants (the W0 analysis undercounted; W1 added IsKnown + the typed seam and did not add/remove error constants). Authoritative count from W1/W3 onward = 11/11. Original W0 field preserved above as historical truth, not rewritten."
```

- SoT R2 block fields: `w0HistoricalStableErrorCountRecorded = 10`, `authoritativeStableErrorCount = 11`, `authoritativeStableErrorDescriptorCount = 11`, `historicalW0ErrorCountState = STALE_ANALYSIS_COUNT_RECONCILED_BY_R2`, `currentW1W3ErrorCountState = AUTHORITATIVE_11_OF_11`, `productionCatalogChangedByR2 = false`.
- No production file, no manifest entry, no guard, and no resx resource was touched by this reconciliation.
