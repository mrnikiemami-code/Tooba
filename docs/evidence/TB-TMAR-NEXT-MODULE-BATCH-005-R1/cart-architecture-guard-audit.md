# cart-architecture-guard-audit

Project: Tooba.Cart.Tests / Architecture/CartArchitectureGuardTests.cs

Enforces: root dump=0 (GlobalUsings allowed), path↔namespace, no TypeForwardedTo, Domain purity, Application Contracts-only foreign refs, Infrastructure no foreign App/Domain/Infra, no foreign DbContext, no clock/id bypass, no silent catch, no localized exception prose (cart.* codes), Host CartDbContext allowlist bootstrap-only.

Cart-Architecture-Guards: ENFORCED
