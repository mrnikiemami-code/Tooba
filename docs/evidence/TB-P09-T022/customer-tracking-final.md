# Customer tracking final — TB-P09-T022

`/v1/customer/orders/{checkoutId}/fulfillments`:

- Authenticated owned customer (`placed_by_user_id`) → 200; preferred = active central tracking
- Guest: correct `X-Tooba-Guest-Secret` + cart ownership → 200
- Wrong secret / GuestActor alone / unowned actor → 404 (no leakage)
- Preference: latest active Created/Dispatched/Delivered package; Cancelled never primary
- Rebuild: new active package replaces cancelled as primary
- No package → legacy seller shipment tracking unchanged (T021-R1)
