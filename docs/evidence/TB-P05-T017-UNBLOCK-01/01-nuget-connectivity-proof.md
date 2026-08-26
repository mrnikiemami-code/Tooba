# 01 — NuGet Connectivity Proof

Task: `TB-P05-T017-UNBLOCK-01`

## Root cause

Local DNS for `api.nuget.org` resolved to Akamai edges (`185.200.125.x` / `2.16.10.x`) where HTTPS timed out (~20s+). Direct CONNECT to Azure Front Door edge `150.171.109.34` with SNI `api.nuget.org` succeeded (HTTP 200, service index + vulnerability index).

Hosts-file override failed (Access Denied without elevation).

## Restoration method (audit preserved)

1. Local CONNECT proxy: `scripts/nuget-connect-proxy.mjs` on `127.0.0.1:18888` mapping `api.nuget.org` → `150.171.109.34`.
2. Temporary NuGet config with `http_proxy` / `https_proxy` pointing at that proxy, still using source `https://api.nuget.org/v3/index.json`.
3. Helper: `scripts/run-backend-validation-with-nuget-proxy.ps1`

## Proof probes

| Probe | Result |
| --- | --- |
| DNS default → Akamai | TCP often “open”, HTTPS timeout |
| `curl --connect-to api.nuget.org:443:150.171.109.34:443` | HTTP 200, index 9272 bytes |
| `curl -x http://127.0.0.1:18888 https://api.nuget.org/v3/index.json` | HTTP 200 |
| `curl -x … /v3-vulnerabilities/index.json` | HTTP 200 |
| `dotnet restore --configfile NuGet.Proxy.config` | **NU1900 = 0**; vulnerability base/update JSON OK |

## Controls

- NuGetAudit: **not** disabled
- NU1900: **not** suppressed
- TLS: not weakened (full HTTPS to nuget.org via proxy CONNECT)
- No secrets hard-coded
