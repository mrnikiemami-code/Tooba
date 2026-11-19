# CORRECTION-ADDENDUM — TB-282-R2

Parent 282-R1 worker PASS was Architect PARTIAL on recovery/date/lookup/runtime. This addendum records B1–B4 closures:

| Gap | Closure |
| --- | --- |
| B1 recovery toast-only | Recovery card click runs `replayPendingStoryIntent` on frozen envelope bytes; executor owns all create/update/item/submit/review/schedule/stop/return/upload_add |
| B1 intent namespace | Storage key includes actor+role+shop; validate against current scope; lock before async digest |
| B2 raw YYYY date | JalaliDatePicker day + Tehran clock → canonical ISO; absent end = null |
| B2 seller adminHeaders language | `GET …/stories/locale-options` via Localization Contracts adapter |
| B2 product card winner CTA | `GET …/stories/product-options` + Offer eligibility lookup; platform published list when no shop |
| B3 localStorage-only scope | VendorShell dispatches same-tab scope event; Story screen invalidates on actor/shop change |
| B3 disableLiveGrid production | Removed; mount suite uses Story-owned harness |
| B4 Guid.Empty seller occurrence | Platform upload uses asset PK + metadata only; no Empty seller ledger rows |

Parent 282 / 282-R1 RESULT.md files are preserved unchanged.
