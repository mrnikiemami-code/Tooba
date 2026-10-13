# TB-P0-PAYMENT-IDEMPOTENCY-METHOD-BINDING-020 — bounded audit evidence

Task: audit (and, only if safely possible, fix) Payment-owned persisted idempotency replay so that a
reused payment idempotency key with a **different selected method** can never silently return the
previous method's payment or start a second charge. Mode `BOUNDED_IMPLEMENTATION_OR_BLOCKED`,
`STOP_FOR_ARCHITECT`, 20-minute timebox.

Verdict: **BLOCKED_ARCHITECT_DECISION** — the defect is confirmed with source-line evidence and is
reachable, but the only correct fail-closed behaviour requires a **new Payment-owned typed error
code**, because no declared Payment code is semantically exact for "this idempotency key is already
bound to another method". That new code must be declared, registered and localized, and those three
facts are pinned by **three certified `Tooba.Host.Tests` guards** which this task forbids editing
(28/24/24). Nothing unsafe was published: **zero production files were changed.**

## 1. Defect confirmation (source evidence)

`src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs`,
`InitiateAsync` (`:61`–`:210`):

| Evidence | Line |
| --- | --- |
| Replay lookup is by **key only** | `:64` `var key = command.IdempotencyKey.Trim();` / `:65` `FirstOrDefaultAsync(x => x.IdempotencyKey == key, ...)` |
| The replay gateway is derived from the **persisted** provider, not the request | `:79` `var replayGateway = _gateways.Resolve(existing.ProviderCode);` |
| A `Failed` prior payment is **re-initiated against the old provider** (new attempt + `SaveChanges`) | `:88` `await replayGateway.InitiateAsync(existing.PaymentId, existing.Amount, existing.Currency, ...)` |
| Every replay response reports the **persisted** provider | `:101`, `:115`, `:132` `existing.ProviderCode` |
| `command.ProviderCode` is consulted in the **fresh path only** | `:166` `var gateway = _gateways.Resolve(command.ProviderCode);` (no other occurrence in the method) |

Consequence matrix for the same key with a different selected method:

* prior **Pending** → silent wrong-method replay: the caller receives the previous method's payment
  and `ProviderCode` as if the new method had been honoured (`:128`–`:136`) — the "previous method's
  payment returned as a new-method success" the task forbids;
* prior **Failed** → a **new provider initiation and attempt** under the *old* method (`:86`–`:105`),
  i.e. a second charge-class action with the request's method silently discarded;
* prior **Succeeded** → returns the paid payment (benign: the checkout is genuinely paid; `:109`–`:120`).

**Secondary gap (same conflict class):** checkout identity is never compared. Access is validated
against the *persisted* payment's checkout (`:68` → `PaymentActorAccess.EnsureActorCanSeeAsync` →
`orders.GetPayableAsync(payment.CheckoutId, ...)`), while `command.CheckoutId` is used only in the
fresh path (`:139`, `:144`). A same-actor caller reusing a key across two of their own checkouts is
therefore returned the *first* checkout's payment with no identity comparison at all.

**Reachability:** `PaymentStorefrontEndpoints.InitiateAsync` accepts a client `Idempotency-Key`
(`src/backend/Modules/Payment/Tooba.Payment.Endpoints/Storefront/PaymentStorefrontEndpoints.cs:44`)
plus `body.ProviderCode` (`:51`) and forwards both unchanged. 019's composer derives the Payment key as
`start-payment:payment:<callerKey>` — namespaced by layer, **not** by method — precisely because 019
leaves method authority in Payment. One client key reused with another method therefore reaches the
defective branch.

## 2. Why no declared Payment code can be used (exhaustive check)

The declared set is exactly **24 registered descriptors** (`PaymentErrorCatalogContributor.Contribute()`)
plus **4 consumed foreign-owned codes** (`inventory.reservation.retry_limit_reached`,
`inventory.supply.unavailable`, `admin.authorization.denied`,
`checkout.authentication_required`). `PaymentErrorCodes.KnownCodes` contains those 28 and nothing else;
`PaymentErrors.resx` / `PaymentErrors.fa.resx` contain the same 24 keys. **No declared code has
idempotency/key-binding semantics.**

