# R16 ownership

- Authenticated / Dev-actor: `PlacedByUserId` only; proofs are not used to list other Orders
- Guest: only `proofs[]` with matching `checkoutId` + committed `cartId` + `TryGetForOwnershipAsync`
- Wrong proof / new Active Cart secret: omitted (denied, no enumeration)
- FE list POST does not send Active Cart `readCartSession()` as ownership
- Pay/retry uses `resolveCommittedCheckoutAccess` / committed proofs (R13)
- Guest limitation: only proofs stored in this browser session; multiple keys supported (not first-order-only)
