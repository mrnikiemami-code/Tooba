# RESULT — TB-296-R9-R1-FINAL-ARCHITECTURE-AND-FUNCTIONAL-REPAIR

TASK: TB-296-R9-R1-FINAL-ARCHITECTURE-AND-FUNCTIONAL-REPAIR
STATUS: PASS_WITH_LIMITATIONS (architecture acceptance remains with architect)
BASE R9: 63da7a2dbcaf61c0469252c65e5e42b499db6a0e
BRANCH: opencode/iso-agents-002
START: 2026-11-25 19:29:39.875 +03:30
END: 2026-11-25 21:06:26.855 +03:30
DURATION: 01:36:46

## Architecture repairs

- Workspace source split: `offer-workspace.tsx` 1161→785 physical lines via `offer-actions.ts` and `local-detail.ts`; save/create orchestration no longer lives in the rendering component. New files <800; no source-size baseline/exception changed.
- Frontend locale: removed feature-local `offer-locale.ts` query-param override and deep `features/admin-languages/api` import. Added shared `lib/i18n/registry-locale.ts`, shared middleware/routing registry prefix support (`/ru`), and actual `/ru/admin/offers` runtime proof. No feature query parameter.
- Public language ownership: new Localization.Application `ListActiveLanguagesQuery` + public DTO; `/v1/languages` no longer reuses admin query/DTO or admin edit metadata.
- Create/apply: moved duplicated admin/seller multi-domain workflow to shared `features/offer-create/offer-create-and-apply.ts`; pages supply transport only; reread now verifies amount, currency, SKU, return policy/days, min/max, fulfillment responsibility/type.
- Seller transport: split offer/picker functions into `seller-offer-api.ts`; `seller-api.ts` 1488→1116, below its 1211 baseline.
- Preserved R8 layout, R9 Toast/MessageBox, unit policy, inventory recovery, two-position proof, security and all functional behavior.

## Verification

- typecheck/build/lint PASS (lint pre-existing unrelated warnings); mount 26/26; API 16/16; model/i18n/routing 26/26; Offer 97/97; Host localization/Catalog/Stage2 11/11; runtime 32/32; OpenSpec strict.
- Runtime JSON SHA-256 `DA00DD5B86A5333D4798EE20BB8C201F36DFABC47C5AF15C989DDDE4A176DAFB`; cleanup `932561400F096C616955AE5309A1D9ECA1D46476DDAF06A1E8360FFD108A138D` (ok true).
- Performance comparable to R8: form 1028 ms vs 1598, picker 71 ms vs 103; `/evidence/r9r1-performance.json`.
- Frontend architecture guard: 27 pre-existing unrelated baseline debts; no offer/task file in size violations, no threshold/baseline change.
- Public language DTO evidence: `/evidence/r9r1-public-languages.json`.

## AQ outcome

AQ-01 PASS_WITH_LIMITATION, AQ-02 PASS, AQ-03 PASS, AQ-04 PASS, AQ-05 PASS, AQ-06 PASS_WITH_LIMITATION, AQ-07 PASS_WITH_BASELINE_DEBT, AQ-08 PASS. Full detail: `AUDIT-AQ-R9-R1.md`.

## Remaining exact blockers / limitations

- Channel authority by edition/seller/permission/policy has no owning contract; no fake selection or policy was added.
- Pre-create inventory provisioning and offer history contracts are absent; those tabs remain honestly gated.
- Offer/Pricing/Inventory expose no RowVersion/ConcurrencyToken; pre-read is not atomic compare-and-swap and is not claimed as such.
- Shared root Locale type/global shell and AppDataGrid chrome remain fa/en; registry route support currently covers the configured `ru` prefix only.
- 27 unrelated frontend architecture baseline debts remain. Owner UAT and architect acceptance remain external. TB-297 was not started.

STOP_FOR_ARCHITECT
