# Locale foundation — TB-TMAR-FND-OBSERR-001-R1

## Central resolver

- Type: `IRequestLocaleResolver` / `RequestLocaleResolver`
- Options: `RequestLocaleOptions` (`Tooba:Localization:RequestLocale`)
  - `DefaultCulture` = `en` (not FA-first)
  - `FallbackCultures` configurable chain
  - `SupportedCultures` optional allow-list (empty = unlimited .NET cultures)

## Behavior

- Parses Accept-Language with quality values
- Canonical `CultureInfo` selection
- Malformed headers fall back safely
- Consumed by `ApiResponseFactory`

## Offer path

- Removed `AcceptLanguage.Contains("en")` ad-hoc parsing from Offer touched paths
- `OfferEndpointLocalizer` now takes `CultureInfo` from central resolver via `OfferErrorMessageContributor`
- Residual: Offer bilingual catalog still uses en vs non-en message pairs (not FA-first resolver). Full Localization SSOT resource catalog deferred to R2/R3.
