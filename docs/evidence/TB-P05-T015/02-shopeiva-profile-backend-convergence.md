# 02 — Shopeiva Profile / Backend Convergence

Task: `TB-P05-T015`

| Shopeiva field/feature | Backend before | Backend after | Module | Editable | Binding path | Deferred |
| --- | --- | --- | --- | --- | --- | --- |
| Profile form shell | read-only simplified page | live editable form | Host + CustomerProfile | yes | `/customer-panel/profile` | — |
| name | order recipient fallback | persisted profile | CustomerProfile | yes | `customer-profile-api.ts` → `PUT /v1/customer/profile` | — |
| birthDate | none | persisted optional string | CustomerProfile | yes | same | — |
| bio | none | persisted optional string | CustomerProfile | yes | same | — |
| email | none on profile API | Identity lookup read | Identity | read-only | composer `email` field | verified-change flow |
| mobile | checkout fallback | Identity lookup + fallback | Identity/Order read | read-only | composer `contactMobile` | verified-change flow |
| nationalCode | template only | not implemented | — | read-only | disabled input | KYC |
| address textarea | template only | AddressBook owns addresses | AddressBook | read-only | link to addresses page | profile textarea removed |
| avatar upload | fake client preview | not implemented | — | read-only | disabled camera | media pipeline |
| save button | mock timeout | real API save | CustomerProfile | yes | `saveCustomerProfile` | — |
| dashboard greeting | order name | profile displayName priority | Host composer | reflects save | `/v1/customer/dashboard` | — |

No redesign: Shopeiva grid, grouping, labels, and save CTA structure preserved with Tooba blue token.
