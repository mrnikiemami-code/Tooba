# Order contract surface audit

`Tooba.Order.Contracts` owns focused Fulfillment, Notifications, Payments, and Returns surfaces and does not intentionally expose ASP.NET or Host types. Persistence implementation remains outside Contracts.

Extraction safety is not yet accepted because Host composers still consume broader Application/Infrastructure seams and Order Infrastructure retains foreign Application dependencies. R1 must complete the inbound HTTP contracts and remove those structural leaks before claiming `EXTRACTION_SAFE`.