Candidates considered and rejected:

| Code | Why it is not semantically exact |
| --- | --- |
| `payment.already_succeeded` | means "a payment for this checkout already succeeded"; in the failing cases nothing succeeded (its copy is "این سفارش قبلاً با موفقیت پرداخت شده است") |
| `payment.method.unavailable` | means the method is not **offered by store configuration** ("این روش پرداخت در حال حاضر در دسترس نیست"); the offered method *is* available — using it is exactly the repurposing the task forbids and would misreport an available method |
| `payment.rejected` | declared as the **generic** rejected operation ("امکان انجام این عملیات در حال حاضر وجود ندارد") — the task forbids a generic error |
| `payment.unpaid.retry.invalid`, `payment.confirm.invalid_state`, `payment.reject.invalid_state` | manual/retry state machines, not key binding |
| `payment.access.denied`, `payment.guest.invalid` | actor/guest visibility, not key binding |

**Repository precedent settles the question:** Wallet owns the identical defect class and solves it
with a *dedicated* code — `wallet.idempotency_conflict` ("Idempotency key conflicts with a different
stored operation",
`src/backend/Modules/Wallet/Tooba.Wallet.Contracts/Errors/WalletErrorCodes.cs:212`), thrown when the
same key maps to a different stored operation (`WalletDirectory.Payments.cs:46`,
`WalletDirectory.Refunds.cs:38,50`), with en/fa resources. The canonical answer for Payment is a new
dedicated Payment code, not a reused one.

**The unsafe shortcut was also rejected on evidence:** throwing an unregistered code does not fail
closed with a domain error. `PaymentOperation` maps only codes in `PaymentErrorCodes.KnownCodes`
(`PaymentOperation.cs:29,45`); anything else propagates to the global boundary, and
`SafeErrorMapper.MapUnexpected` (`SafeErrorMapper.cs:162-179`) yields `platform.unexpected` with
**HTTP 500** (surfaced through `ApiResponseFactory.cs:91`). The task explicitly forbids a generic 500.
(A `SemanticException` is mapped without `IsKnown`, but its code is still absent from the catalog and
therefore still ends as `platform.unexpected`/500.)

Also rejected: scoping the idempotency key by method inside Payment (that would create a *second*
payment/charge for the same click — the opposite defect), and adding a persisted column
(schema/migration = forbidden).

## 3. Certified guards that pin the required code (must not be edited in this slice)

| Guard | Assertion |
| --- | --- |
| `src/backend/Host/Tooba.Host.Tests/Architecture/PaymentModuleAmsc001W1MigrateGuardTests.cs:237` | `Assert.Equal(28, declared.Length)` — every public const string in `PaymentErrorCodes` |
| `src/backend/Host/Tooba.Host.Tests/Architecture/PaymentModuleAmsc001W3CertGuardTests.cs:288` | `Assert.Equal(24, codes.Length)` — registered descriptors (uniqueness + foreign-owned exclusion) |
| `.../PaymentModuleAmsc001W3CertGuardTests.cs:320` | `Assert.Equal(24, codes.Length)` + en/fa presence and non-fallback localization for every registered code |

Declaring + registering + localizing the required code necessarily flips **28 → 29** and **24 → 25**
in those three certified Host.Tests assertions. The task forbids touching them, and no equivalent
guard exists in the editable Payment test project. Therefore the fix cannot be completed inside this
authorized slice without publishing either an unsafe unregistered code (500) or a guard-breaking
change.

## 4. Exact file-level change proposal (needs Architect authorization)

1. `src/backend/Modules/Payment/Tooba.Payment.Contracts/Errors/PaymentErrorCodes.cs`
   add `public const string IdempotencyConflict = "payment.idempotency.conflict";` with doc
   "the reused idempotency key is already bound to a different payment operation", and add it to
   `KnownCodes` (declared 28 → 29).
2. `src/backend/Modules/Payment/Tooba.Payment.Endpoints/Errors/PaymentErrorCatalogContributor.cs`
   add `D(PaymentErrorCodes.IdempotencyConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Conflict")`
   (registered 24 → 25).
