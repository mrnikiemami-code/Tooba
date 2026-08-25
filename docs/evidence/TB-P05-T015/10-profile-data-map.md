# 10 — Profile Data Map

Task: `TB-P05-T015`

```text
Shopeiva profile/page.tsx (customer-panel/profile)
  → customer-profile-api.saveCustomerProfile()
  → PUT /v1/customer/profile (CustomerPanelEndpoints)
  → CustomerPanelComposer.UpdateProfileAsync()
  → ICustomerProfileDirectory.UpsertAsync(actor)
  → customer_profile.customer_profiles (OwnerUserId PK)

GET /v1/customer/profile
  → CustomerPanelComposer.GetProfileAsync()
  → ICustomerProfileDirectory.GetAsync(actor)
  → IIdentityContactLookup.GetContactAsync(actor)  [email/mobile read-only]
  → latest Order checkout snapshot [lastShippingAddress fallback]
```

Write DTO: `{ displayName, firstName?, lastName?, birthDate?, bio? }`

Read DTO adds: `email`, `contactMobile`, flags `emailEditable=false`, `mobileEditable=false`, `editable=true`.

Dashboard read path uses stored `displayName` when profile exists.
