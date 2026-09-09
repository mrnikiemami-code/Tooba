# Split delivery — TB-P09-T019

Existing `LineDeliverySlice` clocks reused. Two pre-dispatch shipments 0.50 then 0.75 of 1.25 kg: first delivery does not start the clock for undelivered 0.75. Requesting 0.75 after only 0.50 delivered is rejected (`return.quantity_exceeded`); after second delivery 0.75 is accepted.
