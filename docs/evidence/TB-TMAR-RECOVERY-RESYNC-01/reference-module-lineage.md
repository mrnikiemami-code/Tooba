# Reference Module Lineage (Tax / Pricing)

Order on `main` after Offer W1 feat:

1. Offer W1 COMPLETE (`a9e6ee4f`)
2. Tax W1 COMPLETE (`edecccce`) — applied Offer golden pattern; Endpoints+Tests; MapTaxModule; PROVEN_ON_2_MODULES
3. Pricing W1 COMPLETE (`ccc90672`) — applied Offer/Tax pattern; Endpoints+Tests; MapPricingModule; PROVEN_ON_3_MODULES
4. Offer W1-R1 physical repair (`17597ddb`) — did **not** redesign Tax/Pricing; returned Pricing-Reference-State NOT_STARTED_UNTIL_OFFER_REPAIR_ACCEPTED for Architect sequencing

## Pattern authority

- **Offer remains the canonical golden module** (especially after physical lock ARCH-MODULE-PHYSICAL-001).
- Tax/Pricing reused the pattern and shipped COMPLETE historically.
- Offer R1 SoT + Master note: Tax/Pricing **physical recheck** against ARCH-MODULE-PHYSICAL-001 is still Architect-gated / independent later — does not reopen Offer COMPLETE on disk.

## Why they exist

Architect advanced reference-module reuse after Offer W1 Worker PASS, before/while Offer physical reopen/repair narrative. Git history proves Tax/Pricing commits sit between Offer W1 and Offer W1-R1.
