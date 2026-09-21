# Anti-workaround audit — TB-TMAR-NEXT-MODULE-BATCH-001-R1

## Inventory (after)
| Pattern | Count |
|---|---:|
| `?? new SystemUtcClock()` | 0 |
| `?? new UuidV7IdGenerator()` | 0 |
| `?? new ModuleCallTracer()` | 0 |
| `catch (Exception)` | 0 |
| empty/silent catch | 0 |
| localized exception prose | 0 |

Repairs: required IClock/IIdGenerator/IModuleCallTracer ctors; CheckoutInventoryReservationAdapter silent catch removed; ReleaseAsync Released|Consumed idempotent no-op; EnsureOrderSupply no catch(Exception)/empty rollback catch; outbox message → `inventory.outbox.unmapped_event_type`.

## Promotion (after)
| Pattern | Count |
|---|---:|
| `?? new SystemUtcClock()` | 0 |
| `?? new UuidV7IdGenerator()` | 0 |
| `?? new ModuleCallTracer()` | 0 |
| `catch (Exception)` | 0 |
| empty/silent catch | 0 |
| localized exception prose | 0 |

Repairs: required directory IClock/IIdGenerator; Persian store-mismatch → `merchandising.campaign.store_mismatch`; PromotionDirectory seller/not-found codes; outbox → `promotion.outbox.unmapped_event_type`.

Localized leftovers: none
