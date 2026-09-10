# TB-P09-T022-R2 — authorization negative proof

Owned checkout: `01a089a6-154f-7000-9586-bc1463ac6c8b`  
Endpoint: `GET /v1/customer/orders/{checkoutId}/fulfillments` (same ownership gate used by BFF `/api/customer/...`)

| Scenario | Proof | HTTP |
| --- | --- | --- |
| Foreign Dev-Actor | `X-Tooba-Dev-Actor-User-Id: cccccccc-cccc-4ccc-8ccc-0000000000cc` (not owner) | **404** |
| No session / guest actor alone | no Bearer, no guest secret | **404** |
| Owned Bearer session | fixture customer access token / BFF `tooba_session` | **200** |

Recorded on fixture as `foreignStatus: 404`, `guestActorAloneStatus: 404`.

## Conventions preserved

- Fail closed with **404** (no existence leakage beyond current security convention)
- No auth bypass introduced
- No weakening of guest-secret or ownership checks

Error UX on denied path: no raw GUID/enum/exception dump in Customer UI for the owned session path tested.
