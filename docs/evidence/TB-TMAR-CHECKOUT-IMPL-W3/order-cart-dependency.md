# Order–Cart dependency
- Order.Application → Cart.Contracts (Cart.Application ProjectReference removed)
- App→App baseline shrunk: removed Tooba.Order.Application → Tooba.Cart.Application
- Order.Infrastructure → Cart.Application retained for ICartDirectory / CartConversionAdapter / reconcile mutations (baselined Infra→foreign Application)
- Order.Infrastructure → Cart.Contracts for snapshot/query types
