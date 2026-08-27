# 02 — Shopeiva settings source map

Root: D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva

| Surface | URL | Page | Component |
|---|---|---|---|
| Customer profile | /user-panel/profile | src/app/user-panel/profile/page.jsx | components/dashboard/profileForm/profileForm.jsx |
| Customer settings | /user-panel/settings | src/app/user-panel/settings/page.jsx | components/dashboard/settings/settings.jsx |
| Vendor settings | /vendor-panel/settings | src/app/(vendor)/vendor-panel/settings/page.jsx | components/vendor/panel/settings/settings.jsx |
| Admin | none | — | — |

Customer settings tabs: security, notifications, appearance, language (all client mock saves).
Vendor settings tabs: store, profile, notifications, appearance (client mock).
Avatar/password/notif/theme: mock → hide unless Host capability exists.
Store fields (name/phone/email/address): port with real Party-backed API.
Language: Tooba cookie + persist to user preference API.
