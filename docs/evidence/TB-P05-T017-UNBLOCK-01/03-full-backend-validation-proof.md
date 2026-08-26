# 03 — Full Backend Validation Proof

Task: `TB-P05-T017-UNBLOCK-01`

Commands (with NuGet proxy config for restore/build; proxies cleared for test):

```text
dotnet restore src/backend/Tooba.slnx --configfile <NuGet.Proxy.config>
dotnet build   src/backend/Tooba.slnx --configfile <NuGet.Proxy.config>
dotnet test    src/backend/Tooba.slnx --no-build
```

| Step | Result |
| --- | --- |
| restore | exit 0, **NU1900 = 0**, vulnerability feeds OK |
| build | **0 Warning(s), 0 Error(s)** |
| test | **Failed 0 / Passed 204 / Skipped 0 / Total 204** |

Contract: warnings=0, errors=0, failed=0, skipped=0 — **met**.

Frontend (parent integrity D):

| Step | Result |
| --- | --- |
| typecheck | PASS |
| lint | PASS |
| test:storefront | PASS 18/18 |
| build | PASS |
