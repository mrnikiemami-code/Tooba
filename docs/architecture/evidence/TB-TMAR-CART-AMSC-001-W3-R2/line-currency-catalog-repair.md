# TB-TMAR-CART-AMSC-001-W3-R2 — Line-currency expected-failure catalog repair

- Task: `TB-TMAR-CART-AMSC-001-W3-R2`
- Parent: `TB-TMAR-CART-AMSC-001-W3-R1`
- Channel: `tooba-main`
- Mode: `BOUNDED_EXPECTED_FAILURE_CATALOG_REPAIR`
- Starting HEAD: `b5721b0f10c9f79319f329d7b97df9153f02eee9` (== `origin/main`)
- Verdict: `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` STRUCTURE_CERTIFIED

## Defect (before)

`CartErrorCodes.LineCurrencyMissing` = `cart.line.currency_missing` was:

- declared in `Tooba.Cart.Contracts/Errors/CartErrorCodes.cs`,
- localized in both `CartErrors.resx` (en) and `CartErrors.fa.resx` (fa),
- thrown **reachable, fail-closed** from two production sites:
  - `Tooba.Cart.Application/Presentation/CartPresentationComposer.cs:90`
  - `Tooba.Cart.Infrastructure/Directories/CartLineCurrency.cs:33`,
- **but registered by no error-catalog contributor**.

Because `SafeErrorMapper` resolves a code through the composed `ErrorDefinitionCatalog` and falls back to `MapUnexpected()` (`platform.unexpected`, HTTP 500) when the code is not found, this expected Cart-owned fault surfaced as an unexpected 500 instead of a typed/localized expected failure. `CartErrorCatalogContributor` registered only 25 descriptors.

`checkout.authentication_required` is Foundation-owned (`FoundationErrorCodes.CheckoutAuthenticationRequired`, registered by the Foundation contributor) and is only declared/consumed by Cart — it is **not** Cart-registered and must not be re-registered in Cart.

## Classification / status rationale (evidence-based, not guessed)

Selected descriptor:

```
D(CartErrorCodes.LineCurrencyMissing, ErrorClassification.Business, StatusCodes.Status409Conflict,
    "Cart line currency is unavailable. Please retry.")
```

Rationale:

- The code is the **line-level equivalent of a missing price quote**: a quoted line must always carry its own currency truth; when it is absent the caller must re-quote/retry and no server-side change can succeed without that currency truth. This is the same operational shape as the existing Cart code `cart.pricing.quote_missing`, which is registered as `Business` / `409 Conflict`.
- It is **not** a `400 BadRequest`: `400` is reserved in Cart for malformed/invalid client input (`cart.currency.invalid`, `cart.quantity.invalid`, `cart.offer.unavailable`, ...). The supplied request is valid; the line's currency truth is what is missing.
- It is **not** a `404`: the line exists (`cart.line.missing` already covers absence of a line).
- It is **not** a `503 Platform`: this is not an unresolved store-commerce/platform configuration; the fail-closed guard is deterministic Cart line state.
- Resulting repository convention: `Business` is the dominant Cart classification and `Business/409` is an existing, documented Cart pairing (`cart.pricing.quote_missing`, `cart.rejected`, `cart.line.merge_via_quantity`).

`Severity`/fallback follow the existing Cart expected-failure convention; `LocalizationKey` is the existing stable code, so the existing resx keys (both cultures) are reused unchanged.

## After

- `CartErrorCatalogContributor` registers **26** descriptors.
- `cart.line.currency_missing` is registered **exactly once** as `Business` / `409`.
- `checkout.authentication_required` remains Foundation-owned and consumed-not-registered by Cart.
- Duplicate descriptor ownership: **ZERO** (the composed catalog remains fail-fast and now covers every reachable Cart-owned code).
- Unregistered reachable Cart-owned codes: **ZERO**.

## Count truth

| Metric | Before | After |
| --- | --- | --- |
| Declared/consumed boundary codes (`CartErrorCodes`) | 27 | 27 |
| Cart-owned/registered descriptors | 25 | 26 |
| `checkout.authentication_required` | Foundation-owned, consumed-not-registered | Foundation-owned, consumed-not-registered |
| Unregistered reachable Cart-owned codes | 1 (`cart.line.currency_missing`) | 0 |
| Duplicate descriptor ownership | 0 | 0 |

## Bounded production delta

Exactly one production file changed:

- `src/backend/Modules/Cart/Tooba.Cart.Endpoints/Errors/CartErrorCatalogContributor.cs`

Delta: one `D(...)` descriptor added (plus its explanatory comment). No other production file touched.

Unchanged (proven by `git diff` in `validation.md`):

- error-code **values** — unchanged
- throw sites — unchanged
- routes / endpoints — unchanged
- DTO shapes — unchanged
- resx resources — unchanged (existing keys reused)
- schema / migrations — unchanged
- manifest **structural** fields (`projects`, `rootAllowlist`, `forbiddenRootFiles`, `forbiddenTopLevelFolders`, `structureCertified`, `lockVersion`) — unchanged (only `certificationNote` wording/count)

## Certification / SoT reconciliation

- Closed the previous `cart.line.currency_missing` residual risk.
- Preserved `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002`; no structure reopened.
- Preserved the W3-R1 bounded behavior truth and extended its status set with the newly proven `cart.line.currency_missing -> 409` mapping.
- Recorded this R2 as a **bounded expected-failure repair**, not `behaviorChange=NONE`.
- Metadata set:
  - `certificationReconciliation=W3_R2_LINE_CURRENCY_CATALOG_CLOSED`
  - `evidenceW3R2=docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3-R2/`
  - `workflowStop=USER_REVIEW_CART_AMSC_001_W3_R2`
  - `automaticNextImplementationTask=NONE`

## Durable guard

Extended the existing focused `CartModuleAmsc001W3CertGuardTests` to lock:

- `LineCurrencyMissing` descriptor present exactly once;
- Cart-owned descriptor count = 26 and contributor `D(` count = 26;
- the shared `checkout.authentication_required` is **not** registered by Cart;
- duplicate ownership = ZERO;
- SoT no longer calls `LineCurrencyMissing` unregistered (`unregisteredDeclaredCode = NONE`, `cartModuleAmsc001W3R2.unregisteredReachableCartCodeState = ZERO`);
- the R2 reconciliation record exists with `Business` / `409` classification and status.

No assertion was weakened and no baseline was widened.
