# R18 policy Admin discovery

| Level | Existing storage | Existing Admin surface | Can override? | Who may edit? | Effective-value display pattern |
| --- | --- | --- | --- | --- | --- |
| Platform | `ReservationCycle` in appsettings / `ReservationCycleOptions` | none (defaults only) | no | Host config | backend preview source=`platform` |
| Store | `StoreHoldPolicySettings` nullable Initial/Retry/Max | Admin Settings → مهلت‌ها (R10 hold-policy) | yes | Admin | hold-policy GET now includes ReservationCycle editor + source |
| Category | `ReservationCyclePolicyOverrides` scope=`category` (R15) | Category workspace had no policy UI | yes | Admin | GET `/v1/admin/settings/reservation-policy/categories/{id}` |
| Offer | `ReservationCyclePolicyOverrides` scope=`offer` (R15) | Product publication commercial + vendor Offer | yes | Admin write; Seller read-only | batched GET `/offers?offerIds=` + seller GET read-only |
| Product | none in R15 | none | no | n/a | not added |

R15 resolver precedence remains Offer > Category > Store > Platform. Category parent-tree is not a policy level.

Existing inherit pattern: hold-policy empty field = platform inherit. R18 uses explicit inherit checkbox that sends null.

No `reservation.policy.mutate` permission exists in Access Control catalog. Seller PUT is 403.
