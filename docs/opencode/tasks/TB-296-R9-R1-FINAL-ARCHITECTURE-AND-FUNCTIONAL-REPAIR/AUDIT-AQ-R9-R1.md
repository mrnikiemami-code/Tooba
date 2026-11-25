# AQ architecture audit — R9-R1

Base R9: 63da7a2dbcaf61c0469252c65e5e42b499db6a0e. R8-approved layout and R9 behavior preserved.

| Gate | Status | Evidence / limitation |
|---|---|---|
| AQ-01 central multilingual | PASS_WITH_LIMITATION | Feature query override and deep admin-language import removed. Shared `lib/i18n/registry-locale.ts` reads the active route prefix and public `/v1/languages`; shared middleware/routing now accepts the registry `ru` prefix. The root `Locale` type/global shell still has fa/en semantics; full dynamic registry routing needs shared Locale type/provider expansion. Shared grid chrome remains fa/en. |
| AQ-02 clean backend | PASS | Public active-language query/DTO live in Localization.Application; endpoint is thin; no persistence write in read; existing Result/handler pattern. |
| AQ-03 clean frontend | PASS | Route pages compose adapters; multi-domain create/apply moved to `features/offer-create/offer-create-and-apply.ts`; transport only in pages. Workspace actions/local detail/components split. |
| AQ-04 CQRS | PASS | `ListActiveLanguagesQuery` + handler + public DTO; no admin query reuse; thin endpoint; existing Offer CQRS/Result unchanged. |
| AQ-05 component reuse | PASS | ToastContainer, shared MessageBox (additive actions slot), AppDataGrid, shared tabs/design system retained; no parallel notification/grid. |
| AQ-06 folder/namespace | PASS_WITH_LIMITATION | New cohesive modules: `lib/offer-workspace/offer-actions.ts`, `local-detail.ts`, `features/offer-create`, `lib/i18n/registry-locale.ts`; seller offer transport split into `app/vendor-panel/seller-offer-api.ts` (existing feature area). No arbitrary feature moves. |
| AQ-07 source size | PASS_WITH_BASELINE_DEBT | `offer-workspace.tsx` 1161→785 (<800); `seller-api.ts` 1488→1116 (< baseline 1211); new files <800. `npm run test:architecture` still reports 27 pre-existing unrelated oversized/admin-folder debts; none of this task's offer files appear in the size violations and no baseline/exception was changed. |
| AQ-08 module separation/Host | PASS | No Host production diff in this task. Localization public query/endpoint owned by Localization; Offer/Catalog/Inventory changes use Contracts; no foreign DbContext. |

## Architecture result
PASS_WITH_LIMITATIONS. AQ-01 and AQ-06 limitations are explicit. No baseline suppression, threshold change, migration, Host business code or TB-297 start.