3. `src/backend/Modules/Payment/Tooba.Payment.Contracts/Resources/PaymentErrors.resx` — one key.
4. `src/backend/Modules/Payment/Tooba.Payment.Contracts/Resources/PaymentErrors.fa.resx` — one key.
5. `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs` — in the
   replay block, **immediately after `:79` and before `:80`** (i.e. before `HasSucceeded…`, before the
   `Failed` gateway call and before every return):

   ```
   var requestedProvider = _gateways.Resolve(command.ProviderCode).ProviderCode;
   if (!string.Equals(requestedProvider, existing.ProviderCode, StringComparison.OrdinalIgnoreCase)
       || command.CheckoutId != existing.CheckoutId)
   {
       throw new ContractOperationException(PaymentErrorCodes.IdempotencyConflict);
   }
   ```

   The comparison **must** use the registry-resolved canonical code rather than the raw string:
   the storefront orchestrator resolves the `gateway` selector to `_catalog.DefaultProvider` at
   `StorefrontPaymentOrchestrator.cs:151` *before* persistence, and the registry is keyed by each
   gateway's own `ProviderCode` (`PaymentGatewayRegistry` in
   `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/FakePaymentGateway.cs:107-127`).
   Verified consequence: identical
   selector clicks resolve identically, so **legitimate exact-method replays are not denied**, and no
   gateway is registered under the literal selector `gateway` (registered codes: `fake`, `fake-fail`,
   `manual`, `wallet`, `webhook`, `fail-closed`); non-orchestrator callers already pass concrete codes
   (`Host.Tests`: `"fake"`, `"fake-fail"`).
6. Guard updates in the same authorized wave (forbidden here):
   `PaymentModuleAmsc001W1MigrateGuardTests.cs:237` 28 → 29;
   `PaymentModuleAmsc001W3CertGuardTests.cs:288` and `:320` 24 → 25.
7. Test seam caveat for the Architect: the editable Payment test project has **no** DB-backed
   `PaymentDirectory` seam today (only validator/component tests mention `IdempotencyKey`), so the
   required behaviour tests (exact-method repeat returns the existing payment/attempt with **zero**
   gateway invocation; changed-method repeat rejected before money movement; `Failed`-prior +
   changed method → no gateway call and no new attempt; unauthorized replay still fails closed;
   wallet/manual paths unchanged) need either an approved in-memory `PaymentDbContext` seam in
   `Tooba.Payment.Tests` or a different authorized seam — Host.Tests must not be used for this.

## 5. Scope honesty / what was deliberately not done

* **Zero production files changed; zero tests added or modified.** No build and no test run was
  performed, because the slice is inert by construction and the task says to avoid repeated
  builds/tests and to STOP when no safe typed error / trustworthy seam exists.
* Host untouched (no file under `src/backend/Host/**` read-modify or edited); no Host build,
  stop/restart/deploy, and no contact with `127.0.0.1:5088`. Host.Tests were **not** executed — the
  three guard assertions above are cited from their source, as static evidence.
* No schema/migration/settings/config change, no DB mutation, no PSP/wallet call, no Order commit or
  Inventory extension change, no provider eligibility/routing/timeout/hold-duration change, no
  actor-policy weakening, no error-catalog repurposing, no new endpoint, no frontend change.
* Observation only (out of scope, left unchanged): `PaymentDirectory` already throws several
  **unregistered** `payment.*` literals — `payment.checkout.not_payable` `:145`,
  `payment.initiate.reservation_forbidden` `:148`, `payment.currency.mismatch` `:160`, and
  `payment.access.order_identity_required` via `PaymentActorAccess`. Because they are absent from
  `KnownCodes`, they bypass `PaymentOperation` mapping and surface as `platform.unexpected`/500 today.
  This is recorded for the Architect; it was not touched.
* Concurrency: the key is protected by the unique index `ix_payments_idempotency_key`
  (`PaymentDbContext.cs:77`; migration `20260823140000_InitialPayment.cs:121`). Two simultaneous
  distinct-method initiations with the same key can both pass the pre-read, and the loser fails on
  insert (`DbUpdateException`) rather than persisting a second method — but that race is **not**
  closed by the proposal's pre-write comparison, and it remains an honest unresolved risk. No
  cross-module ACID or replay-race guarantee is claimed here.
