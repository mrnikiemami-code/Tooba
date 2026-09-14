# TB-P10-T005-R1 — Host current HEAD

Canonical Host remains `:5088`. No second port.

| Item | Value |
| --- | --- |
| Previous process | PID 18068, started 2026-09-13 17:41:13, pre-T005 binaries (`GET /v1/storefront/appearance` = 404) |
| Recycle | `taskkill` without `/F` refused; then `taskkill /F /PID 18068` |
| Start | `dotnet run --project src/backend/Host/Tooba.Host/Tooba.Host.csproj --launch-profile Tooba.Host` |
| New process | `Tooba.Host` PID 3732, started 2026-09-14 04:16:40 |
| Binaries | `Tooba.Host.exe` / `.dll` LastWriteTime 2026-09-14 04:16:39 (after HEAD `92f9f465`) |
| Listen | `:5088` |
| After recycle | `GET /v1/storefront/appearance` = 200 |

Host left running after validation.
