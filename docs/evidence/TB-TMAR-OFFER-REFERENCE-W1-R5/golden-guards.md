# golden-guards

Guards in OfferArchitectureGuardTests / OfferPhysicalStructureGuardTests cover:
- Domain→Contracts forbidden
- TypeForwardedTo absent
- foreign Application absent from Offer.Infrastructure
- DbContext absent from Application/Endpoints
- IOfferSellerPanel absent
- obsolete dump files absent
- send-and-ignore CQRS absent
- Persian Domain/Application/Infrastructure absent
- source >800 LOC fail
- NEW: Offer endpoints do not string-match expected exceptions
