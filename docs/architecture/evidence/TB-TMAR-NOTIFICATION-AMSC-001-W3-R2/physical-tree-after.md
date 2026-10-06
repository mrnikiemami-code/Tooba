# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — physical-tree-after

Repaired tree at the W3-R2 commit (all ten use-case leaf folders flattened into their existing
Commands/Queries axes; zero child directories under the four request axes):

```text
Tooba.Notification.Application/
Composition/                      (shared typed-fault seam)
Customer/
  Commands/
    DismissCustomerNotificationCommand.cs
    DismissCustomerNotificationCommandValidator.cs
    MarkAllCustomerNotificationsReadCommand.cs
    MarkCustomerNotificationReadCommand.cs
    MarkCustomerNotificationReadCommandValidator.cs
  Queries/
    GetCustomerUnreadNotificationCountQuery.cs
    ListCustomerNotificationsQuery.cs
    ListCustomerNotificationsQueryValidator.cs
Models/                           (shared HTTP response models + recipient-kind mapping)
Ports/                            (INotificationDirectory)
Rendering/                        (NotificationCopy)
Seller/
  Commands/
    DismissSellerNotificationCommand.cs
    DismissSellerNotificationCommandValidator.cs
    MarkAllSellerNotificationsReadCommand.cs
    MarkSellerNotificationReadCommand.cs
    MarkSellerNotificationReadCommandValidator.cs
  Queries/
    GetSellerUnreadNotificationCountQuery.cs
    ListSellerNotificationsQuery.cs
    ListSellerNotificationsQueryValidator.cs
Validators/                       (NotificationValidationCodes)
```

Mechanics: 16 `git mv` operations (no content change beyond namespace), the ten emptied leaf
directories deleted, and the two stale `Customer/Validators` / `Seller/Validators` leftover shells
removed. Reference repoints: `NotificationCustomerEndpoints.cs`, `NotificationSellerEndpoints.cs`
(usings → axis namespaces), `Tooba.Host/Program.cs` CQRS scan anchor (fully-qualified
`...Customer.Commands.MarkCustomerNotificationRead.MarkCustomerNotificationReadCommand` →
`...Customer.Commands.MarkCustomerNotificationReadCommand`), and the module behavior test usings.
No route, DTO, validator-semantic, DI, schema, or error-code change.
