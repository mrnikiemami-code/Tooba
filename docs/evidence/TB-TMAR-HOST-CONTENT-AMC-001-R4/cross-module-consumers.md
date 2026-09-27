# Cross-module Content consumers — R4

Grep of backend Modules (excluding Content) for Tooba.Content.Application project references: ZERO.
Host composition (Program MediatR assembly + StorefrontComposer) and Host.Tests remain legitimate Host consumers of Application (not foreign modules).
Content.Contracts stays Errors-only — no additional boundary DTOs proven necessary.
