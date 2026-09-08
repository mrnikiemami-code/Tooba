# Lifecycle sequence

ReadyToFulfill: `mark_processing` only. Pack throws `fulfillment.pack.requires_processing`.
Processing: pack eligible qty. Seller status stays Processing until every line is fully packed.
Packed: CreateShipment / T005 unpack. No skip stages.
