# TB-281 RECEIPTS

## Git

- Expected-Base-SHA: `6c0914c37eb9de1e915c874718d813270cc8d9f0` (ancestor OK)
- Claim HEAD: `c15a3a69c15675ae68acb7e001e01bf373f1d0b0` (docs-only issuance)
- Worker code commit: recorded after this evidence pack lands

## Backup (before apply)

| Item | Value |
| --- | --- |
| Path | `D:\temp\tb281-backup\tooba_alpha-pre-tb281.dump` |
| Bytes | 3061329 |
| SHA256 | `7551A1A12E5758C21ACBBACC1DB52609ACA75D3C403331B6F36A9B78D8B7CD42` |
| `pg_restore --list` TOC lines | 1475 |

## Plan (store-alpha)

Pending only:

- Media: `20261118233932_Tb281SellerAssetOwnerScope`
- Story: `20261118233937_Tb281CreatedByAndOccurrences`

## Apply

`Tooba.MigrationRunner apply --tenant store-alpha` once; Media/Story `pendingMigrations: []`, `succeeded: true`.

## Tests

| Suite | Result |
| --- | --- |
| StoryMarketplace281DomainTests | 4/4 |
| Story+Media AMSC focused filter | 52/52 |
| FE story-api.test.ts | 3/3 |
| `tsc --noEmit` | 0 |

## Host

| Item | Value |
| --- | --- |
| Publish | `artifacts/publish-primary` |
| dll SHA256 | `5B1CDCF438101EE8A99F734C13E5C9E535C14C90955355ADD7988EEA62AA3E17` |
| Port | 5088 |
| `/health` | 200 |
| `/health/ready` | 200 SingleStore + messaging Healthy |

## Live writes

ZERO Story/Media business POST/upload/seed/reset.
