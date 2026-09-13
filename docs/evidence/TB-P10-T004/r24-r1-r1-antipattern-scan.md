# TB-P10-T004-R24-R1-R1 — Anti-pattern scan

| Anti-pattern | Result |
| --- | --- |
| guessed splitting of RecipientName | CLEAN — no Split/parse of RecipientName in Host recipient helpers |
| frontend-only precedence | CLEAN — Payment/Order use Host Display; FE `recipientDisplayName` matches the same rule for address lists only |
| Payment re-reading stale AddressBook RecipientName | CLEAN — MapPage uses committed snapshot Display |
| multiple different name-format helpers | CLEAN — one Host helper; FE list helper is the same first+last-else-recipient rule, no new splitter |
| RecipientName overriding non-empty First/Last | CLEAN — Resolve / ResolveExplicitOverLegacy / Display |
| destructive migration of historical names | CLEAN — no data rewrite |
| hardcoded sample names in product code | CLEAN — sample names only in focused tests/evidence |
| regression to combined-only storage | CLEAN — First/Last still persisted independently |

Scan: CLEAN.
