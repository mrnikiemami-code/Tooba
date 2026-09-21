# AntiPattern gate — TB-TMAR-FND-OBSERR-001-R1

AntiPattern-Gate: **CLEAN**

Checked touched paths under BuildingBlocks Observability/Presentation/Localization, Offer.Endpoints Seller, Host Errors/Program wiring.

| Check | Result |
| --- | --- |
| polling / magic sleep | CLEAN |
| service locator static ApiResults | CLEAN (rejected) |
| static IHttpContextAccessor holder | CLEAN |
| endpoint global mutable state | CLEAN |
| exception.Message client leak | CLEAN |
| duplicate Activity ownership | CLEAN |
| Offer-specific switch in SafeErrorMapper | CLEAN |
| FA-first locale fallback | CLEAN (default en) |
| AcceptLanguage.Contains in touched Offer paths | CLEAN |
