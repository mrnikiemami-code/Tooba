# 01 — Runtime start (TB-P06-T009-R1)

## PostgreSQL

- Port `5432` already allocated (existing local instance; docker compose postgres skipped bind conflict)
- Connection refs in `appsettings.Development.json`: `admin` / databases `tooba_alpha`, `tooba_marketplace`, `tooba_messaging`

## Backend

```text
cd src/backend/Host/Tooba.Host
dotnet run --no-build --urls http://127.0.0.1:5088
```

- URL: `http://127.0.0.1:5088`
- PID (final restart): background host process after validation rebuild

## Frontend

```text
Already running: http://127.0.0.1:3000
TOOBA_HOST_ORIGIN=http://127.0.0.1:5088
```

- URL: `http://127.0.0.1:3000`
