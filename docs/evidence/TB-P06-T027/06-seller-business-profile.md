# Seller business profile (Party Organization)

- Owner: `BusinessParty` in Party module.
- New optional fields: Description (1000), SupportPhone (32), SupportEmail (256), AddressLine (512).
- Domain: `UpdateOrganizationProfile(...)` rejects Person kind.
- Contracts: `OrganizationProfileSnapshot` / `OrganizationProfileWrite` + Get/Update on `IPartyDirectory`.
- Migration: `20260827215300_OrganizationProfileFields`.
- Dev seed fills demo seller «فروشگاه آرمان» via `SettingsFoundationDevelopmentSeed`.
