# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — physical-tree-before

Starting HEAD: `7b8ab79e6f16d4eccd2eeaba5974936c109dba9b` (branch `main`, `HEAD == origin/main`).

Live tree of the four request axes before the Structure repair (16 production `.cs` sources; every
leaf folder is named after one use case and carries only that use case's files):

```text
Tooba.Notification.Application/
Customer/
  Commands/
    DismissCustomerNotification/
      DismissCustomerNotificationCommand.cs
      DismissCustomerNotificationCommandValidator.cs
    MarkAllCustomerNotificationsRead/
      MarkAllCustomerNotificationsReadCommand.cs
    MarkCustomerNotificationRead/
      MarkCustomerNotificationReadCommand.cs
      MarkCustomerNotificationReadCommandValidator.cs
  Queries/
    GetCustomerUnreadNotificationCount/
      GetCustomerUnreadNotificationCountQuery.cs
    ListCustomerNotifications/
      ListCustomerNotificationsQuery.cs
      ListCustomerNotificationsQueryValidator.cs
Seller/
  Commands/
    DismissSellerNotification/
      DismissSellerNotificationCommand.cs
      DismissSellerNotificationCommandValidator.cs
    MarkAllSellerNotificationsRead/
      MarkAllSellerNotificationsReadCommand.cs
    MarkSellerNotificationRead/
      MarkSellerNotificationReadCommand.cs
      MarkSellerNotificationReadCommandValidator.cs
  Queries/
    GetSellerUnreadNotificationCount/
      GetSellerUnreadNotificationCountQuery.cs
    ListSellerNotifications/
      ListSellerNotificationsQuery.cs
      ListSellerNotificationsQueryValidator.cs
```

Classification: Folder-Granularity-State = `OVER_FOLDERED` — ten use-case leaf folders; four of them
hold exactly one production source file (`MarkAllCustomerNotificationsRead`,
`GetCustomerUnreadNotificationCount`, `MarkAllSellerNotificationsRead`,
`GetSellerUnreadNotificationCount`), the remaining six hold only request + validator.
Namespaces used the leaf-qualified form, e.g.
`Tooba.Notification.Application.Customer.Commands.MarkCustomerNotificationRead`.
