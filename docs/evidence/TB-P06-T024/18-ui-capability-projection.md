# 18 — UI Capability Projection

LIVE foundation:
- Access Control nav items on Admin + Vendor shells
- ACC pages respect `canManage` for mutations
- Seller catalog marks `disabledByCeiling` / `platformOnly`

PARTIAL: not every product/order action button is wired to fine-grained permission ids yet (panel `tenant#view`/`party#view` remains entry gate; backend ACC APIs enforce capability + ceiling).
Backend still returns 403 for unauthorized ACC mutations.
