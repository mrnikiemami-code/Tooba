# cart-cqrs-audit

Cart-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
MediatR: 12.5.0 via BuildingBlocks AddToobaCqrsFoundation
Commands: CreateGuestCart, MergeCartAfterLogin, AddCartLine, ChangeCartLineQuantity, RemoveCartLine
Queries: GetCurrentAuthenticatedCart, GetCart
No CartHandlers.cs monolith
Host registers Cart.Application assembly in CQRS foundation
