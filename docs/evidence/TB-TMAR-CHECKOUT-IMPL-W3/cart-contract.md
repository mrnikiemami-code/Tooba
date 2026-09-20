# Cart contract
- Assembly: Tooba.Cart.Contracts (refs Offer.Contracts only; no Domain/Application)
- Types: CartAccess, CartSnapshot/CartLineSnapshot, CartStatus/CartAccessKind/CartConversionIntent (contract enums, value-aligned with Domain), ICartQueryGateway
- Conversion seam: ICartConversionPort.ConvertForCheckoutAsync(CartConversionRequest) → CartConversionResult
- Request carries ProcessId + CorrelationId
- No EF/DbContext/entity leakage; Cart remains conversion authority
- Restore/compensation port NOT activated
