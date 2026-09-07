# Reverse Transition Matrix

| From | Action | Allowed when |
| Packed qty | unpack | not in Created allocation and not shipped |
| Shipment Created | cancel_shipment | pre-dispatch only |
| Dispatched/Delivered | reverse pack/cancel | REJECTED |
