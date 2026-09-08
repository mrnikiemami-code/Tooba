# Focused validation

Host (`Tooba.Host.Tests` filter ScopeSequence + line-qty + work-queue + corrective + split-delivery): Passed 60 / Failed 0.

Frontend: `admin-order-items-shipping.test.ts`, `admin-order-operations.test.ts`, `admin-t006-return-shipping.test.ts` — 35 pass.

Recovery guard: 3 pass.

`git diff --check`: clean (CRLF warnings only on pre-existing files).
