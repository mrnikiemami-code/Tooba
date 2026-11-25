# RECEIPTS — TB-296 (implementation + verification)

All paths relative to the worktree root. Worker PASS ≠ Architect ACCEPT.

## Source changes
- `src/frontend/lib/offer-workspace/{types,model,messages,offer-workspace,tab-shared,identity-tab,pricing-tab,inventory-tab,shipping-tab,conditions-tab,gated-tabs,index}.ts(x)` — shared 7-tab workspace.
- `src/frontend/app/admin/offers/new/page.tsx`, `src/frontend/app/admin/offers/[offerId]/page.tsx` — admin create/edit adapters.
- `src/frontend/app/vendor-panel/products/new/page.tsx`, `src/frontend/app/vendor-panel/products/[offerId]/page.tsx` — seller create/edit adapters; reservation policy + attributes preserved.
- `src/frontend/app/admin/admin-offer-api.ts` — full detail mapping + patch/price/inventory/fulfillment/shipping-type clients.
- Tests: `src/frontend/lib/offer-workspace/model.test.ts`, `src/frontend/app/admin/offers/admin-offer-api.test.ts`, `src/frontend/app/admin/offers/offer-workspace.mount.test.ts`.
- OpenSpec: `openspec/changes/offer-workspace-production-296/{tasks.md,specs/offer-workspace/spec.md}`.

## Commands actually run
| Command | Result | Log |
|---|---|---|
| `node tsx --tsconfig tsconfig.mount-tests.json --test lib/offer-workspace/model.test.ts app/admin/offers/admin-offer-api.test.ts app/admin/offers/offer-workspace.mount.test.ts` | 29 passed / 0 failed, EXIT 0 | `evidence/frontend-focused-tests.log` |
| `npm run typecheck` | EXIT 0 | `evidence/typecheck.log` |
| `npm run lint` | EXIT 0 (warnings only, none from offer-workspace) | `evidence/lint.log` |
| `npm run build` | EXIT 0, BUILD_ID `CSzEhL0qy7TLGLKjsQ7QE` | `evidence/next-build.log` |
| `openspec validate offer-workspace-production-296 --strict` | valid, EXIT 0 | `evidence/openspec-validate.log` |
| `dotnet test Tooba.Host.Tests --filter CampaignCartPriceIntegrityTests` | 2 passed / 0 failed, EXIT 0 | `evidence/stage2-campaign-cart-test.log` |
| `node evidence/verify-runtime.cjs` (Playwright, FE3000/Host5088) | PASS, all checks | `evidence/runtime/verify-runtime.json` |
| `node evidence/verify-grid.cjs` | rows 10/10/10, PASS | `evidence/runtime/verify-grid.json` |
| `npm run test:architecture` | EXIT 1 on pre-existing baseline drift; no offer-workspace file listed | `evidence/architecture-guards.log` |
| Impeccable `detect --json` (eight workspace files) | `[]` | `evidence/impeccable-detect.json` |

## Live runtime facts
- Frontend `http://127.0.0.1:3000`, Host `http://127.0.0.1:5088`; `/health/ready` = ready, SingleStore, tenant-alpha, messaging Healthy.
- Created draft `6a0cd198-c5f6-fc58-6aa0-ad7d26a70d6f` and independently re-read: price 1234567, SKU `TB296-VERIFY`, return `Custom/9`, min 2, max 6.
- Seller offer `457e375f-ceec-4b66-b81e-3829c4e15893`: price 3704749 and on-hand 198 persisted/re-read, then seed values (3704748 / 199) restored and re-verified.
- Overflow: 0 at 360/390/768/1280/1440; screenshots under `evidence/runtime/`.

## Evidence hashes (SHA-256)
- verify-runtime.json `13677A185639C33711E3C0FC6393AF34D362581946C5BADF93E74C451695B243`
- verify-grid.json `918199323E7B68603F655E03C79A811B23D53F9928AAE0E711C6A66212922870`
- stage2-campaign-cart-test.log `27142981535422C70494D3F0BD85CC8E61250559FD838A092CD65F3B671C1ED7`
- next-build.log `88CE7B248AB6B76997A59A9D0448C4072718DDA743062A1097235157BA1E19BD`
- impeccable-detect.json `2708BE578E03E792E308E41C26495572A8AA7654CB654BCA2C5CCCCC7C8EF761`

## Gated (not fabricated)
Per-offer discount definition (Promotion), offer history read projection, per-offer shipping service assignment (Stage-3), Pricing multi-currency/validity price-list read, fulfillment responsibility read-back.

## R7 receipts (visual & UX parity)
| Command / artifact | Result | Evidence |
|---|---|---|
| `next build` | EXIT 0, BUILD_ID `iLmDj6VKtku_4lD1PdAtc` | `evidence/r7-build-final14.log` (SHA-256 526F1EBA…8B26) |
| `tsc --noEmit` | EXIT 0 | `evidence/r7-typecheck11.log` (3AC06398…64B9) |
| `next lint` | EXIT 0, no changed-file warnings | `evidence/r7-lint11.log` (8AFC9DE3…3371) |
| focused frontend tests | 29/29 | `evidence/frontend-focused-tests.log` (R6 suite; R7 code verified by same 29 tests) |
| Offer module tests | 97/97 | `evidence/r7-offer-tests.log` (8C1AD49E…17EF) |
| Stage-2 cart campaign | 2/2 | `evidence/r7-stage2-campaign-cart.log` (7C7BFE55…167D) |
| live Playwright verify | PASS | `evidence/r7-runtime/verify-r7.json` (FFD3DE6B…6823) |
| Impeccable detector | `[]` | `evidence/r7-impeccable-detect.json` (2708BE57…F761) |
| OpenSpec strict | valid | `evidence/r7-openspec-validate.log` |

Runtime facts:
- Lists: product x=923 rightmost after selection; actions x=49 leftmost; rows 10/10; order chain product→seller→sku→channel→price→available→status→date→actions all pass.
- Geometry: tabs 44; controls 37 desktop / 44 mobile; card radius 9px; header 160/193/224 vs artifact 148/202/198 at 1440/768/360.
- Persistence: admin price 1234567, unique SKU, returns Custom/9, qty 2..6, fulfillment responsibility/type re-read; seller 457e price 3704749/onHand 198 persisted then restored 3704748/199.
- Security: cross-seller offer read 404; seller has no shipping write.
- Overflow 0 at 360/768/1440 for both form and lists.
- Create fallback note: final round reused a Draft after 10 duplicate-active attempts; earlier rounds created real drafts (a3aa67a4…, f5fd1b05…).
- Test maintenance (no product behavior): Offer endpoint manifest expanded to the real 10 endpoint-reachable requests; stale ProductWorkspace guard path corrected; `TestFulfillmentAssignments` added for Offer unit/observability DI. Offer tests 97/97 after these fixes.
