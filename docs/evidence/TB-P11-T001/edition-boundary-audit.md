# Edition Boundary Audit — TB-P11-T001

Architecture SoT: `docs/architecture/30-tenant-edition-database-foundation.md` — one process = one Edition (`Marketplace` | `SingleStore`); no Store switcher; Single-Store cannot add stores.

| Check | Finding | Evidence |
| --- | --- | --- |
| Store create Admin UI | **Absent** (good) | no `/admin/stores`, no CreateStore admin route |
| Store switcher | **Absent** (good) | `admin-shell.tsx` has no switcher |
| Sellers nav | Always LIVE with `seller.view` — not edition-gated in FE | `admin-shell.tsx` market group |
| Settlement/Payouts | Marketplace copy always in Admin — not edition-hidden | `AdminSettlementScreen` / payouts titles |
| Dashboard sellers metric | Always shown | `AdminDashboardScreen` |
| Auth edition hardcode | Admin/ACC authorization call context forces `ToobaEdition.SingleStore` even if deployment is Marketplace | `AdminPanelAccess.cs`, `AccessControlEndpoints.cs` |
| SalesChannel.Marketplace | Used widely in commerce even under SingleStore test contexts — channel enum ≠ deployment edition | composers/bootstraps |

**Risk:** FE treats marketplace modules as always-on; BE auth path assumes SingleStore edition in call context — edition boundary incomplete at Admin UX + auth context layers.

**LOCK-SF-365** added to encode Single-Store must never expose multi-store creation/management (create/switcher already absent; visibility gating still required).
