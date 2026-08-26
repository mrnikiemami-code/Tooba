# 02 — SpiceDB Runtime Proof

Task: `TB-P05-T017-UNBLOCK-01`

## Expected setup (repository-supported)

`SpiceDbIntegrationTests` uses **Testcontainers** image `authzed/spicedb:v1.56.0` (documented in `docs/architecture/38-spicedb-authorization-foundation.md`). No alternate auth service invented.

## Failure root cause on prior BLOCKED run

1. Environment had `DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT=false`, which breaks cleartext gRPC HTTP/2 to SpiceDB.
2. After enabling HTTP/2, leaving `HTTPS_PROXY` set caused the gRPC client to attempt proxied HTTP/2 to localhost and fail with:
   `Requesting HTTP version 2.0 with version policy RequestVersionExact while unable to establish HTTP/2 connection.`

## Restoration

1. `docker pull authzed/spicedb:v1.56.0` — image present / up to date.
2. Process env: `DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT=true`.
3. Clear `HTTP_PROXY` / `HTTPS_PROXY` / `ALL_PROXY` before tests; set `NO_PROXY=127.0.0.1,localhost`.
4. Tests start SpiceDB via Testcontainers (supported mechanism).

## Proof

```text
dotnet test ... --filter FullyQualifiedName~SpiceDbIntegrationTests
Passed SpiceDbIntegrationTests.Real_spicedb_allows_member_denies_other_tenant_and_fails_closed_when_stopped
```

Full suite (after fix):

```text
Passed! - Failed: 0, Passed: 204, Skipped: 0, Total: 204
```
