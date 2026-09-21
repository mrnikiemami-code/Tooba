# Namespace audit — TB-TMAR-NEXT-MODULE-BATCH-001-R1

## Inventory
| Project count | Handwritten .cs | Namespace mismatches (after) | Root-dump .cs (after) | TypeForwardedTo |
|---|---:|---:|---:|---:|
| 4 | 33 | 0 | 0 | 0 |

Before (Architect reopen): many Domain/Application/Infrastructure/Contracts files root-dumped; InventoryDomain god-file; flat namespaces.
After: responsibility folders + matching namespaces; mismatches=0; root-dump=0.

## Promotion
| Project count | Handwritten .cs | Namespace mismatches (after) | Root-dump .cs (after) | TypeForwardedTo |
|---|---:|---:|---:|---:|
| 4 | 34 | 0 | 0 | 0 |

Before: PromotionDomain/MerchandisingCampaignDomain root god-files; directories/module/outbox root-dumped.
After: Aggregates/ValueObjects/Events/Policies/Merchandising + Infrastructure Directories/Queries/Adapters/Messaging/DependencyInjection; mismatches=0; root-dump=0.

Wrong-layer namespace hits: 0 (Contracts≠Domain; Domain≠Infrastructure).
