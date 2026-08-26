# 30 — Zero-Warning Validation Proof

Task: `TB-P05-T017-R1`

## Network / advisory feed probes

Attempts to reach NuGet vulnerability advisory metadata all failed (timeout). Auditing was **not** globally suppressed (`NuGetAudit` left enabled).

| Probe | Result |
| --- | --- |
| `Invoke-WebRequest https://api.nuget.org/v3/index.json` | FAIL — operation timed out |
| `Invoke-WebRequest https://api.nuget.org/v3-vulnerabilities/index.json` | FAIL — operation timed out |
| `curl -4 https://api.nuget.org/v3/index.json` | FAIL — connection timed out after ~20s (`http_code=000`) |
| `curl -4 https://api.nuget.org/v3-vulnerabilities/index.json` | FAIL — connection timed out |

## Backend commands (repo-supported)

```text
dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx
```

### restore

- Result: packages up-to-date
- **NU1900 count: 23+** (one per project): `Error occurred while getting package vulnerability data: Unable to load the service index for source https://api.nuget.org/v3/index.json.`
- NuGetAudit was **not** disabled to manufacture green output

### build (after stopping file-locking `Tooba.Host` process)

- Errors: 0
- Warnings: **non-zero** — all observed warnings were **NU1900** (advisory feed unreachable)
- Sample: `warning NU1900: Error occurred while getting package vulnerability data: Unable to load the service index for source https://api.nuget.org/v3/index.json.`

### test

- Host.Tests after unlock: **Passed 203 / Failed 1 / Skipped 0 / Total 204**
- Failure: `SpiceDbIntegrationTests.Real_spicedb_allows_member_denies_other_tenant_and_fails_closed_when_stopped` — SpiceDB container not present in `docker ps` (only `postgres-db` + `rabbitmq`); gRPC HTTP/2 connection error. Unrelated to PDP repair code.
- Contract requirement `warnings = 0` still cannot be met while `api.nuget.org` is unreachable (NU1900 on restore/build)

## Frontend validation

| Step | Result |
| --- | --- |
| `npm run typecheck` | PASS (exit 0) |
| `npm run lint` | PASS (exit 0) |
| `npm run test:storefront` | PASS — 18/18 |
| `npm run build` | PASS (exit 0) |

## Sticky pin proof (runtime)

From `scripts/capture-t017-r1-evidence.mjs sticky`:

```text
sticky-pin-check { before: 174, after: 0, stuck: true }
```

## Conclusion

**BLOCKED** on zero-warning backend validation solely due to environment inability to load NuGet vulnerability advisory index (`NU1900`). Repair scope items A/B/C/E (sticky, real Shopeiva PNGs 02–08, fidelity docs, frontend green) are complete. No global `NuGetAudit=false` / project-wide audit disable applied.
