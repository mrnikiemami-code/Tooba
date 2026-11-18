# TB-280 — RESULT

Task: `TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280`
Parent: `TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: cursor
Status: **PASS within the issued A–E operator-UX scope**

Decision:
`PLAIN_BUSINESS_LANGUAGE_ONE_CLEAR_NEXT_ACTION_CONTEXTUAL_REVIEW_OF_REAL_RECEIVED_GOODS_AND_PROGRESSIVE_HISTORY_ON_THE_EXISTING_RETURNS_WORKSPACE_WITH_ACCEPTED_OWNER_GUARDS_PRESERVED`

Expected-Base-SHA `9aaf5bcd486ed63271ef431be20b7e24e8bb1951` is an ancestor of the claimed HEAD. Claim HEAD
`2fb3791f0c1bd124ee9877f163918a25fdfa585a` is the docs-only issuance of this task (its parent is the accepted 279-R3
result), so no incoming source/config/schema drift arrived with the claim.

NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT sellable/Marketplace readiness. 279-R3's accepted
backend correctness is preserved, not re-reviewed here.

| Area | State | What this means |
| --- | --- | --- |
| A stage visibility | received goods only | `ReturnWorkPolicy.Describe` publishes `publish_resolution` only for a coherent receipt with `Received > 0m` (it previously tested `Receiving.Lines.Count > 0`, which is true for the REQUESTED lines even when nothing arrived); a complete reviewed plan is still never re-published; every write rule is untouched |
| A primary action | one, obvious | the published codes are rendered from ONE ordered plan: the first offered entry is a full-width labelled primary button with a one-sentence explanation, the rest are quiet secondary buttons under «اقدام‌های دیگر»; no action is invented, the same `data-testid="admin-return-action-*"` identities are kept |
| B layout | next step near status | the next-step card now sits immediately after the case status card, above the items table; the audit/receipt/refund/decision logs live in ONE collapsed «سابقه» disclosure; outstanding rejected goods stay visible while any remain |
| C review dialog | honest local validation | an EMPTY accepted portion means zero (no red error on an untouched dialog); a typed over-ceiling/mis-precision value is refused inline; the per-line reason is labelled required, must be ≥3 chars, and is refused locally before any bytes are stored or sent; the five exact portion fields are still derived from the deliberate accepted values + stored receipt facts |
| C copy | shop language | action labels, dialog titles, status sentence, money words and the Returns `return.resolution.*` / `return.correction.*` error copy no longer use «برنامهٔ تفکیک»، «نسخهٔ تصمیم»، «نسخهٔ بررسی‌شده»، raw tokens or lock/storage explanations; the technical version/occurrence values still travel on the wire unchanged |
| D proof | focused | `ReturnsPlainLanguageWorkflow280Tests` 4/4; `ReturnsOperatorWorkflow275Tests` + `ReturnsDecisionCorrection279DirectoryTests` 36/36 (combined focused 40/40); mounted 276+277+279+280 plus quantity/queue/error-map/return-api 76/76; `tsc` 0; `next build` 0 (`BUILD_ID aneVaAaZv-4AvKZggbQEJ`); `dotnet build Tooba.slnx -c Release` 0 errors; Host publish 0 errors |
| served | refreshed | Host `5088` PID `31652`, dll SHA256 `147D033E79F774EE2F24925C501CF0DCE823571529F3ACE0004EBA37C096605B` (`/health` 200, `/health/ready` 200 SingleStore + messaging Healthy, `/__platform-commerce` → `store-alpha`); Next `3000` PID `26856`, `BUILD_ID aneVaAaZv-4AvKZggbQEJ`, chunk `7944-e29aaa760b08cd39.js` 200 carries «قدم بعدی»/«بررسی مرجوعی» and none of «برنامهٔ تفکیک»/«نسخهٔ بررسی» |
| live | read-only, one case | `01a1eefb-ea96-7000-838d-9a0b37661c8b` is still `Requested`, `updatedAt` `2026-11-18T14:07:29.430866Z`, received `0`, attempts `0`, receipts `0`, decision version `0`, `canPublishResolution=false`, `actionCodes=["record_receipt"]`; DOM at 1440/390: document overflow 0, ONE primary action, «قدم بعدی» sentence, no alert before interaction; the receipt dialog opened and cancelled with zero command sent |
| data/schema | untouched | zero migration/DDL/backfill/seed/reset/cleanup/restore, zero live POST, zero receipt/decision/resolution/refund/custody/stock/provider/Wallet/notification effect, no second DB/port/config change |

## Known limits

- the mounted proofs are jsdom behaviour with controlled HTTP (not geometry); the live checks are read-only DOM/text
  measurement at 1440 and 390 (no screenshots), so "looks right" remains the owner's call;
- the live case has no received goods, so the positive-received review dialog was exercised only on the mounted component
  and the fixture, not against stored data (no live POST was authorized);
- «اختیاری» still appears inside the shared admin chunk for unrelated surfaces; the Returns resolution dialog itself is
  proven label-free of it;
- the 279-R3 controlled-port limits and the contained key exposure (`CONTAINED_PENDING_REVIEWED_RETIREMENT`) are unchanged
  and out of this task's scope.

STOP_FOR_ARCHITECT_AND_USER_REVIEW; `automaticNextImplementationTask: NONE`.
