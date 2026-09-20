# Focused validation

- `dotnet build Tooba.slnx --no-restore`: PASS (0 errors; 3 pre-existing analyzer warnings).
- Offer tests: PASS, 29/29.
- Pricing tests: PASS, 11/11.
- Focused Host Offer/Seller tests: PASS, 18/18.
- Inventory has no dedicated `Tooba.Inventory.Tests` project in the solution; Inventory production compiled in the solution build and its owner boundary is covered by Offer architecture/build validation.
