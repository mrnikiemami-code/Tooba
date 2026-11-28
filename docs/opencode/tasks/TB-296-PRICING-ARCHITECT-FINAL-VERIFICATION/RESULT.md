# TB-296 — PRICING ARCHITECT FINAL VERIFICATION — RESULT

**STATUS: BLOCKED (16 PASS / 1 PARTIAL / 1 NOT_RUN / 0 FAIL of 18 mandatory cases).** Final acceptance is not claimed. No product/architecture change was made by this task; the executed candidate is the architect commit below.

## Identity
- Branch `opencode/iso-agents-002`; remote/working HEAD `4d30a21bff422676484df1943da6d05f694b0e86`.
- Architect product candidate `4f19708be3d980a22535fb446d3cfaa1a2ddfc9c` is an ancestor; `git diff 4f19708..4d30a21` touches only the two architect docs (`ARCHITECT-REVIEW.md`, `ARCHITECT-VALIDATION-MATRIX.json`). The built/served product is therefore the exact repaired candidate.
- Host 5088 (existing, DLL SHA-256 `10098e16bfca69fa41a736609b2535aad8e911457db194ed916a112e4c379529`), Next 3000 (BUILD_ID `fzXq0vXgQ2zeCrG2p_IBW`), PostgreSQL `tooba_alpha`. No new Host/DB; no migration (no schema change).
- Test-only DEV config tampering for REF-02/03 was restored byte-exactly (SHA-256 `063fd0d074d12bda390048dedc1e9b8cd0eb0cae6c726570b3a77d402f073e86`); final P1C residue 0 offers / 0 carts.

## 18-case ledger
| ID | Status | Evidence |
|---|---|---|
| R11-REF-01 | PASS | `evidence/http-cases.json` (admin+seller exact active codes/order/precision vs config) |
| R11-REF-02 | PASS | `evidence/http-cases.json` + `evidence/ref02-inactive.json` (unknown→pricing.currency.invalid, TMN→display_unit, controlled inactive CAD→invalid; zero price/fact/outbox change each) |
| R11-REF-03 | PARTIAL | `evidence/ref03-no-active.json` PASS (all-inactive reference: `/currencies` empty, IRR write 400 `pricing.currency.invalid`, zero change); missing-trusted-market half NOT_RUN |
| R11-REF-04 | PASS | `evidence/browser-cases.json` (aborted reference → localized unavailable, disabled editor, retry against real endpoint recovers, no price request) |
| R11-PRICE-01 | **NOT_RUN** | `evidence/price01-NOTRUN.json` |
| R11-PRICE-02 | PASS | `evidence/http-cases.json` (concurrent USD stale conflict; IRR revision independent; facts) |
| R11-PRICE-03 | PASS | `evidence/http-cases.json` (latest Retired authoring only; effective quote still older Active; write follows existing `pricing.overlap`, no overwrite) |
| R11-PRICE-04 | PASS | `evidence/price04.json` (real browser + concurrent HTTP/SQL: conflict blocks overwrite, identical intent reconciles, unrelated IRR change preserved) |
| R11-PRICE-05 | PASS | `evidence/http-cases.json` (identical replay same receipt, changed amount/currency 409, one fact) |
| R11-PREC-01 | PASS | `evidence/browser-cases.json` (IRR/JPY fractional blocked with no request; USD excess scale blocked; exact scales persisted) |
| R11-PREC-02 | PASS | `evidence/http-cases.json` (100000.75 → price/fact 100001; replay one fact) |
| R11-LOCALE-01 | PASS | `evidence/browser-cases.json` (real panel selector fa-IR/en-US/ru-RU; direction/labels; Persian IRR integer; ru comma decimal) |
| R11-LOCALE-02 | PASS | `evidence/browser-cases.json` (dirty-currency switch warning, draft retained, no accidental write, USD restores) |
| R11-UAT-01 | PASS | `evidence/browser-cases.json` (edit-mode warehouse provisioning + physical 50 with reason; reload/SQL) |
| R11-UAT-02 | PASS | `evidence/http-cases.json` (cross-owner/mismatched seller+actor denied; no mutation) |
| R11-UAT-03 | PASS | `evidence/browser-cases.json` (390x844 + 1440: no overflow, RTL, no market column, no duplicated variant label) |
| R11-REG-01 | PASS | `evidence/reg01.json` (real default-currency cart/checkout/payment; order unit price = primary IRR; offer/stock unchanged; exact cleanup) |
| R11-REG-02 | PASS | raw architecture log (27 baseline size + FE-FOLDER baseline identities unchanged, 0 Offer-owned), OpenSpec strict valid, gate self-tests (11 + 14 negative controls), zero cleanup residue |

## Exact blockers
1. **R11-PRICE-01 NOT_RUN** — `node docs/opencode/tasks/TB-296-PRICING-ARCHITECT-FINAL-VERIFICATION/price01.cjs`, phase “admin create product/variant selection”. Seller A has no free published variant (DB: every published product/variant already has a seller-A offer); an already-offered variant raises the shared duplicate-active-offer dialog. Seller B has free published variants, but the create UI product picker did not expose the matching free product option across its paged dialog within the harness loop (`product option not found`), so the create submit was never reached. This is a selection/harness limitation, not a proven product defect; no offer was created by the failed attempt (fixture check).
2. **R11-REF-03 missing-market half NOT_RUN** — the Host always supplies the trusted store market; the matrix oracle requires the *existing Pricing integration provider / real PostgreSQL* for an absent-market commerce context, and no such provider exists in the repository. Creating a new Host/DB is forbidden. The no-active-currency half is proven above.

## Regression / gates actually run
- Offer 144/144, Pricing 20/20, Inventory 53/53, frontend typecheck 0, model 20/20, mount 29/29, i18n 20/20.
- Architecture guards: 27 pre-existing size violations and the FE-FOLDER baseline, 0 Offer-owned/new (exact identities compared; only timing text differs).
- OpenSpec `offer-multicurrency-pricing-296-r11 --strict` valid; `acceptance-gate --self-test` and `architect-review-gate --self-test` PASS (positive + 11/14 negative controls).
- No product/architecture change, no new Host/DB, no migration, no screenshots, no reviewer subagents.

## Remaining for architect
- Complete R11-PRICE-01 via the real create UI with a valid free published seller/product/variant fixture (or authorize a deterministic UI fixture) and R11-REF-03’s missing-market branch with the existing Pricing integration provider.
- Independent architect receipt/owner UAT; this task does not self-authorize acceptance.
