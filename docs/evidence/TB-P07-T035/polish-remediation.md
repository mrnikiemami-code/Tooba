# TB-P07-T035 — Polish remediation after live audit FAIL

Source audit: [`browser-audit-report.md`](./browser-audit-report.md) from [Audit live Catalog UX](fbca184d-eab5-4256-9a6f-31232f6432a4)  
`USER_VISUAL_ACCEPTED=NO`

## Audit items addressed

| Audit finding | Remediation | Live verify |
|---|---|---|
| Raw HTML tags in VIEW description | Render sanitized HTML via `sanitizeProductRichHtml` + `dangerouslySetInnerHTML` (`product-general-description-preview`) | CDP: `htmlLeak=[]` |
| Technical `no-active-offer` | FE `formatReadinessWarningFa` + Host warning string Persian | Snapshot shows «پیشنهاد فروشندهٔ فعالی ثبت نشده است» |
| Raw ISO in activity/audit feed | Map `at` through `formatHistoryTimestamp` | «۸ شهریور ۱۴۰۵، ۲۰:۰۹» |
| «اجازه نیست» under intentional VIEW | Removed automatic `permissionDenied` banner from `WorkspaceShell` when `readOnly` | CDP: `denied=false` |
| Catalog updated-at ISO | `formatJalaliDateTime` on SummaryCards | «1405/06/08 20:09» |
| Tiny gray 48×48 demo media | `CatalogDemoMediaFactory` patterned 320×320 PNG (`-v2`), WriteChunk buffer fixed; reset-and-seed | CDP: `nw/nh=320` |

## Still not photographic vs locked reference

Demo media remains **generated patterned PNG** (not stock photography). After reseed they are clearly colored 320×320 thumbs (not blank gray 48px). Photographic parity with the beanie reference image is **out of Catalog binary-storage policy** (Media DAM placeholders only).

## Intentional / non-defect distance from reference

- Locale-based title/description live under Translations (not General fixed FA/EN fields).
- Draft products correctly show commercial warnings (no Seller Offer/price/stock) — Product ≠ Offer.
- Admin chrome differs from the marketing-style reference chrome (notifications / dark mode / logo cluster).

## Follow-up audit note

Earlier FAIL on “gray circular hero / color blocks” was captured **before** the media factory + reseed landed. Re-check screenshots after Host restart + `POST /v1/admin/catalog/demo/reset-and-seed`.
