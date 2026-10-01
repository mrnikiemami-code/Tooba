# Failure semantics — Localization AMC-001-R1

Owner layers throw `SemanticException(new SemanticError(LanguageErrorCodes.*))`:

- Localization.Domain.Language
- Localization.Application.LanguageMappings
- Localization.Infrastructure.LanguageDirectory

Endpoints map only:

- PlatformHttpException → ApiResponseFactory.FromPlatformException
- SemanticException → ApiResponseFactory.FromSemanticException

Unknown exceptions propagate.
