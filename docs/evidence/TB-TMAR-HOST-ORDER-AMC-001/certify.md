# Certify — Host/Order AMC-001

## Verdict

**KEEP PASS** for `Host/Order`. Thin storefront actor + checkout identity gate remain Host platform adapters; Order HTTP/CQRS unchanged.

## Checklist

| Goal | State |
|---|---|
| Host/Order production files | 1 (`HostOrderStorefrontActor.cs`) |
| Path↔namespace | EXACT `Tooba.Host.Order` |
| Guest actor | Order.Contracts `StorefrontGuestActor` |
| Application.Services leak (`StorefrontCheckoutService`) | ZERO |
| Domain/Infrastructure/Persistence | ZERO |
| Host endpoints/routes in Order folder | ZERO |
| Schema / frontend | NONE / UNCHANGED |
| Durable guard | `HostOrderAmcGuardTests` |
