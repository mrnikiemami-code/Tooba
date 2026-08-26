# 09 — NuGet zero-warning proof (TB-P05-T026-R1)

## Problem (from TB-P05-T026 rejection)

Prior gate Result reported **NU1900** during backend validation (NuGet vulnerability advisory index unreachable). Contract requires **warnings = 0**.

## Accepted recovery pattern

Repository helper: `scripts/run-backend-validation-with-nuget-proxy.ps1`

- Local CONNECT proxy via `scripts/nuget-connect-proxy.mjs`
- Proxy config at `%TEMP%/tooba-nuget-proxy-validation/NuGet.Proxy.config`
- Edge IP `150.171.109.34` (Azure Front Door for api.nuget.org)
- **Does NOT** disable NuGetAudit
- **Does NOT** suppress NU1900
- **Does NOT** weaken TLS

## Procedure

1. Stop running `Tooba.Host` temporarily to release DLL locks (Host restarted after validation).
2. Run helper (restore → build → test).

Log: `11-full-backend-validation.log`

## Results

| Step | Exit | Warnings | Errors | NU1900 |
|---|---:|---:|---:|---:|
| restore | 0 | 0 | 0 | **0** |
| build | 0 | **0** | **0** | **0** |
| test | 0 | — | — | — |

Tests: **205 passed, 0 failed, 0 skipped**

Build summary line: `0 Warning(s), 0 Error(s)`

**NU1900 gate: PASS**
