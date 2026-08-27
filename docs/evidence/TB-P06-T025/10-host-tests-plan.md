# 10 — Host.Tests plan

Task: TB-P06-T025

## File

`src/backend/Host/Tooba.Host.Tests/SupportFoundationTests.cs`

## Authz cases

| Test | Expect |
|------|--------|
| Customer own ticket | 200 |
| Customer foreign ticket | 403/404 |
| Seller own SellerParty | 200 |
| Seller foreign party header | 403 |
| Seller employee without support.view | denied/capability |
| Admin without support.view | 403 |
| Admin with support.manage | reply/patch OK |
| Related Order ownership | reject foreign order id |
| Internal note isolation | absent from customer/seller JSON |
| Deep-link ownership | notification target routes under allowlist |

## Integration cases

| Test | Expect |
|------|--------|
| Create customer + seller ticket | snapshot returned |
| Replies thread | LastMessageAt advances |
| Status close/reopen | policy |
| Admin public reply → notification | CreateIfAbsentAsync / event |
| Idempotency-Key duplicate reply | same message / no dup |
| Admin search/filter | q + status |
| Seed idempotency | second run no dup |

## Gate

Typed integration tests wait until `Modules/Support` + `Host/Support` land (sibling worker). Source-scan facts may skip until endpoints file exists.
