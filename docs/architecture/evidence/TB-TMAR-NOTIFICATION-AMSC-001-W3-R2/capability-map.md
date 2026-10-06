# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — capability-map

Capability discovery per Structure skill §5 (business responsibility axes from Endpoints
audience/capability maps and the module's own established names; nothing invented).

| Capability | Audience | Requests (flattened home) |
| --- | --- | --- |
| Customer notifications | Customer storefront | ListCustomerNotificationsQuery, GetCustomerUnreadNotificationCountQuery, MarkCustomerNotificationReadCommand, MarkAllCustomerNotificationsReadCommand, DismissCustomerNotificationCommand |
| Seller notifications | Seller panel | ListSellerNotificationsQuery, GetSellerUnreadNotificationCountQuery, MarkSellerNotificationReadCommand, MarkAllSellerNotificationsReadCommand, DismissSellerNotificationCommand |

Layer placement after repair:

| Layer | Home |
| --- | --- |
| Contracts (cross-module) | `Contracts/Commands/CreateNotificationCommand`, `Ports/INotificationCreationPort`, `Copy/`, `Dtos/`, `Routes/`, `Errors/`, `Resources/` — unchanged |
| Domain | `Aggregates/UserNotification`, `ValueObjects/NotificationRecipientKind` — unchanged |
| Application | `Customer/{Commands,Queries}` + `Seller/{Commands,Queries}` (files directly on the axis) + shared `Composition/` (NotificationOperation), `Models/`, `Ports/`, `Rendering/`, `Validators/` |
| Infrastructure | `DependencyInjection/`, `Directories/`, `Handlers/`, `Messaging/`, `Observability/`, `Projectors/`, `Persistence/Migrations` — unchanged |
| Endpoints | `Customer/`, `Seller/`, `Errors/` + root `NotificationEndpointModule.cs` — unchanged |

The two capabilities mirror the two audience endpoint groups (`/v1/customer/notifications`,
`/v1/seller/notifications`), so capability-first + audience axes are aligned; Commands/Queries are
secondary technical axes under each capability branch. No cross-capability coupling was introduced:
each axis file references only `Composition/`, `Models/`, `Ports/`, `Validators/`, Contracts and
BuildingBlocks.
