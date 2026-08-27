# Settings access control

- Customer: own profile/preference only (actor from session/Dev header).
- Seller: `seller.settings.view` / `seller.settings.manage` via effective Access Control.
- Seller foreign deny: party#view via SellerPanelAccess before capability check.
- Employee mobile-operator seed: no `seller.settings.manage` → PUT 403.
- Admin: own operator profile/preference after AdminPanelAccess.
- Bootstrap reconciles catalog onto seller-owner / platform-admin.
