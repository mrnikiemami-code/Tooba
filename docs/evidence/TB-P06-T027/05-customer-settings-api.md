# Customer settings API

- Profile: `GET/PUT /v1/customer/profile`
- Preferences (locale only): `GET/PUT /v1/customer/preferences`
- Actor resolution: session Bearer, else Dev header `X-Tooba-Dev-Actor-User-Id`, else storefront guest in Development.
- Own-only; no OwnerUserId in request bodies.
- Unsupported security toggles remain deferred/hidden (see `11-security-settings-decision.md`).
