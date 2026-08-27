# 07 — Authorization

- Customer: own wallet / own order / own return only (existing Host actor filters)
- Seller: approve/reject returns for own seller party; no customer wallet authority
- Admin: refund retry + wallet.view / wallet.adjust capabilities
- Tenant isolation via CommerceContext / module schemas

Foreign customer spend with empty wallet fails closed (insufficient balance / no funds).
