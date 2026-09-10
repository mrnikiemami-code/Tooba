# TB-P10-T002 — Applicable locks

- LOCK-SF-001 — Shopeiva /shipping structure preserved (hero, steps, address, methods, delivery, notes, summary)
- LOCK-SF-002 / LOCK-SF-006 — checkout money + shipping price backend-authoritative
- LOCK-SF-003 — ATC still Offer+qty only (unchanged from T001)
- LOCK-SF-004 — shipping methods from Store-enabled ShippingService catalog ∩ EnabledCodes
- LOCK-SF-005 — delivery never earlier than backend minimum
- LOCK-OPS-024…028 — cart reservation lifecycle untouched
- Product != Offer; Language Registry dynamic; no Admin fulfillment concepts on storefront
