# Fulfillment Physical Tree

| path | namespace | responsibility |
|---|---|---|
| Tooba.Fulfillment.Application/Commands/CreateShippingService/CreateShippingServiceCommand.cs | Tooba.Fulfillment.Application.Commands.CreateShippingService | production type |
| Tooba.Fulfillment.Application/Commands/DeactivateShippingService/DeactivateShippingServiceCommand.cs | Tooba.Fulfillment.Application.Commands.DeactivateShippingService | production type |
| Tooba.Fulfillment.Application/Commands/EnsureShippingCatalogSeed/EnsureShippingCatalogSeedCommand.cs | Tooba.Fulfillment.Application.Commands.EnsureShippingCatalogSeed | production type |
| Tooba.Fulfillment.Application/Commands/ExecuteAdminFulfillmentBulk/ExecuteAdminFulfillmentBulkCommand.cs | Tooba.Fulfillment.Application.Commands.ExecuteAdminFulfillmentBulk | اجرای گروهی صف کار Fulfillment. |
| Tooba.Fulfillment.Application/Commands/SellerMutateFulfillment/SellerMutateFulfillmentCommand.cs | Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment | Snapshot مجوز که Host از AccessControl می‌سازد. |
| Tooba.Fulfillment.Application/Commands/UpdateShippingService/UpdateShippingServiceCommand.cs | Tooba.Fulfillment.Application.Commands.UpdateShippingService | production type |
| Tooba.Fulfillment.Application/Errors/FulfillmentExceptionMapper.cs | Tooba.Fulfillment.Application.Errors | /// Maps known Fulfillment/Shipping domain/directory stable machine codes to Sem |
| Tooba.Fulfillment.Application/Models/ActivePackageMembershipSnapshot.cs | Tooba.Fulfillment.Application.Models | /// عضویت فعال مرسوله در بسته تجمیعی. /// |
| Tooba.Fulfillment.Application/Models/AdminFulfillmentWorkQueueModels.cs | Tooba.Fulfillment.Application.Models | /// ردیف صف کار ارسال و تحویل Admin — fulfillment/seller-group نه Customer Order |
| Tooba.Fulfillment.Application/Models/ConsolidatedPackageMemberSnapshot.cs | Tooba.Fulfillment.Application.Models | /// عضو بسته تجمیعی در snapshot. /// |
| Tooba.Fulfillment.Application/Models/ConsolidatedPackageSnapshot.cs | Tooba.Fulfillment.Application.Models | /// snapshot بسته تجمیعی. /// |
| Tooba.Fulfillment.Application/Models/FulfillmentItemSnapshot.cs | Tooba.Fulfillment.Application.Models | /// snapshot خط fulfillment. /// |
| Tooba.Fulfillment.Application/Models/FulfillmentSelectionCommand.cs | Tooba.Fulfillment.Application.Models | /// انتخاب خط/تعداد برای عملیات seller-scoped. /// |
| Tooba.Fulfillment.Application/Models/FulfillmentSnapshot.cs | Tooba.Fulfillment.Application.Models | /// snapshot خواندنی fulfillment. /// |
| Tooba.Fulfillment.Application/Models/ShipmentLineCommand.cs | Tooba.Fulfillment.Application.Models | /// خط محموله در فرمان. /// |
| Tooba.Fulfillment.Application/Models/ShipmentLineSnapshot.cs | Tooba.Fulfillment.Application.Models | /// snapshot خط محموله. /// |
| Tooba.Fulfillment.Application/Models/ShipmentSnapshot.cs | Tooba.Fulfillment.Application.Models | /// snapshot محموله. /// |
| Tooba.Fulfillment.Application/Ports/IFulfillmentDirectory.cs | Tooba.Fulfillment.Application.Ports | /// ارکستراسیون fulfillment. /// |
| Tooba.Fulfillment.Application/Ports/IFulfillmentInventoryGateway.cs | Tooba.Fulfillment.Application.Ports | /// درز موجودی برای dispatch؛ Fulfillment مستقیم Inventory DbContext باز نمی‌کند |
| Tooba.Fulfillment.Application/Ports/IFulfillmentUseCaseGuard.cs | Tooba.Fulfillment.Application.Ports | /// نگهبان use-case fulfillment. /// |
| Tooba.Fulfillment.Application/Ports/ISellerFulfillmentAuthorizer.cs | Tooba.Fulfillment.Application.Ports | Snapshot مجوز order.handle که Host از AccessControl می‌سازد (بدون وابستگی Access |
| Tooba.Fulfillment.Application/Queries/GetAdminFulfillment/GetAdminFulfillmentQuery.cs | Tooba.Fulfillment.Application.Queries.GetAdminFulfillment | production type |
| Tooba.Fulfillment.Application/Queries/GetSellerFulfillment/GetSellerFulfillmentQuery.cs | Tooba.Fulfillment.Application.Queries.GetSellerFulfillment | production type |
| Tooba.Fulfillment.Application/Queries/GetShippingService/GetShippingServiceQuery.cs | Tooba.Fulfillment.Application.Queries.GetShippingService | production type |
| Tooba.Fulfillment.Application/Queries/ListAdminFulfillments/ListAdminFulfillmentsQuery.cs | Tooba.Fulfillment.Application.Queries.ListAdminFulfillments | production type |
| Tooba.Fulfillment.Application/Queries/ListCustomerCheckoutFulfillments/ListCustomerCheckoutFulfillmentsQuery.cs | Tooba.Fulfillment.Application.Queries.ListCustomerCheckoutFulfillments | پاسخ HTTP پایدار لیست fulfillment مشتری برای یک checkout. |
| Tooba.Fulfillment.Application/Queries/ListEnabledShippingMethodsTree/ListEnabledShippingMethodsTreeQuery.cs | Tooba.Fulfillment.Application.Queries.ListEnabledShippingMethodsTree | گزینهٔ سطح ۲ درخت روش ارسال فعال (شکل JSON پایدار برای UI). |
| Tooba.Fulfillment.Application/Queries/ListSellerFulfillments/ListSellerFulfillmentsQuery.cs | Tooba.Fulfillment.Application.Queries.ListSellerFulfillments | فهرست fulfillment فروشنده. |
| Tooba.Fulfillment.Application/Queries/ListShippingServices/ListShippingServicesQuery.cs | Tooba.Fulfillment.Application.Queries.ListShippingServices | production type |
| Tooba.Fulfillment.Application/Queries/QueryAdminFulfillmentWorkQueue/QueryAdminFulfillmentWorkQueueQuery.cs | Tooba.Fulfillment.Application.Queries.QueryAdminFulfillmentWorkQueue | Module-owned fulfillment work-queue Normalize — whitelist + default sort updated |
| Tooba.Fulfillment.Application/Shipping/IShippingCatalogReader.cs | Tooba.Fulfillment.Application.Shipping | ترجمه سرویس ارسال برای خواندن کاتالوگ. |
| Tooba.Fulfillment.Application/Shipping/ShippingMethodRegistry.cs | Tooba.Fulfillment.Application.Shipping | /// تعریف روش ارسال فعال در رجیستری فروشگاه. /// |
| Tooba.Fulfillment.Application/Shipping/ShippingServiceReadContracts.cs | Tooba.Fulfillment.Application.Shipping | ردیف لیست سرویس ارسال (شکل JSON پایدار). |
| Tooba.Fulfillment.Application/Shipping/ShippingServiceWriteContracts.cs | Tooba.Fulfillment.Application.Shipping | درز موقت اعتبار LanguageId و فهرست seed بدون وابستگی Fulfillment به Localization |
| Tooba.Fulfillment.Contracts/Errors/FulfillmentErrorCodes.cs | Tooba.Fulfillment.Contracts.Errors | Stable semantic error codes owned by Fulfillment. |
| Tooba.Fulfillment.Contracts/Events/FulfillmentCreatedIntegrationEvent.cs | Tooba.Fulfillment.Contracts.Events | /// رویداد Outbox fulfillment.created.v1 /// |
| Tooba.Fulfillment.Contracts/Events/ShipmentDispatchedIntegrationEvent.cs | Tooba.Fulfillment.Contracts.Events | /// رویداد Outbox shipment.dispatched.v1 /// |
| Tooba.Fulfillment.Contracts/Returns/FulfillmentReturnContracts.cs | Tooba.Fulfillment.Contracts.Returns | /// برش تحویل یک خط در یک مرسولهٔ Delivered (ساعت مرجوعی per-slice). /// |
| Tooba.Fulfillment.Domain/Aggregates/ConsolidatedPackage.cs | Tooba.Fulfillment.Domain.Aggregates | /// بسته تجمیعی مرکزی روی مرسوله‌های چند فروشنده از یک Checkout. /// |
| Tooba.Fulfillment.Domain/Aggregates/ConsolidatedPackageMember.cs | Tooba.Fulfillment.Domain.Aggregates | /// عضو بسته تجمیعی (مرسولهٔ فروشنده). /// |
| Tooba.Fulfillment.Domain/Aggregates/FulfillmentItem.cs | Tooba.Fulfillment.Domain.Aggregates | /// خط fulfillment با snapshot تعداد سفارش. /// |
| Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs | Tooba.Fulfillment.Domain.Aggregates | /// واحد fulfillment برای یک SellerOrder پرداخت‌شده. /// |
| Tooba.Fulfillment.Domain/Aggregates/Shipment.cs | Tooba.Fulfillment.Domain.Aggregates | /// محموله fulfillment. چند محموله برای یک fulfillment مجاز است. /// |
| Tooba.Fulfillment.Domain/Aggregates/ShipmentItem.cs | Tooba.Fulfillment.Domain.Aggregates | /// خط محموله. /// |
| Tooba.Fulfillment.Domain/Aggregates/ShippingService.cs | Tooba.Fulfillment.Domain.Aggregates | سرویس ارسال سطح والد (مثلاً پست، تیپاکس). |
| Tooba.Fulfillment.Domain/Aggregates/ShippingServiceOption.cs | Tooba.Fulfillment.Domain.Aggregates | نوع/گزینهٔ سرویس سطح فرزند (مثلاً پیشتاز، معمولی). |
| Tooba.Fulfillment.Domain/Aggregates/ShippingServiceOptionTranslation.cs | Tooba.Fulfillment.Domain.Aggregates | ترجمهٔ نوع سرویس فرزند. |
| Tooba.Fulfillment.Domain/Aggregates/ShippingServiceTranslation.cs | Tooba.Fulfillment.Domain.Aggregates | ترجمهٔ نام سرویس ارسال والد. |
| Tooba.Fulfillment.Domain/Events/FulfillmentCreatedDomainEvent.cs | Tooba.Fulfillment.Domain.Events | رویداد ایجاد fulfillment. |
| Tooba.Fulfillment.Domain/Events/FulfillmentLinePackedDomainEvent.cs | Tooba.Fulfillment.Domain.Events | رویداد بسته‌بندی خط/تعداد. |
| Tooba.Fulfillment.Domain/Events/FulfillmentLineUnpackedDomainEvent.cs | Tooba.Fulfillment.Domain.Events | رویداد بازگشت از بسته‌بندی. |
| Tooba.Fulfillment.Domain/Events/ShipmentCancelledDomainEvent.cs | Tooba.Fulfillment.Domain.Events | رویداد ابطال مرسوله پیش از ارسال. |
| Tooba.Fulfillment.Domain/Events/ShipmentCreatedDomainEvent.cs | Tooba.Fulfillment.Domain.Events | رویداد ایجاد محموله. |
| Tooba.Fulfillment.Domain/Events/ShipmentDeliveredDomainEvent.cs | Tooba.Fulfillment.Domain.Events | رویداد تحویل محموله. |
| Tooba.Fulfillment.Domain/Events/ShipmentDispatchedDomainEvent.cs | Tooba.Fulfillment.Domain.Events | رویداد dispatch محموله. |
| Tooba.Fulfillment.Domain/Events/ShipmentTrackingCorrectedDomainEvent.cs | Tooba.Fulfillment.Domain.Events | رویداد اصلاح کد رهگیری پیش از ارسال. |
| Tooba.Fulfillment.Domain/ValueObjects/ConsolidatedPackageStatus.cs | Tooba.Fulfillment.Domain.ValueObjects | /// وضعیت بسته تجمیعی چندفروشنده‌ای. لایهٔ ارکستراسیون است، نه جایگزین Shipment. |
| Tooba.Fulfillment.Domain/ValueObjects/FulfillmentStatus.cs | Tooba.Fulfillment.Domain.ValueObjects | /// وضعیت واحد fulfillment. با وضعیت تجاری Order یکی نیست. /// |
| Tooba.Fulfillment.Domain/ValueObjects/ShipmentStatus.cs | Tooba.Fulfillment.Domain.ValueObjects | /// وضعیت محموله. Order نیست. /// |
| Tooba.Fulfillment.Endpoints/FulfillmentEndpointModule.cs | Tooba.Fulfillment.Endpoints | Thin composition for Fulfillment HTTP ownership. |
| Tooba.Fulfillment.Endpoints/Admin/FulfillmentAdminEndpoints.cs | Tooba.Fulfillment.Endpoints.Admin | Thin admin Fulfillment HTTP routes. |
| Tooba.Fulfillment.Endpoints/Admin/IFulfillmentAdminAuthorizer.cs | Tooba.Fulfillment.Endpoints.Admin | Neutral admin auth seam for Fulfillment Endpoints (Host implements). |
| Tooba.Fulfillment.Endpoints/Customer/FulfillmentCustomerEndpoints.cs | Tooba.Fulfillment.Endpoints.Customer | Thin customer Fulfillment HTTP routes. |
| Tooba.Fulfillment.Endpoints/Customer/IFulfillmentCustomerAuthorizer.cs | Tooba.Fulfillment.Endpoints.Customer | Neutral customer/guest ownership seam for checkout fulfillments. |
| Tooba.Fulfillment.Endpoints/Seller/FulfillmentSellerEndpoints.cs | Tooba.Fulfillment.Endpoints.Seller | Wire-only shipment line. |
| Tooba.Fulfillment.Endpoints/Seller/IFulfillmentSellerAuthorizer.cs | Tooba.Fulfillment.Endpoints.Seller | Neutral seller auth seam for Fulfillment Endpoints (Host implements). |
| Tooba.Fulfillment.Endpoints/Shipping/ShippingMethodsEndpoints.cs | Tooba.Fulfillment.Endpoints.Shipping | Enabled shipping-methods tree — formerly Host AdminOrderOperations. |
| Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs | Tooba.Fulfillment.Endpoints.Shipping | Wire-only translation DTO. |
| Tooba.Fulfillment.Infrastructure/Bridges/FulfillmentReturnBridge.cs | Tooba.Fulfillment.Infrastructure.Bridges | /// snapshot تحویل برای Returns بدون cross-DbContext. /// |
| Tooba.Fulfillment.Infrastructure/Bridges/FulfillmentSellerOrderCancelGate.cs | Tooba.Fulfillment.Infrastructure.Bridges | /// درز Fulfillment برای قاعدهٔ لغو Paid پیش از محموله در Order. /// |
| Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs | Tooba.Fulfillment.Infrastructure.DependencyInjection | /// ماژول Fulfillment: ارکستراسیون ارسال پس از Paid. سفارش و موجودی مستقیم اینجا |
| Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.cs | Tooba.Fulfillment.Infrastructure.Directories | /// نگهبان باز موردکاربرد Fulfillment. /// |
| Tooba.Fulfillment.Infrastructure/Directories/FulfillmentShippedQuantityReader.cs | Tooba.Fulfillment.Infrastructure.Directories | خواندن QuantityShipped برای OrderSupply بدون افشای FulfillmentDbContext به Host. |
| Tooba.Fulfillment.Infrastructure/Directories/SellerFulfillmentAuthorizer.cs | Tooba.Fulfillment.Infrastructure.Directories | پیاده‌سازی سیاست احراز order.handle برای جهش‌های فروشنده. |
| Tooba.Fulfillment.Infrastructure/Errors/FulfillmentErrorCatalogContributor.cs | Tooba.Fulfillment.Infrastructure.Errors | کاتالوگ صریح کدهای خطای Fulfillment. |
| Tooba.Fulfillment.Infrastructure/Gateways/FulfillmentInventoryGateway.cs | Tooba.Fulfillment.Infrastructure.Gateways | /// مصرف رزرو موجودی از طریق قرارداد Inventory. /// |
| Tooba.Fulfillment.Infrastructure/Handlers/FulfillmentPaymentSucceededHandler.cs | Tooba.Fulfillment.Infrastructure.Handlers | /// مصرف‌کننده payment.succeeded.v1 برای ایجاد idempotent fulfillment. /// |
| Tooba.Fulfillment.Infrastructure/Messaging/FulfillmentOutboxRegistration.cs | Tooba.Fulfillment.Infrastructure.Messaging | /// ثبت Outbox ماژول Fulfillment. سفارش را مستقیم به‌روز نمی‌کند؛ ترجمه فقط Inte |
| Tooba.Fulfillment.Infrastructure/Observability/FulfillmentInstrumentation.cs | Tooba.Fulfillment.Infrastructure.Observability | /// متریک‌های fulfillment بدون PII. /// |
| Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs | Tooba.Fulfillment.Infrastructure.Persistence | /// DbContext مالک schema <c>fulfillment</c>. سفارش و موجودی را نگه نمی‌دارد. // |
| Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentPaymentInboxRecord.cs | Tooba.Fulfillment.Infrastructure.Persistence | /// dedup رویداد payment.succeeded برای ایجاد fulfillment. /// |
| Tooba.Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkQueueQueryEngine.cs | Tooba.Fulfillment.Infrastructure.Queries | پرس‌وجوی DB-native صف کار ارسال و تحویل Admin با batch map و بدون N+1. |
| Tooba.Fulfillment.Infrastructure/Shipping/ShippingCatalogReader.cs | Tooba.Fulfillment.Infrastructure.Shipping | خواندن کاتالوگ سرویس ارسال از schema fulfillment. |
| Tooba.Fulfillment.Infrastructure/Shipping/ShippingServiceDirectory.cs | Tooba.Fulfillment.Infrastructure.Shipping | orchestration نوشتن ShippingService روی Fulfillment DbContext. |
| Tooba.Fulfillment.Infrastructure/Shipping/ShippingServiceLanguageGate.cs | Tooba.Fulfillment.Infrastructure.Shipping | /// Fulfillment-owned language gate for shipping-service writes/seed. /// Depend |
| Tooba.Fulfillment.Tests/Architecture/FulfillmentArchitectureGuardTests.cs | Tooba.Fulfillment.Tests.Architecture | production type |
| Tooba.Fulfillment.Tests/Behavior/AdminFulfillmentBulkTests.cs | Tooba.Fulfillment.Tests.Behavior | TB-TMAR-NEXT-MODULE-BATCH-004-R2 — work-queue bulk Application ownership. |
| Tooba.Fulfillment.Tests/Behavior/FulfillmentCharacterizationTests.cs | Tooba.Fulfillment.Tests.Behavior | production type |
| Tooba.Fulfillment.Tests/Behavior/FulfillmentErrorAndGridTests.cs | Tooba.Fulfillment.Tests.Behavior | production type |
| Tooba.Fulfillment.Tests/Behavior/SellerFulfillmentAuthTests.cs | Tooba.Fulfillment.Tests.Behavior | production type |
| Tooba.Fulfillment.Tests/Behavior/ShippingMethodsTreeTests.cs | Tooba.Fulfillment.Tests.Behavior | TB-TMAR-NEXT-MODULE-BATCH-004-R3 — shipping methods tree Application ownership. |
| Tooba.Fulfillment.Tests/Behavior/ShippingServiceLanguageGateTests.cs | Tooba.Fulfillment.Tests.Behavior | TB-TMAR-NEXT-MODULE-BATCH-004-R4 — Infrastructure-owned shipping language gate. |
| Tooba.Fulfillment.Tests/Endpoints/FulfillmentEndpointOwnershipTests.cs | Tooba.Fulfillment.Tests.Endpoints | production type |
