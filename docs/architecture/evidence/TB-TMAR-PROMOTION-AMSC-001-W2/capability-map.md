# TB-TMAR-PROMOTION-AMSC-001-W2 — capability map

Capabilities are business responsibility axes (not invented folder names). Promotion is a
two-capability HTTP-owning module; the W2 tree is capability-first.

| Capability | Business responsibility | Application home | Domain | Endpoints | Contracts |
| --- | --- | --- | --- | --- | --- |
| **Promotions** | Promotion definition/lifecycle/evaluation for seller + admin: create/update/activate/deactivate, coupon, stacking, priority, effective window, eligibility facts, rounding | `Application/Promotions/{Commands,Queries,Models,Ports}` | `Domain/Aggregates/PromotionDefinition.cs`, `Domain/Policies`, `Domain/ValueObjects`, `Domain/Events` | `Endpoints/Seller/PromotionSellerEndpoints.cs` (6 routes), `Endpoints/Admin/PromotionAdminEndpoints.cs` (3 routes) | `Contracts/Checkout/CheckoutPromotionContracts.cs`, `Contracts/Errors` |
| **Merchandising** | Merchandising campaign definition/lifecycle/membership/translation for admin | `Application/Merchandising/{Ports,Models,Admin/{Commands,Queries}}` | `Domain/Merchandising/*` | `Endpoints/Admin/MerchandisingCampaignAdminEndpoints.cs` (12 routes) | `Contracts/Merchandising/*` |

Shared (cross-capability) homes, justified by the repository pattern and kept at the capability root:

| Home | Responsibility | Why shared |
| --- | --- | --- |
| `Application/Checkout/CheckoutPromotionAdapter.cs` | Checkout-facing promotion adapter | Consumed by the Cart/Checkout flow, not owned by either capability |
| `Application/Composition/PromotionOperation.cs` | Single typed-fault → `Result` seam | Used by both capabilities |
| `Application/Validation/*` | Transport validators + validation codes | One assembly-wide validator registration surface |
| `Contracts/Errors/*`, `Contracts/Resources/*` | Stable codes + bilingual resources | One module-wide code/resource keyspace (`promotion.` / `merchandising.` / `campaign.`) |

Route ownership evidence (unchanged by W2, structure only):

```text
/v1/seller/promotions                    6 routes  Endpoints/Seller
/v1/admin/promotions                     3 routes  Endpoints/Admin
/v1/admin/merchandising-campaigns       12 routes  Endpoints/Admin
                                        --------
                                        21 module-owned routes, Host route count ZERO
```

Capability names were taken from the module's own existing vocabulary (`Promotions`, `Merchandising`)
and the existing `Endpoints/{Admin,Seller}` audience folders — no name was mechanically invented from a
command type name.
