# 01 — Profile Field Ownership Inventory

Task: `TB-P05-T015`

| Field (Shopeiva) | Meaning | Backend before | Owning module after | Editable | Binding |
| --- | --- | --- | --- | --- | --- |
| name (نام و نام خانوادگی) | Display / derived first+last | Order snapshot only | CustomerProfile | yes | `PUT /v1/customer/profile` → `displayName` |
| birthDate | Optional birth date text | none | CustomerProfile | yes | `birthDate` |
| bio | Optional biography | none | CustomerProfile | yes | `bio` (max 200) |
| email | Login identifier | Identity LoginIdentifier | Identity | read-only | `IIdentityContactLookup` |
| phone/mobile | Login identifier | Identity / order fallback | Identity | read-only | `IIdentityContactLookup` + checkout fallback |
| nationalCode | KYC placeholder | none | deferred | read-only | honest disabled UI |
| address | Shipping address | AddressBook + order snapshot | AddressBook | read-only on profile | link to `/customer-panel/addresses` |
| avatar | Profile image | none | deferred | read-only | camera disabled; no fake upload |
| gender | Schema only in Shopeiva reference | none | deferred | n/a | not rendered in reference JSX |
| password | Security | Identity auth endpoints | Identity | out of scope on profile page | `/v1/auth/password-change` exists; not bound here |

Principles preserved:

- Identity/User ≠ CustomerProfile descriptive data
- No credential or login-identifier mutation through profile save
- No cross-module SQL joins
