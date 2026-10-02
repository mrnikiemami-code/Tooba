# Localization / semantic audit

## Violations

- Domain/Infra: dozens of Persian `InvalidOperationException` messages used as business failure channel
- Application: `StoryFailureMapper` classifies by substring on those messages
- Composer: `RequireTenantId` still throws FA `Tenant resolve نشده است` then maps

## Canonical target

- Stable codes in `Tooba.Story.Contracts.Errors.StoryErrorCodes`
- Failures = `SemanticException(SemanticError(code))` (or Result — Offer strategy; Story keeps SemanticException for behavior parity with current Endpoints)
- User text only from resx via error catalog localizer
- XML/doc comments FA OK; seed fixture titles OK; **not** exception message channel
