# Canonical semantic error pattern
- Types: SemanticError, SemanticException
- Project: Tooba.BuildingBlocks (TmarFoundation.cs)
- Codes: stable dotted strings (e.g. offer.archived.cannot_activate)
- Domain/Application/Contracts throw SemanticException(code)
- Host OfferSemanticLocalizer + Offer Endpoint localizer map FA/EN titles
- PlatformExceptionMapper maps SemanticException → 400 + errorCode
