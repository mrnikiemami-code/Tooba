# TB-281-R1 RECEIPTS

## Git

- Expected-Base-SHA: `860c6a2c72842c8971d8b9f9885e26cdb0f3f657` (== claim HEAD / origin main at start)
- Bridge row: `99c98755-4a95-443e-aea8-2b09e02136e3`
- TASK.md SHA256: `2BCA9E9A26D1F01833E3B9B5D7E5199F7882F6DCF0989224EB9D54B4B826D20A`

## Backup (before R1 Media apply)

| Item | Value |
| --- | --- |
| Path | `D:\temp\tb281r1-backup\tooba_alpha-pre-tb281r1.dump` |
| Bytes | 3070590 |
| SHA256 | `4D23B35B53FCEB41B35D42B4FA6B00FE0280B47FC41EAF225957C6D874BCBE63` |
| `pg_restore --list` TOC lines | 1487 |
| Exit | 0 |

## Parent TB-281 dump verification (NOW)

| Item | Value |
| --- | --- |
| Path | `D:\temp\tb281-backup\tooba_alpha-pre-tb281.dump` |
| Bytes | 3061329 |
| SHA256 | `7551A1A12E5758C21ACBBACC1DB52609ACA75D3C403331B6F36A9B78D8B7CD42` |
| VerifiedAt | 2026-11-19T03:50:27+03:30 |
| Note | Historical pre-TB-281 archive; not R1 pre-apply proof |

## Plan / apply (store-alpha)

Pending only before apply:

- Media: `20261119000844_Tb281R1OwnerOccurrenceIntentHash`

Applied once via `Tooba.MigrationRunner apply --tenant store-alpha`; Media `pendingMigrations: []`, Story unchanged pending `[]`.

## Tests

| Suite | Result |
| --- | --- |
| StoryMarketplace281* (domain+R1) | 7/7 |
| Story+Media AMSC focused filter | 52/52 |
| FE `npm run test:stories` | 6/6 |
| `tsc --noEmit` | 0 |

## Host

| Item | Value |
| --- | --- |
| Publish | `artifacts/publish-primary` |
| dll SHA256 | `96F28D635AD937484887C60B01FF550369A7F23955B8A0B30A23168EABE61FD5` |
| Port | 5088 |
| `/health` | 200 |
| `/health/ready` | 200 SingleStore + messaging Healthy |
| `/__platform-commerce` | store-alpha |

## Live writes

ZERO Story/Media business POST/upload/seed/reset/cleanup.
