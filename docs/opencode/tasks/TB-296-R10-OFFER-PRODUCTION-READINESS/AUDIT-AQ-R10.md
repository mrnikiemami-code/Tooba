# AQ audit — R10

| AQ | Result | Evidence |
|---|---|---|
| AQ-01 central locale | PASS_WITH_LIMITATION | Public Localization CQRS/DTO; shared registry route `/ru`; no feature query or admin-language deep import. Global shell/shared grid chrome remains fa/en. |
| AQ-02 backend layering | PASS | Channel policy/query, Inventory provisioning gateway, history ports/routes, thin endpoints; no Host business logic. |
| AQ-03 frontend layering | PASS | Routes compose; typed admin/seller transport; shared create/apply use case; workspace 798 lines; seller transport split. |
| AQ-04 CQRS | PASS | MediatR queries/commands for allowed channels, inventory locations/positions, history; Result/validators; no DB write in reads. |
| AQ-05 component reuse | PASS | Shared Toast/MessageBox/AppDataGrid/design system; no parallel notification/grid. |
| AQ-06 folder/namespace | PASS_WITH_LIMITATION | New Offer feature/use-case and seller transport modules; legacy flat `app/admin/admin-offer-api`/intent remains pre-existing. |
| AQ-07 source size | PASS_WITH_BASELINE_DEBT | `offer-workspace.tsx` 798; `seller-api.ts` split; no offer size violation. 27 unrelated baseline debts unchanged; no baseline/guard edits. |
| AQ-08 module separation/Host | PASS | Contracts-only cross-module access; StoreContext public contract; no foreign DbContext/Host business diff. |

Overall: PASS_WITH_LIMITATIONS. Architect acceptance and owner UAT remain external. No TB-297.
