# Contracts seam

Moved IOrderStorefrontActor + IOrderStorefrontCheckoutIdentityGate to Tooba.Order.Contracts.Storefront (OrderStorefrontActorContracts.cs).
Application Ports retain only shipping/pending store interfaces. Consumers and Program DI repointed. CartAccess remains Cart.Contracts (allowed).
