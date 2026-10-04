# TB-TMAR-FULFILLMENT-AMSC-001 — W2 physical tree (before → after)

## Before (technical-axis-first + over-foldered)

```text
Tooba.Fulfillment.Application/
  Commands/
    CreateShippingService/CreateShippingServiceCommand.cs
    DeactivateShippingService/DeactivateShippingServiceCommand.cs
    EnsureShippingCatalogSeed/EnsureShippingCatalogSeedCommand.cs
    ExecuteAdminFulfillmentBulk/ExecuteAdminFulfillmentBulkCommand.cs
    SellerMutateFulfillment/SellerMutateFulfillmentCommand.cs
    UpdateShippingService/UpdateShippingServiceCommand.cs
  Queries/
    GetAdminFulfillment/GetAdminFulfillmentQuery.cs
    GetSellerFulfillment/GetSellerFulfillmentQuery.cs
    GetShippingService/GetShippingServiceQuery.cs
    ListAdminFulfillments/ListAdminFulfillmentsQuery.cs
    ListCustomerCheckoutFulfillments/ListCustomerCheckoutFulfillmentsQuery.cs
    ListEnabledShippingMethodsTree/ListEnabledShippingMethodsTreeQuery.cs
    ListSellerFulfillments/ListSellerFulfillmentsQuery.cs
    ListShippingServices/ListShippingServicesQuery.cs
    QueryAdminFulfillmentWorkQueue/QueryAdminFulfillmentWorkQueueQuery.cs
  Models/     (10 files, technical axis)
  Ports/      (4 files, technical axis)
  Validators/
    Admin/    (3 files)
    Customer/ (1 file)
    Seller/   (2 files)
    Shipping/ (4 files)
    FulfillmentFluentRules.cs
    FulfillmentValidationCodes.cs
  Errors/     (2 files)
  Shipping/   (3 files: two mixed *Contracts.cs bundles + mis-named ShippingMethodRegistry.cs)
```

Defects: 6 single-file Command leaves, 9 single-file Query leaves, technical-axis-first
primary organization, audience-axis validator split, root-level `Models`/`Ports`.

## After (capability-first, shallow, professional)

```text
Tooba.Fulfillment.Application/
  Checkout/
    Queries/ListCustomerCheckoutFulfillmentsQuery.cs
    Validators/ListCustomerCheckoutFulfillmentsQueryValidator.cs
  Errors/
    FulfillmentErrors.cs
    FulfillmentExceptionMapper.cs
  Fulfillments/
    Commands/SellerMutateFulfillmentCommand.cs
    Models/   (9 files)
    Ports/    (4 files)
    Queries/  (4 files)
    Validators/(3 files)
  Shipping/
    Commands/ (4 files)
    Ports/IShippingServiceDirectory.cs
    Queries/  (3 files)
    Validators/(4 files)
    ShippingProviderMetadata.cs
    ShippingServiceReadModels.cs
    ShippingServiceSemantic.cs
    ShippingServiceWriteModels.cs
  Validators/
    FulfillmentFluentRules.cs
    FulfillmentValidationCodes.cs
  WorkQueue/
    Commands/ (1 file)
    Models/   (1 file)
    Queries/  (1 file)
    Validators/(2 files)
```

## Infrastructure

```text
Tooba.Fulfillment.Infrastructure/Directories/
  FulfillmentDirectory.cs              1214 LOC  ->  794 LOC  (lifecycle surface)
  FulfillmentDirectory.Packages.cs     (new)    ->  440 LOC  (consolidated-package surface)
```

Both partials are under the ARCH-SIZE-001 800 ceiling.
