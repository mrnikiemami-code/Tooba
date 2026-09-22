# Order cross-module boundary audit

Order uses stable Contracts for Inventory, Offer, Pricing, Promotion, Tax, Fulfillment, Payment, Returns, and Notification in several seams. Residual direct Application references remain visible in Order Infrastructure, notably Cart and Catalog Application ports/types and Payment Application models/ports in `OrderModule.cs`.

No foreign Infrastructure reference or foreign DbContext usage was identified in the inspected production Order sources. Because foreign Application references remain, `CONTRACTS_ONLY` is not proven. R1 must extract only the concrete contract seams needed by Order, without reopening protected modules broadly.
