# TB-P10-T004-R24-R1-R3 — Header identity discovery

| Source | CustomerId | Name fields | Mobile | Current header use (before) | Required use |
| --- | --- | --- | --- | --- | --- |
| `/v1/auth/me` | UserId | none | none | only `authenticated` | DisplayName/First/Last/Mobile + label |
| `/api/auth/me` | forwards Host | same | same | same | same |
| CustomerProfile | actor UserId | DisplayName, FirstName, LastName | no | unused by header | canonical name when present |
| Identity contact | UserId | no | Phone DisplayValue | unused by header | mobile fallback |
| Shipping recipient | checkout | First/Last/RecipientName | ContactMobile | unused by header (correct) | must stay unused |
| `StorefrontAccountMenu` | session flag | hardcoded حساب کاربری | no | generic only | `storefrontAccountLabel` |

Defect: header showed generic حساب کاربری whenever authenticated.

Repair: Host `MeResponse` now includes profile name + Identity mobile. One FE `storefrontAccountLabel` on Home/PLP/PDP/Cart/Shipping/Payment/Customer via shared `StorefrontAccountMenu`.
