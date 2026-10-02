# Configuration root hygiene — TB-TMAR-HOST-ROOT-FINAL-CERT-001

## PostgreSqlOptions.ConnectionString property

ZERO — property absent; no live production consumer.

## Stale appsettings key Tooba:PostgreSQL:ConnectionString

Confirmed inert empty residue with zero binding authority after W1 removal of the options property.

REMOVED from:

- appsettings.json
- appsettings.Production.json
- appsettings.Development.json

ConnectionReferences untouched. Values/schema otherwise unchanged.

`Legacy-PostgreSQL-ConnectionString-Key-State = ZERO_ALL_ROOT_APPSETTINGS`

## Program options / fail-fast (preserved)

- AddOptions\<ToobaPlatformOptions\>().Bind(...).ValidateOnStart()
- IValidateOptions\<ToobaPlatformOptions\>, PlatformOptionsValidator
- ControlPlaneRegistry via PlatformOptionsValidator.BuildRegistry
- TrustedProxies IPAddress.Parse after validation
- PrimaryDomain fail-fast remains on validator/registry path

## Duplicate using hygiene

Removed one exact duplicate:
`using Tooba.Offer.Infrastructure.Adapters.Tracing;`

`Duplicate-Using-State = ZERO`

## Secrets

Production appsettings: no real credentials. No secrets/raw connection strings in evidence or Result.
