# BulkInquiry physical tree (before AMC-001)

```
Modules/BulkInquiry/
  Tooba.BulkInquiry.Domain/
    BulkPurchaseInquiry.cs              # aggregate + BulkInquiryStatus enum
  Tooba.BulkInquiry.Application/
    BulkInquiryContracts.cs             # SubmitBulkInquiryRequest + IBulkInquiryDirectory
    Commands/SubmitBulkInquiryCommand.cs # command + handler + validator
  Tooba.BulkInquiry.Contracts/
    Errors/BulkInquiryErrorCodes.cs     # Rejected only
  Tooba.BulkInquiry.Infrastructure/
    BulkInquiryModule.cs                # + OutboxRegistration
    BulkInquiryDirectory.cs
    Persistence/BulkInquiryDbContext.cs
    Migrations/…                        # root Migrations (non-canonical)
  Tooba.BulkInquiry.Endpoints/
    BulkInquiryEndpointModule.cs
    Storefront/BulkInquiryStorefrontEndpoints.cs  # Results.Json + catch SemanticException
    Errors/ + Resources/
```

slnx: Domain/Application/Infrastructure under flat `/Modules/`; Contracts + Endpoints absent.
