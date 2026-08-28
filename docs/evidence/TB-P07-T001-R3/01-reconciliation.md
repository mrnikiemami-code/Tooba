# TB-P07-T001-R3 — Recovery + Reconciliation

## Git
| Check | Value |
| --- | --- |
| branch | `main` |
| HEAD | `c32b016cb6b5dbbdd91e83b06409ee5ead2ad07f` |
| origin/main | `c32b016cb6b5dbbdd91e83b06409ee5ead2ad07f` |
| Expected baseline | `155a0e46af09259975e4e73f24cb910c304a0c39` |
| Baseline ancestor of HEAD | **YES** (`155a0e4` feat T001 + `c32b016` bridge meta sync) |
| tracked tree | clean (untracked junk/logs only) |
| RECOVERY | OK (not RECOVERY_CONFLICT) |

## Bridge
- Task: `TB-P07-T001-R3`
- Bridge UUID: `6d800950-c681-435e-b100-fbc7a3aa1f5b`
- Worker: `tooba-worker-01` Working
- Supersedes: T001 / T001-R1 / T001-R2

## Runtime before work
- Backend `:5088` live/ready
- FE `:3000`
- Shopeiva `:3001`

## T001 change classification

| Area | Classification | Notes |
| --- | --- | --- |
| Catalog attribute definitions/options metadata | KEEP | Domain foundation |
| Category attribute binding + inheritance + effective schema | KEEP | Admin schema ownership |
| Product typed attributes + validation | KEEP | Admin product specs |
| Product variant axes + fingerprint | KEEP | Admin variants |
| Migration `AddCategoryAttributeSchemaFoundation` | KEEP | |
| Permissions `catalog.attribute.*` | KEEP | Admin AC |
| Admin CatalogAttributeEndpoints | KEEP / ADAPT_FOR_ADMIN | Enrich UX in Admin |
| Admin catalog attribute UI pages | ADAPT_FOR_ADMIN | Human labels, no GUID walls |
| CatalogAttributeSchemaDevelopmentBootstrap | KEEP / ADAPT | Extend for 4+ images seed |
| Seller attribute write endpoints | DEFER_SELLER | Preserve contracts; no Seller visual polish |
| Seller product attributes panel on offer page | DEFER_SELLER | Do not expand Seller UI this task; leave non-regressive |
| Product.Price/Stock shortcuts | REMOVE_IF_CONFLICTING | None present — keep forbidden |

Do **not** discard Category Attribute / Variant foundation.
