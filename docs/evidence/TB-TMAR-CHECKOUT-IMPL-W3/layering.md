# Layering
- Process Manager: Order.Application
- Cart contract: Cart.Contracts
- Adapter: Cart.Application (CartConversionAdapter)
- No new foreign Application dependency on Order.Application
- Contract enums mirror Domain values; Domain entities not leaked into Contracts
- No business handler relocated into Infrastructure
