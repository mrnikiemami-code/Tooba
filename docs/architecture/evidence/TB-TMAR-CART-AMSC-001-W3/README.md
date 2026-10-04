# TB-TMAR-CART-AMSC-001-W3 — Evidence index

Certification evidence for `TB-TMAR-CART-AMSC-001-W3` (`tooba-architecture-certify`).

| Artifact | Purpose |
|---|---|
| `certification.md` | Final verdict, axis summary, behavior statement, bounded repair |
| `cqrs-request-matrix.md` | Route → request → handler → validator matrix, MediatR version |
| `localization-catalog.md` | Error-code ownership, composed-catalog uniqueness, both-culture resx coverage |
| `validation.md` | Focused build/test evidence and pre-existing-drift proof |
| `residual-debt.md` | Non-blocking watch list and the bounded Host guard repair |

Structural evidence for the same surface lives in the W2 gate:
`docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W2/{structure,folder-granularity,validation,physical-tree-after}.md`.

Durable locks added by this wave:
`src/backend/Host/Tooba.Host.Tests/Architecture/CartModuleAmsc001W3CertGuardTests.cs`.
