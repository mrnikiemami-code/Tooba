# Support CQRS Audit

## Before
- `Commands/SupportCommands.cs` and `Queries/SupportQueries.cs` were dumping-ground DTO records (not MediatR)
- No handlers; Host called `ISupportDirectory` directly

## After
### Directory input Models (not MediatR)
- `Models/SupportDirectoryInputs.cs`: `CreateTicketCommand`, `ReplyTicketCommand`, `AdminTicketPatchCommand`, `AudienceTicketListQuery`, `AdminTicketListQuery`

### MediatR Queries (use-case folders)
- ListCustomerTickets, GetCustomerTicket
- ListSellerTickets, GetSellerTicket
- ListAdminTickets, GetAdminTicket
- GetSupportDemoPreview

### MediatR Commands (use-case folders)
- Create/Reply/Close/Reopen Customer
- Create/Reply/Close/Reopen Seller
- ReplyAdminTicket, PatchAdminTicket

### Registration
- `Program.cs` `AddToobaCqrsFoundation(... typeof(CreateCustomerTicketCommand).Assembly)`

### Dump files
- `SupportCommands.cs` **deleted**
- `SupportQueries.cs` **deleted**

## Verdict
**Support-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS**
