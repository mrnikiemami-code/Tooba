# Tracking preference — T021-R1

Priority for customer primary tracking:
1. Active package with tracking (Created / Dispatched / Delivered), newest UpdatedAt
2. Else active package without tracking (preparing)
3. Cancelled packages never primary
4. No active package → existing member shipment tracking only

Member shipment rows remain in fulfillments payload; PreferredTrackingReference stamped when central package wins.
