# Host/Admin empty progress (AMC)

## Current Admin `*.cs` count: **31** (unchanged — RETAIN_PARTIAL)

### Evacuated
| Slice | Owner |
|---|---|
| W20–W25 store/settings/seeds | Catalog / Order |
| PW lifecycle (W26) | ProductWorkspace.Endpoints + Catalog Commands |
| **PW variants create/patch (W27)** | ProductWorkspace.Endpoints + Catalog Variants |

### Next
1. ProductWorkspace delete (W17-plan W23) — Offer.Contracts reference gate
2. Merchandising → Promotion
3. Template/attribute seeds
4. W24-final: delete Host ProductWorkspace* shells

### Cannot empty yet
KEEP platform (~16) · StoreAppearance BLOCK · HoldPolicy BLOCK · ProductWorkspace* shells · Merchandising MUST_SPLIT
