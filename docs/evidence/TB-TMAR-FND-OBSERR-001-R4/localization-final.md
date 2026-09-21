# Localization final — TB-TMAR-FND-OBSERR-001-R4

Only `RequestLocaleResolver` parses `Accept-Language`. `ApiResponseFactory` passes the header string to that resolver. Offer endpoints do not parse it.

Resolver behavior:

- q-values order candidates. `q<=0` is dropped.
- `*` is ignored, then configured fallback applies.
- Malformed tags that are not cultures are skipped.
- Match order is exact culture, then language parent, then `FallbackCultures`, then `DefaultCulture` (`en`).
- Unknown cultures do not select Persian. Default and fallback are English. There is no FA-first branch and no `StartsWith("en")` / `StartsWith("fa")` language switch.

Resources:

- `FoundationErrors.resx` and `FoundationErrors.fa.resx` cover the three foundation keys.
- `OfferErrors.resx` and `OfferErrors.fa.resx` cover every Offer catalog key, including `{min}` / `{max}`.
- `OfferErrorResourceSet.Owns` is the `offer.` key prefix, not a language branch.
- `ResourceManager` parent fallback supplies `en-US` and `fa-IR` from neutral `en` / `fa`.
- `tr-TR` has no satellite, so the neutral English string is returned. Tests assert it is not Persian.

`OfferEndpointLocalizer` is deleted.

Verdict: PASS.
