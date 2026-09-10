# Multi-seller isolation — TB-P09-T022

- Package members must be distinct SellerPartyIds on the same checkout
- No cross-order members (`mixed_checkout`)
- No cross-seller shipment ownership corruption
- Filtered unique index: one shipment cannot join two active packages (`ReleasedAt IS NULL`)
- Concurrency: second create with overlapping active membership → `shipment_already_member`
- Seller-specific shipment history remains intact after package lifecycle
