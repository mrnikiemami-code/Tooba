# behavior-parity — TB-TMAR-HOST-SECURITY-AMC-001-W1

| Path | Before | After |
| --- | --- | --- |
| Missing actor | 401 + seller.actor.missing | same code via SemanticException |
| Invalid seller party | 400 + seller.identity.missing | same |
| Auth unavailable | 503 + seller.authorization.unavailable | same |
| Panel deny | 403 + seller.authorization.denied | same |
| Party capability deny | 403 + seller.authorization.denied | same (no Party-specific code) |
| Support capability deny | 403 + seller.authorization.denied | same |
| Order ResolveAsync failure | SemanticError? with code | same via SemanticException.Error |
| DevActorHeader | Development-only, session wins | preserved |
| SellerPartyHeader | context only | preserved |

Fail-open branches introduced: **none**.
