# R18 permissions

Chosen behavior: Admin can edit Store/Category/Offer. Seller sees effective Offer policy read-only.

`reservation.policy.mutate` is the reserved explicit permission key. It is not present in the Access Control catalog, so Seller PUT `/v1/seller/settings/reservation-policy/offers/{id}` always returns 403 `reservation.policy.seller.denied`.

Backend authorization is authoritative. Frontend does not expose a seller save control.
