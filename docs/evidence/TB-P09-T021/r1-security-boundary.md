# Security boundary — T021-R1

- Owned actor → allowed
- Valid guest secret bound to checkout CartId → allowed
- Wrong guest secret → 404 `customer.order.missing`
- Unowned actor without matching secret → 404
- No anonymous GUID oracle: checkout must exist AND proof must match
- Production without session and without guest secret → 401 `customer.actor.missing`
- Does not open unrestricted public order lookup
