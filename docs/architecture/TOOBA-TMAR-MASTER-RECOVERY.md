TOOBA TMAR MASTER RECOVERY

Current Golden Wave Closure (authoritative)

- Locks: ARCH-COMPLETE-002, HOST-MODULE-ENDPOINT-001, ARCH-CQRS-001/002
- Execution: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE; frontendFrozen = true
- Golden wave: COMPLETE + USER_ACCEPTED; 12 COMPLETE_REFERENCE_PATTERN modules
- Complete modules: Cart, Settlement, Fulfillment, Returns, Notification, Support, Wallet, Payment, Promotion, Offer, Order, Inventory
- HTTP-owning: Cart, Settlement, Fulfillment, Returns, Notification, Support, Wallet, Payment, Promotion, Offer, Order — MODULE_ENDPOINTS + MEDIATR_12_5
- Inventory = INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES
- reopenedModules = empty; internalApplicabilityReviewModules = empty; activeModuleRecovery = empty
- Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
- Order: COMPLETE_REFERENCE_PATTERN after TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE (prior repair lineage: TB-TMAR-ORDER-GOLDEN-001-R11-R1; TB-TMAR-ORDER-GOLDEN-001-R11; TB-TMAR-ORDER-GOLDEN-001-R10; TB-TMAR-ORDER-GOLDEN-001-R9; TB-TMAR-ORDER-GOLDEN-001-R8; TB-TMAR-ORDER-GOLDEN-001-R7; TB-TMAR-ORDER-GOLDEN-001-R6; TB-TMAR-ORDER-GOLDEN-001-R5-R1; TB-TMAR-ORDER-GOLDEN-001-R5; TB-TMAR-ORDER-GOLDEN-001-R4-R1; TB-TMAR-ORDER-GOLDEN-001-R4; TB-TMAR-ORDER-GOLDEN-001-R3B; TB-TMAR-ORDER-GOLDEN-001-R3)
- Post-closure quality: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1 (complete transport FluentValidation coverage; Order remains COMPLETE_REFERENCE_PATTERN)
- Post-closure structure: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001 (Order.Endpoints capability foldering); TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002 (Order.Infrastructure capability + integration foldering); TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001 (ARCH-COMPLETE-002 structure lock; Order STRUCTURE_CERTIFIED)
- Cart Host residual: TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001 (HOST_CART_ILLEGAL_AUTHORITY = 0; Cart expiry + persistence policy Cart-owned; Host worker shell only; no broad Cart global usings; Cart remains COMPLETE_REFERENCE_PATTERN)
- Cart structure + validation: TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001 (Cart physical structure CERTIFIED against ARCH-COMPLETE-002; explicit root allowlists; path↔namespace aligned; Cart.Application foreign boundaries clean; 4 endpoint-reachable requests VALIDATOR_REQUIRED with concrete FluentValidation validators; 3 requests explicitly NO_VALIDATOR_REQUIRED; validator coverage guard added; Cart = COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 STRUCTURE_CERTIFIED); TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001-R1 (SoT consistency repair: hostCartBoundary.structureCertifiedUnderArchComplete002 = true; completeReferenceModules.Cart acceptance metadata points to the certification lineage; durable guard asserts certifiedModules ↔ manifest agreement and Cart SoT coherence)
- Cart post-certification semantic/ownership repair: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001 (Host owns ZERO Cart implementation: HostCartPersistenceHoursResolver + CartExpiryHostedService + CartExpiryHostOptions removed; Cart-owned CartExpiryWorker + CartExpiryOptions + CatalogCartPersistenceHoursResolver; persistence-hours path fully async with cancellation; CreateGuestCart resolves effective Market/Currency/SalesChannel from canonical commerce context + Cart-owned defaults; Cart presentation no longer hardcodes Persian fallbacks; validator classification unchanged; not a new structural certification)
- Cart commerce-authority repair: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1 (CartCommerceDefaultsOptions removed; Cart owns no Market/Currency/SalesChannel policy default and no code-level SalesChannel literal; effective storefront commerce authority is platform-owned StoreCommerceContext on the canonical commerce context, resolved by the Host control-plane registry for request and worker paths; CartCommerceContextResolver consumes it and fails closed with stable cart.commerce.* codes; per-store/shared-DB resolution no longer collapses to one global Cart currency; prior Host closure preserved; Cart remains COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 STRUCTURE_CERTIFIED)
- Cart store-commerce fail-fast: TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001 (Production startup now fails fast on incomplete/invalid StoreCommerce instead of failing later in Cart: Marketplace requires deployment Market/Currency/SalesChannel; SingleStore requires complete effective commerce for every ACTIVE tenant with Disabled/Suspended skipped; SalesChannel validated at startup against canonical Tooba.Offer.Contracts.Dtos.SalesChannel - no second enum, no hardcoded Direct/Marketplace fallback; Currency requires non-empty configured value with no invented currency list; Market keeps existing DefaultMarketReference fallback; no runtime inheritance semantics change; Cart code untouched; Host still owns zero Cart-specific implementation; Cart remains COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 STRUCTURE_CERTIFIED)
- Next task: USER_REVIEW_CART_STORE_COMMERCE_FAILFAST_001
- StoreContext foundation extraction: TB-TMAR-STORECONTEXT-FOUNDATION-001 (effective store commerce context moved out of Tooba.BuildingBlocks into Tooba.StoreContext.Contracts (StoreCommerceContext + ICurrentStoreCommerceContext + IStoreCommerceContextAssigner + IWorkerStoreCommerceContextFactory) and Tooba.StoreContext.Infrastructure (scoped StoreCommerceContextAccessor + StoreContextModule); TenantId/ToobaEdition/ConnectionReference/CommerceContext remain platform primitives in BuildingBlocks; request path assigns StoreContext separately from technical CommerceContext; worker path (Outbox dispatcher + CartExpiryWorker) assigns it through the StoreContext assigner using a thin Host IWorkerStoreCommerceContextFactory adapter over the control-plane registry; Cart consumes only StoreContext.Contracts and keeps zero Host dependency; parent fail-fast preserved; no new default; no Shared-DB claim; StoreContext = FOUNDATION_EXTRACTED_NOT_YET_STRUCTURE_CERTIFIED)
- StoreContext golden hardening: TB-TMAR-STORECONTEXT-GOLDEN-001 (StoreCommerceContext.Currency renamed to StoreCommerceContext.DefaultCurrency: default/preferred storefront selection input ONLY, never a transaction/line/order/settlement/payment-group currency and never a single-currency Cart/Order invariant; canonical config key StoreCommerce:DefaultCurrency with SingleStore:Tenants:<n>:StoreCommerce:DefaultCurrency and no silent Currency alias; Market fallback + SalesChannel startup validation + Disabled/Suspended skip preserved; Cart adapter wording only - CartCommerceContext.Currency renamed to DefaultCurrency and CreateGuestCart passes context.DefaultCurrency - no ShoppingCart/CartLine/Pricing behavior change; StoreContext declared PLATFORM_CONTEXT_REFERENCE_PATTERN / INTERNAL_ONLY / endpointOwnership NOT_APPLICABLE / cqrs NOT_APPLICABLE_NO_APPLICATION_USE_CASE and structure-certified under ARCH-COMPLETE-002 with NO ceremonial Application/Endpoints/MediatR; Known residual debt: Cart still carries a single cart pricing currency - NOT repaired here; Cart and Order certifications preserved; frontend untouched; StoreContext = ARCH-COMPLETE-002 STRUCTURE_CERTIFIED)
- StoreContext golden ACCEPTED: TB-TMAR-STORECONTEXT-GOLDEN-001 (Architect-ACCEPTED at f2667a249d43fb542903a08b429cd1ea8e219704; StoreCommerceContext.DefaultCurrency default-selection semantics; StoreContext = PLATFORM_CONTEXT_REFERENCE_PATTERN / INTERNAL_ONLY / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED)
- Cart multi-currency bounded audit: TB-TMAR-CART-MULTICURRENCY-AUDIT-001 (AUDIT-ONLY; no production code changed; produces docs/evidence/TB-TMAR-CART-MULTICURRENCY-AUDIT-001/cart-multicurrency-audit.md mapping every cart-level Currency use, CartLine.QuotedCurrency truth, the PRICING_CURRENCY_SELECTION_BLOCKER, the MULTICURRENCY_INVALID_TOTAL presentation sites, contract change map and DB impact; deferring Order/Checkout/Payment)
- Cart multi-currency line slice: TB-TMAR-CART-MULTICURRENCY-LINES-001 (implements the audit's single Cart slice: cart level currency is no longer transaction authority - ShoppingCart.DefaultCurrency/CartSnapshot.DefaultCurrency are default-selection metadata only; the physical DB column stays currency with no migration; add-line accepts an optional requested currency otherwise the cart default; existing lines requote in their own sticky QuotedCurrency and missing line currency fails closed as cart.line.currency_missing; merge uses line truth and never the cart default; CartPage replaces the scalar Currency/SubtotalExclusiveOfTax with DefaultCurrency + TotalsByCurrency; Pricing Contracts untouched and remain the quote authority; PRICING_CURRENCY_SELECTION_BLOCKER resolved as an explicit selector passed into the existing Pricing contract; Order/Checkout/Payment multi-currency explicitly deferred)
- Cart multi-currency Order compatibility repair: TB-TMAR-CART-MULTICURRENCY-LINES-001-R1 (parent verdict: NOT YET ACCEPTED, both defects confirmed on main - Order shipping summed cart.TotalsByCurrency and Order checkout used cart.DefaultCurrency as transaction/order/pricing currency even when the sole CartLine currency differed; repair: StorefrontCartCurrencyCompatibility requires exactly one distinct non-empty line currency (CartSnapshot.QuotedCurrency / CartPage line Currency), never falls back to DefaultCurrency, and fails closed with the typed stable checkout.multicurrency.not_supported; shipping reads only the matching per-currency total with no cross-currency sum; StorefrontCheckoutService rejects mixed/missing currency before preview/submit economics; CheckoutDirectory and CheckoutSubmitHost derive CheckoutGroup/SellerOrder currency, FinancialRounder.MoneyPlaces and the campaign/base Pricing selector from the sole line currency; Cart production code, Pricing contracts, Payment and frontend untouched; Cart multi-currency line slice = ACCEPTED_WITH_ORDER_SINGLE_CURRENCY_COMPATIBILITY_GUARD; Order boundary = SINGLE_CURRENCY_ONLY_FAIL_CLOSED_UNTIL_DEDICATED_WAVE; Order multi-currency = DEFERRED; Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT; Cart/Order/StoreContext certifications unchanged)
- Offer ARCH-COMPLETE-002 bounded audit ACCEPTED: TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001 (Architect-ACCEPTED at 7a0b36061db85fc4ba57e117bcf50d9ee390fa73; audit-only, no production code change; Host residue classified; Offer structure certification split into a separate follow-up)
- Offer Host residue repair: TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001 (Host no longer owns Offer business selection policy or hidden Offer type aliases: StorefrontPrimaryOfferResolver.cs deleted and replaced by the Offer-owned boundary Tooba.Offer.Contracts/Dtos/OfferSelectionCandidate.cs + Tooba.Offer.Contracts/Ports/IPrimaryOfferSelectionPolicy.cs implemented by Tooba.Offer.Application/Policies/PrimaryOfferSelectionPolicy.cs and registered as a Singleton by OfferModule; StorefrontComposer consumes the Contracts port at the 3 audited call sites through an explicit selection-only mapping while StorefrontOfferCandidate stays Host presentation shape; OfferGlobalUsings.cs deleted and the exact 9 audited consumers now compile through their existing explicit Tooba.Offer.Contracts.Dtos using; Host .tmp-t014-test-out dead residue deleted; HostOfferSellerAuthorizer.cs remains the allowed thin Host security adapter with no ranking/pricing/inventory/persistence/response/business call; Offer -> Host dependency remains zero; Host residue = BUSINESS_RESIDUE_REMOVED_THIN_SECURITY_ADAPTER_ONLY; selection owner = OFFER_CONTRACT_PORT_APPLICATION_POLICY; global alias = REMOVED; temp residue = REMOVED; behavior parity preserved; structure certification = PENDING_TB_TMAR_OFFER_ARCH_COMPLETE_002_STRUCTURE_001; evidence docs/evidence/TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001/offer-host-residue-repair.md)
- Offer ARCH-COMPLETE-002 structure certification: TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001 (Offer structure-certified under ARCH-COMPLETE-002: Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs namespace aligned to path as Tooba.Offer.Contracts.Errors and the exact Offer consumers qualified (no project-wide/global alias); five transport FluentValidation validators added under Tooba.Offer.Application/Validators (GetOfferQueryValidator, CreateOfferCommandValidator, UpdateOfferCommandValidator, SetOfferPriceCommandValidator, SetOfferInventoryCommandValidator) with ListSellerOffersQuery explicitly classified NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY because SellerPartyId comes from the trusted seller authorization boundary; validators shape transport input only and duplicate no DB uniqueness/seller/catalog existence/return-policy governance/domain invariant; discovery stays the existing MediatR validation pipeline with MediatR 12.5.0 and no direct endpoint/handler validator invocation; Application/Mappings and Infrastructure/Adapters/Tracing namespaces aligned exactly to path; the physical structure guard now enforces exact path-derived namespace equality, an alias-workaround check and the Contracts/Validators folder alignment; new OfferEndpointValidatorCoverageGuardTests inventories exactly six endpoint-reachable requests; manifest adds Offer as structureCertified with root allowlists and forbidden files and Offer removed from uncertifiedHttpOwningModules; structureLock.certifiedModules = Order, Cart, StoreContext, Offer; Host Offer business residue stays removed and HostOfferSellerAuthorizer stays a thin security adapter; Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT; frontendFrozen = true; evidence docs/evidence/TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001/offer-structure-certification.md)
- Payment ARCH-COMPLETE-002 bounded audit: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001 (AUDIT-ONLY; no Payment or Host production code changed; Payment remains COMPLETE_REFERENCE_PATTERN / HTTP_OWNING / MODULE_ENDPOINTS / MEDIATR_12_5 but NOT yet ARCH-COMPLETE-002 structure-certified; all five Host Payment residue files classified - HostPaymentAdminAuthorizer + HostPaymentStorefrontAuthorizer = KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER, HostPaymentAdminGridQueryNormalizer + PaymentReconciliationHostOptions + PaymentReconciliationHostedService = MOVE_TO_PAYMENT_MODULE; reconciliation worker decision = PAYMENT_INFRASTRUCTURE_OWNS_WORKER_AND_OPTIONS mirroring CartExpiryWorker/CartModule and the ReservationCycleOptions precedent; grid normalizer decision = PAYMENT_OWNS_GRID_WHITELIST with only the thin Host panel authorizer kept, mirroring the Settlement/Returns/Fulfillment host-grid removals; 16 endpoint-reachable MediatR requests plus 1 worker-only ReconcileStalePaymentsCommand, all real IRequest/ISender with zero direct Directory/Application calls; 15 VALIDATOR_REQUIRED and all 15 validators MISSING, 1 NO_VALIDATOR_REQUIRED_NO_INPUT, worker request = NO_VALIDATOR_REQUIRED_INTERNAL_WORKER; root .cs files empty in all five Payment projects and path-namespace already EXACT, migrations/snapshot exempt; Application/Orchestration/GlobalUsings.cs is a self-import alias that needs justification not removal; PaymentDirectory.cs = 879-LOC 4-interface god-file flagged for extraction; cross-module references are Contracts-only (Wallet/Order/Media) with no foreign Application/Infrastructure/Domain and no Host dependency; PaymentHostContractBridge classified RENAME_INTERNAL_PAYMENT_BRIDGE (not renamed); four [Obsolete] unused ports identified as dead residue; guard gaps listed (StartsWith namespace checks, no alias check, no root allowlists, no validator coverage, no exhaustive request inventory); split decision = TWO_TASKS; evidence docs/evidence/TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001/payment-arch-complete-002-audit.md)
- Payment Host-residue repair: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001 then TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1 (Host no longer owns Payment reconciliation runtime or the admin payments grid whitelist: PaymentReconciliationHostedService.cs + PaymentReconciliationHostOptions.cs deleted from Host and replaced by the Payment-owned Tooba.Payment.Infrastructure/Workers/PaymentReconciliationWorker.cs + PaymentReconciliationOptions.cs (SectionName Tooba:PaymentReconciliation, canonical defaults Enabled=true / PollIntervalSeconds=60 / PendingAgeMinutes=5 / BatchSize=20; effective minimum poll interval 15s matching pre-move Host behavior, PendingAge min 1 minute, non-positive BatchSize -> 20); PaymentModule now binds the options and calls AddHostedService<PaymentReconciliationWorker>(), duplicate registrations cleaned; HostPaymentAdminGridQueryNormalizer.cs + the Host Grid/AdminListGridPolicies.Payments policy + the now-dead Host AdminReceiptListItem row model deleted and replaced by the Payment-owned Tooba.Payment.Endpoints/Admin/PaymentAdminGridQueryNormalizer.cs implementing IPaymentAdminGridQueryNormalizer over BuildingBlocks.Grid only (exact whitelist reference/customer/amount/status/supply/reservation/provider/created/completed, default sort created desc) with a single catch (GridQueryValidationException ex) -> SemanticException(ex.ErrorCode) boundary so every grid structural error surfaces as a stable semantic code (grid.filter.field.invalid / grid.filter.operator.invalid / grid.advancedFilter.field.invalid / grid.advancedFilter.connector.count / grid.advancedFilter.connector.invalid) and maps centrally to HTTP 400 instead of 500, registered as Singleton by PaymentEndpointModule.AddPaymentEndpointPresentation; Host retains only the two approved thin security adapters HostPaymentAdminAuthorizer + HostPaymentStorefrontAuthorizer; generic worker seams stay Host process adapters and Payment.Infrastructure keeps ZERO Tooba.Host reference; runtime behavior parity restored and proven; focused Payment worker/options/grid tests + strengthened Payment host-residue guard added; Payment remains COMPLETE_REFERENCE_PATTERN and NOT yet ARCH-COMPLETE-002 structure-certified; Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT; frontendFrozen = true; structure certification = PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001)
- Accepted parent: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1 at f43904967311a3ac3e3f2cca773af3c733458a76 (parent repair ACCEPTED_AFTER_R1_BEHAVIOR_PARITY_REPAIR: min poll cadence 15s restored, grid validation errors all stable semantic 400)
- Accepted parent: TB-TMAR-PAYMENT-PRECERT-HYGIENE-001 at 22849a17e31aa3357277c3979ac351a3f3f9f8e5 (Payment pre-cert hygiene ACCEPTED: dead ports removed, PaymentContractBridge rename, typed Payment/Wallet faults, locale-neutral admin grid)
- Payment pre-cert directory split R1 (runtime DI proof repair): TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1 (parent production decomposition ACCEPTED_AFTER_R1_RUNTIME_DI_RESOLUTION_PROOF; the parent's Directory_ports_resolve_to_distinct_focused_implementations test inspected IServiceCollection descriptors only and never built a provider, so the required four-port resolution proof was strengthened in place with NO Payment production change: the focused test now builds a real ServiceProvider over the Payment module registration with ValidateOnBuild + ValidateScopes, creates a real IServiceScope, resolves IPaymentDirectory / IPaymentReconciliationDirectory / IPaymentAdminDirectory / IPaymentExpiryDirectory from runtime DI, asserts the exact concrete runtime types PaymentDirectory / PaymentReconciliationDirectory / PaymentAdminDirectory / PaymentExpiryDirectory, asserts four distinct instances and four distinct runtime types, proves scoped caching in one scope and fresh instances in a second scope, and resolves the admin port first so a broken core<->admin cycle would fail for real rather than pass on registrations alone; the existing reflection assertion that each concrete class implements only its own single directory port is preserved; focused runtime DI test PASS; Tooba.Payment.Tests project build succeeded with 0 errors; production DI unchanged; no construction cycle observed; Payment -> Host remains ZERO; Payment remains NOT structure-certified; next task = TB-TMAR-PAYMENT-PRECERT-VALIDATION-001 with validation coverage NOT claimed here; evidence docs/evidence/TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1/runtime-di-resolution-proof.md)
- Payment pre-cert storefront validation: TB-TMAR-PAYMENT-PRECERT-VALIDATION-001 (first Payment validator wave, storefront slice only: the target folder src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Storefront (namespace Tooba.Payment.Application.Validators.Storefront) now holds exactly nine FluentValidation transport validators — InitiateStorefrontPaymentCommandValidator, GetStorefrontWalletQuoteQueryValidator, GetStorefrontPaymentQueryValidator, GetStorefrontPaymentSandboxContextQueryValidator, CompleteSandboxPaymentCommandValidator, SubmitManualPaymentEvidenceCommandValidator, RetryManualPaymentCommandValidator, RetryUnpaidPaymentCommandValidator, UploadManualPaymentProofCommandValidator — while ListStorefrontPaymentMethodsQuery is explicitly classified NO_VALIDATOR_REQUIRED_NO_INPUT and has no validator; validation is primitive/transport shape only with ownership/authorization/DB existence/gateway availability/payment state/eligibility/payable amount left to Application/Domain; two reusable helpers PaymentValidationCodes + PaymentFluentRules carry the stable payment.validation.* codes; discovery uses ONLY the existing AddValidatorsFromAssembly via AddToobaCqrsFoundation with MediatR still 12.5.0 and no manual endpoint/handler validator invocation; Admin + Webhook validator coverage was closed by TB-TMAR-PAYMENT-PRECERT-VALIDATION-002; Payment -> Host remains ZERO; Payment still NOT structure-certified; evidence docs/evidence/TB-TMAR-PAYMENT-PRECERT-VALIDATION-001/storefront-validation.md)
- Payment pre-cert Admin/Webhook validation: TB-TMAR-PAYMENT-PRECERT-VALIDATION-002 (second and final Payment validator wave, closing the remaining endpoint-reachable transport coverage: the folder src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Admin (namespace Tooba.Payment.Application.Validators.Admin) now holds exactly five validators — GetAdminPaymentQueryValidator, ReconcileAdminPaymentCommandValidator, ConfirmAdminDepositCommandValidator, RejectAdminDepositCommandValidator, QueryAdminPaymentsGridQueryValidator — and src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Webhooks (namespace Tooba.Payment.Application.Validators.Webhooks) holds exactly one — ProcessPaymentWebhookCommandValidator; the four PaymentId-only Admin requests validate PaymentId != Guid.Empty while admin authorization stays in the Host/Payment admin authorizer; QueryAdminPaymentsGridQueryValidator shapes only the primitive grid envelope (Input not null, Input.Filters not null, SortField non-blank, SortDirection non-blank, Page >= 1, PageSize >= 1) and deliberately does NOT duplicate grid-normalizer policy (no field/operator/connector/sort whitelist, no search or supply/reservation enrichment semantics — those stay owned by PaymentAdminGridQueryNormalizer with its stable semantic grid codes); ProcessPaymentWebhookCommandValidator shapes only the transport envelope (ProviderCode non-blank, RawBody not null and length > 0, BodyText non-blank, SignatureHeader nullable but never whitespace-only) and deliberately does NOT parse JSON, validate payload/provider-event fields, verify signatures, call IPaymentWebhookSignatureVerifier, or invent provider allowlists/signature format rules — those stay in the existing handler and signature-verifier boundary; a focused PaymentValidatorCoverageGuardTests now proves the COMPLETE endpoint inventory of exactly 16 requests (Storefront 10 = 9 required + ListStorefrontPaymentMethodsQuery NO_VALIDATOR_REQUIRED_NO_INPUT, Admin 5 required, Webhook 1 required) with 15/15 validators resolvable to their exact concrete validator via foundation DI, all 16 real MediatR requests, ISender-based endpoints, and no direct validator usage in Payment.Endpoints or the relevant handlers; the worker-only ReconcileStalePaymentsCommand remains explicitly excluded from the endpoint inventory and NO_VALIDATOR_REQUIRED_INTERNAL_WORKER; discovery still uses only AddValidatorsFromAssembly via AddToobaCqrsFoundation with MediatR 12.5.0; focused in-memory Admin/Webhook validator tests PASS and the Payment test project builds; Payment -> Host remains ZERO; Host Payment residue unchanged (HostPaymentAdminAuthorizer + HostPaymentStorefrontAuthorizer only); PaymentContractBridge and the focused directory split intact; no schema/migration change; Payment still NOT structure-certified; evidence docs/evidence/TB-TMAR-PAYMENT-PRECERT-VALIDATION-002/admin-webhook-validation.md)
- Payment ARCH-COMPLETE-002 structure certification: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001 (Payment is now ARCH-COMPLETE-002 STRUCTURE_CERTIFIED under the same lock as Order/Cart/StoreContext/Offer: the live physical folder set satisfies APPLICATION_CAPABILITY_FOLDERS (Commands/Queries/Errors/Models/Orchestration/Ports/Validators), ENDPOINTS_CAPABILITY_FOLDERS (Admin/Errors/Storefront/Webhooks), INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS (Adapters/DependencyInjection/Directories/Events/Messaging/Persistence/Providers/Workers); PATH_NAMESPACE_ALIGNMENT is now EXACT and guarded — PaymentArchitectureGuardTests replaces the old loose namespace-prefix/StartsWith acceptance with exact path-derived namespace equality for every Payment production .cs file (Application/Validators/{Admin,Storefront,Webhooks} => Tooba.Payment.Application.Validators.*, Infrastructure/Directories/Shared => Tooba.Payment.Infrastructure.Directories.Shared, Infrastructure/Workers => Tooba.Payment.Infrastructure.Workers, Endpoints/Webhooks => Tooba.Payment.Endpoints.Webhooks, etc.) with EF Migrations/ModelSnapshot keeping their legitimate migrations-namespace exemption; ROOT_ALLOWLIST is explicit and enforced — Application rootAllowlist [] with forbidden root files (PaymentContracts, PaymentHandlers, PaymentRequests, PaymentQueries, PaymentQueryHandlers, StorefrontPaymentOrchestrator, PaymentErrorCodes), Endpoints rootAllowlist [PaymentEndpointModule.cs] with the flattened endpoint/authorizer/normalizer/error-resources/localizer root files forbidden, Infrastructure rootAllowlist [] with PaymentModule/PaymentDbContext/PaymentDirectory/PaymentAdminDirectory/PaymentReconciliationDirectory/PaymentExpiryDirectory/PaymentEvents/PaymentOutboxRegistration/PaymentReconciliationWorker/PaymentReconciliationOptions/PaymentGatewayRegistry forbidden at project root; NO_NAMESPACE_ALIAS_WORKAROUND is enforced — no type-forwarding/compat shims, no foreign-module global aliases, and the single remaining Tooba.Payment.Application/Orchestration/GlobalUsings.cs self-namespace import is retained as a narrowly justified, explicitly recorded exception that hides no physical path mismatch and imports no foreign layer; validator coverage is preserved as a certification prerequisite via PaymentValidatorCoverageGuardTests (16 endpoint-reachable requests, 15/15 required validators present, 1 NO_VALIDATOR_REQUIRED_NO_INPUT, worker-only ReconcileStalePaymentsCommand remains NO_VALIDATOR_REQUIRED_INTERNAL_WORKER, all requests IRequest, endpoints ISender-only, MediatR 12.5.0, no direct validator invocation); cross-module/Host boundaries unchanged — Payment -> Host ZERO, Host Payment residue stays exactly HostPaymentAdminAuthorizer + HostPaymentStorefrontAuthorizer thin security adapters, no reconciliation worker/options/grid policy returned to Host; the four focused directory classes each still implement exactly one of the four ports with PaymentDirectory <700 LOC; tmar-module-structure-manifests.json certifies Payment (structureCertified true, lockVersion ARCH-COMPLETE-002, the three project allowlists/forbidden sets above) and Payment is removed from uncertifiedHttpOwningModules; structureLock.certifiedModules = Order, Cart, StoreContext, Offer, Payment; no schema/migration change; Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT and frontendFrozen = true; evidence docs/evidence/TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001/payment-structure-certification.md)
- Settlement ARCH-COMPLETE-002 bounded audit (+R1 classification repair): TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001 then TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001-R1 (AUDIT-ONLY; zero Settlement/Host production code change; Settlement stays COMPLETE_REFERENCE_PATTERN / HTTP_OWNING / MODULE_ENDPOINTS / MEDIATR_12_5 and NOT yet ARCH-COMPLETE-002 certified; exhaustive endpoint inventory = exactly 10 endpoint-reachable MediatR requests (Seller 5: RequestSellerPayoutCommand, GetSellerSettlementBalanceQuery, ListSellerSettlementEntriesQuery, ListSellerSettlementStatementsQuery, ListSellerPayoutRequestsQuery; Admin 5: ProcessAdminPayoutCommand, RetryAdminPayoutCommand, ListAdminSettlementBalancesQuery, ListAdminPayoutQueueQuery, QueryAdminPayoutGridQuery) with 10 real IRequestHandler implementations, all dispatched through ISender, all endpoint bodies thin (authorizer + ISender + ApiResponseFactory only, no direct Application/Directory/DbContext call), zero worker/internal-only requests; correct validator classification after Architect repair = 4 VALIDATOR_REQUIRED / 6 NO_VALIDATOR_REQUIRED — required are RequestSellerPayoutCommand (untrusted body Amount, IdempotencyKey), ProcessAdminPayoutCommand (untrusted route PayoutRequestId), RetryAdminPayoutCommand (untrusted route PayoutRequestId) and QueryAdminPayoutGridQuery (untrusted GridQueryRequest envelope only, grid policy still owned by AdminPayoutGridQueryPolicy), with 0 present and 4 missing; the other six are NO_VALIDATOR_REQUIRED = four seller queries NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY because SellerPartyId comes only from ISettlementSellerAuthorizer and there is no untrusted payload, plus two parameterless admin queries NO_VALIDATOR_REQUIRED_NO_INPUT, matching the accepted Offer auth-scoped precedent and requiring no ceremonial empty validators; ActorUserId/SellerPartyId values from trusted authorizers must never be the reason for a validator; Settlement has NO Validators folder, NO FluentValidation reference and NO validator anywhere, so the existing AddToobaCqrsFoundation/AddValidatorsFromAssembly discovery is not exercised for Settlement; physical structure is already capability-grouped (Application Commands/Queries/Errors/Models/Ports; Endpoints Admin/Seller; Infrastructure Adapters/Bridges/DependencyInjection/Directories/Errors/Gateways/Handlers/Messaging/Observability/Persistence/Queries) with root .cs limited to GlobalUsings files and SettlementEndpointModule.cs; exact path-derived namespace scan across all five production projects found zero mismatches, no GlobalUsings in Endpoints, and only legitimate project-wide imports (Application/Infrastructure GlobalUsings.Domain + GlobalUsings.Layout, no aliases, no TypeForwardedTo, no foreign-module global alias); Host Settlement residue = exactly two thin security adapters (HostSettlementAdminAuthorizer, HostSettlementSellerAuthorizer) KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER, with SettlementDbContext reachable only from the Program/ModuleMigrationRegistry/MarketplaceDevelopmentBootstrap allowlist, no Host grid/panel/endpoints/settlement business logic, and Settlement -> Host dependency ZERO; cross-module references are Contracts-only (Order/Payment/Returns/Party Contracts) with no foreign Application/Domain/Infrastructure or DbContext; existing guards prove approximate folder-name dumping-ground and a loose StartsWith namespace prefix plus selected Host/route/exception boundaries but do NOT prove exact path-derived namespaces, root allowlists, alias-workaround rejection, exhaustive endpoint inventory, validator coverage, MediatR version or ISender-only invariants; audit decision = NEEDS_PRECERT_REPAIR_THEN_STRUCTURE with repair scope = one bounded Settlement validator wave of exactly four primitive-shape transport validators discovered via the existing AddToobaCqrsFoundation/AddValidatorsFromAssembly followed by the Settlement structure-certification task that adds exact-namespace/root-allowlist/alias/exhaustive-inventory guards; Settlement NOT certified here, no repair executed, no guard strengthened; focused SettlementArchitectureGuardTests PASS and Tooba.Settlement.Tests builds; evidence docs/evidence/TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001/settlement-arch-complete-002-audit.md)
- Settlement pre-cert validation: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 (bounded pre-certification validator wave adding exactly four Settlement transport validators after the AUDIT-001/R1 classification repair: Tooba.Settlement.Application/Validators/Seller/RequestSellerPayoutCommandValidator (namespace Tooba.Settlement.Application.Validators.Seller) validates only the untrusted body — Amount > 0 and IdempotencyKey non-blank — while SellerPartyId/ActorUserId come from ISettlementSellerAuthorizer and are deliberately not validated; Tooba.Settlement.Application/Validators/Admin/ProcessAdminPayoutCommandValidator and RetryAdminPayoutCommandValidator (namespace Tooba.Settlement.Application.Validators.Admin) validate only PayoutRequestId != Guid.Empty and never ActorUserId (trusted admin authorizer); QueryAdminPayoutGridQueryValidator validates only Request is not null and deliberately does not duplicate AdminPayoutGridQueryPolicy (no paging normalization, no field/operator/sort/connector whitelist, no filter/advanced-filter/search semantics); primitive shape only, no business/domain rules and no invented IdempotencyKey max length; stable transport codes in SettlementValidationCodes (settlement.validation.*) kept separate from business SettlementErrorCodes; the six NO_VALIDATOR_REQUIRED requests stay unvalidated by design (4 seller AUTH_SCOPED_QUERY: GetSellerSettlementBalanceQuery, ListSellerSettlementEntriesQuery, ListSellerSettlementStatementsQuery, ListSellerPayoutRequestsQuery; 2 admin NO_INPUT: ListAdminSettlementBalancesQuery, ListAdminPayoutQueueQuery) with no ceremonial empty validators; discovery uses only the existing AddToobaCqrsFoundation/AddValidatorsFromAssembly with MediatR still 12.5.0 and no manual validator invocation in endpoints/handlers; a new exhaustive SettlementValidatorCoverageGuardTests proves the exact 10 endpoint-reachable inventory (4 required present and resolvable to their exact concrete validator via foundation DI, 6 no-validator-required with none registered), all 10 real IRequest types, ISender-based endpoints, no IValidator/ValidateAsync anywhere in endpoints or the relevant handlers, the exact Seller/Admin validator folder layout, MediatR 12.5.0 and Settlement still NOT structure-certified; focused in-memory SettlementValidatorTests cover reject/accept/trusted-value/grid-policy-non-duplication; Settlement -> Host remains ZERO and Host Settlement residue stays two thin security adapters; no schema/migration change; structure certification remains PENDING; evidence docs/evidence/TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001/settlement-precert-validation.md)
- Settlement ARCH-COMPLETE-002 structure certification: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001 (Settlement is now ARCH-COMPLETE-002 STRUCTURE_CERTIFIED under the same lock as Order/Cart/StoreContext/Offer/Payment: live physical structure satisfies APPLICATION_CAPABILITY_FOLDERS (Commands/Queries/Errors/Models/Ports/Validators, root .cs = GlobalUsings.Domain.cs + GlobalUsings.Layout.cs), ENDPOINTS_CAPABILITY_FOLDERS (Admin/Seller, root .cs = SettlementEndpointModule.cs), INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS (Adapters/Bridges/DependencyInjection/Directories/Errors/Gateways/Handlers/Messaging/Observability/Persistence/Queries, root .cs = GlobalUsings.Domain.cs + GlobalUsings.Layout.cs); PATH_NAMESPACE_ALIGNMENT is now EXACT and guarded — SettlementArchitectureGuardTests replaced the loose StartsWith namespace prefix acceptance with exact path-derived namespace equality for every Settlement production .cs file across Domain/Contracts/Application/Infrastructure/Endpoints (project-root non-GlobalUsing files use the exact project-root namespace; EF Persistence/Migrations + model snapshot exempt); explicit root allowlists and forbidden flattened root files are enforced for Application/Endpoints/Infrastructure via Settlement_root_allowlists_and_forbidden_flattened_files_are_enforced; GlobalUsings are pinned to the approved project-wide Settlement imports only (Application Domain/Layout, Infrastructure Domain/Layout) with alias assignments rejected, plus foreign-module global alias and flattened-namespace shim rejection (NO_NAMESPACE_ALIAS_WORKAROUND, no TypeForwardedTo); validator coverage preserved exactly — 10 endpoint-reachable requests, 4 VALIDATOR_REQUIRED (RequestSellerPayoutCommand, ProcessAdminPayoutCommand, RetryAdminPayoutCommand, QueryAdminPayoutGridQuery) with 4 present via AddToobaCqrsFoundation/AddValidatorsFromAssembly discovery, 6 NO_VALIDATOR_REQUIRED (4 AUTH_SCOPED_QUERY + 2 NO_INPUT) with none registered, MediatR 12.5.0 and ISender-only endpoints; Host ownership lock strengthened — Settlement_host_residue_is_exactly_two_thin_security_adapters proves the only Host Settlement security artifacts are Admin/HostSettlementAdminAuthorizer.cs + Seller/HostSettlementSellerAuthorizer.cs, no Host Settlement endpoints/grid engine/panel composer/business runtime owner, SettlementDbContext limited to the accepted Program/ToobaModuleComposition/ModuleMigrationRegistry/MarketplaceDevelopmentBootstrap allowlist, and Settlement -> Host dependency ZERO; cross-module boundaries remain Contracts-only where approved (no foreign Application/Infrastructure/Domain, no foreign DbContext); manifest certifies Settlement (structureCertified=true, lockVersion=ARCH-COMPLETE-002, per-project root allowlists + justifications + forbidden root files) and Settlement is removed from uncertifiedHttpOwningModules; certified set becomes Order, Cart, StoreContext, Offer, Payment, Settlement; Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT and frontendFrozen = true with zero frontend change; focused SettlementArchitectureGuardTests (7), SettlementValidatorCoverageGuardTests, TmarCompleteReferenceStructureGateTests and TmarDurableGuardTests pass and both focused test projects build; no full/broad suite run; next = USER_REVIEW_SETTLEMENT_ARCH_COMPLETE_002_STRUCTURE_001; evidence docs/evidence/TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001/settlement-structure-certification.md)
- Fulfillment ARCH-COMPLETE-002 bounded audit (+R1 classification repair): TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001 then TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001-R1 (AUDIT-ONLY; zero Fulfillment/Host production code change; no guard strengthened; Fulfillment stays COMPLETE_REFERENCE_PATTERN / HTTP_OWNING / MODULE_ENDPOINTS / MEDIATR_12_5 and NOT yet ARCH-COMPLETE-002 certified; exhaustive endpoint inventory = exactly 15 endpoint-reachable MediatR requests, which is also the module's full IRequest set with real IRequestHandler implementations, all dispatched through ISender from thin endpoints (authorizer + ISender + ApiResponseFactory only, no direct Application/Directory/DbContext call), zero worker/internal-only requests; corrected validator classification after the R1 repair = 10 VALIDATOR_REQUIRED (SellerMutateFulfillmentCommand untrusted shipment body + route id, CreateShippingServiceCommand untrusted write body, UpdateShippingServiceCommand untrusted route id + write body, DeactivateShippingServiceCommand untrusted route id, GetShippingServiceQuery untrusted route id, GetAdminFulfillmentQuery untrusted route id, GetSellerFulfillmentQuery untrusted route id, ExecuteAdminFulfillmentBulkCommand untrusted bulk body, QueryAdminFulfillmentWorkQueueQuery primitive grid envelope only with grid policy staying owned by AdminFulfillmentGridQueryPolicy, and ListCustomerCheckoutFulfillmentsQuery whose CheckoutId is untrusted route input — the customer authorizer checking whether the actor may view that checkout does NOT convert the route value into an authorization-derived identity, so its only transport rule is CheckoutId != Guid.Empty and ownership/existence/business access stay out of FluentValidation) with 0 present and 10 missing, plus 5 NO_VALIDATOR_REQUIRED (2 NO_INPUT: ListAdminFulfillmentsQuery, EnsureShippingCatalogSeedCommand; 1 AUTH_SCOPED_QUERY: ListSellerFulfillmentsQuery whose SellerPartyId is produced only by IFulfillmentSellerAuthorizer; 2 OPTIONAL_PRESENTATION_LOCALE: ListShippingServicesQuery, ListEnabledShippingMethodsTreeQuery which take the optional Language HTTP query parameter with no Fulfillment-owned transport-shape constraint and semantic locale resolution delegated to the existing localization path) — trusted authorizer-derived values are never a validator reason and no ceremonial validator is required for the five; Fulfillment has NO Validators folder, NO FluentValidation reference and NO validator anywhere, discovery path would be the existing AddToobaCqrsFoundation/AddValidatorsFromAssembly; physical structure is capability-grouped (Domain Aggregates/Events/ValueObjects; Contracts Errors/Events/History/Operations/Returns/Shipping; Application Commands/Errors/Models/Ports/Queries/Shipping; Infrastructure Adapters/Bridges/DependencyInjection/Directories/Errors/Gateways/Handlers/Messaging/Observability/Persistence/Queries/Shipping; Endpoints Admin/Customer/Seller/Shipping) with the only root .cs = FulfillmentEndpointModule.cs and NO GlobalUsings file anywhere in the module; exact path-derived namespace scan across all five production projects found zero mismatches (PATH_NAMESPACE = EXACT) but the existing guard proves only a loose StartsWith prefix; no module-level alias, no TypeForwardedTo, no flattened-namespace shim, while the Host-only FulfillmentReturnsGridAliases.cs holds 7 dead grid aliases with no production consumer and is currently whitelisted by Host guards (REMOVE_DEAD_RESIDUE, not repaired here); Host Fulfillment residue = exactly three thin security adapters (HostFulfillmentAdminAuthorizer, HostFulfillmentSellerAuthorizer, HostFulfillmentCustomerAuthorizer) KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER plus non-authority composition/dev references (Program.cs module assembly + 3 authorizer registrations + MapFulfillmentEndpoints(), Composition/ToobaModuleComposition.cs FulfillmentModule, Admin/ProductWorkspaceDevelopmentBootstrap.cs FulfillmentDbContext migration) — no Host Fulfillment endpoints/grid engine/panel composer/business runtime owner; Fulfillment -> Host dependency ZERO; cross-module references are Contracts-only everywhere they are foreign (Order/Party/Inventory/Payment/Localization Contracts; Endpoints = Fulfillment.Application + BuildingBlocks only, no Infrastructure/Host/DbContext) with Party/Localization not yet explicitly guarded; existing guards lack exact namespace equality, root allowlists, alias rejection, exhaustive 15-request inventory, validator coverage manifest, MediatR 12.5.0 / ISender-only invariants, an explicit Host residue manifest and Party/Localization boundary assertions; audit decision = NEEDS_PRECERT_REPAIR_THEN_STRUCTURE with repair scope = ADD_10_TRANSPORT_VALIDATORS_AND_REMOVE_DEAD_HOST_FULFILLMENT_RETURNS_GRID_ALIASES_THEN_STRUCTURE (one bounded wave of exactly 10 primitive-shape Fulfillment transport validators under Application/Validators/{Seller,Customer,Admin,Shipping} discovering via the existing CQRS foundation with no business/domain rules, no trusted-authorizer validation and no grid-policy duplication, plus deletion of the dead Host alias file with only the focused guard/allowlist repair that deletion needs, then the Fulfillment structure-certification task); Fulfillment NOT certified here and NOT removed from uncertifiedHttpOwningModules; focused FulfillmentArchitectureGuardTests + FulfillmentEndpointOwnershipTests PASS (4/4) and Tooba.Fulfillment.Tests builds with 0 errors; no broad suite run; Checkout PAUSED_AT_SAFE_W5_CHECKPOINT and frontendFrozen = true preserved; evidence docs/evidence/TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001/fulfillment-arch-complete-002-audit.md)
- Fulfillment pre-cert repair: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 (bounded pre-certification validator wave after AUDIT-001/R1: exactly ten Fulfillment transport validators added under Tooba.Fulfillment.Application/Validators/{Seller,Customer,Admin,Shipping} — Seller/SellerMutateFulfillmentCommandValidator (FulfillmentId != Guid.Empty, Permission not null command-shape invariant, supplied ShipmentId != Guid.Empty, supplied CarrierDisplayName/TrackingReference/ShippingMethodCode not whitespace-only, supplied ShipmentLines with no null items and each OrderLineId != Guid.Empty and Quantity > 0; ActorUserId/SellerPartyId/ownership/existence/mutation-kind/shipping-method/tracking-provider semantics stay with the seller authorizer and Application/Domain), Seller/GetSellerFulfillmentQueryValidator (FulfillmentId != Guid.Empty only; SellerPartyId is authorizer-derived and never policed), Customer/ListCustomerCheckoutFulfillmentsQueryValidator (CheckoutId != Guid.Empty only — untrusted route input; ownership/existence/access stay out), Admin/GetAdminFulfillmentQueryValidator (FulfillmentId != Guid.Empty only), Admin/ExecuteAdminFulfillmentBulkCommandValidator (Request not null only; action-code allowlist, empty-bulk, cross-seller, row-identity, action-compatibility and shipment-resolution rules stay Application-owned; ActorUserId never validated), Admin/QueryAdminFulfillmentWorkQueueQueryValidator (Request not null only; paging/field/operator/sort/filter/advanced/search policy stays owned by AdminFulfillmentGridQueryPolicy), Shipping/CreateShippingServiceCommandValidator (Model not null only), Shipping/UpdateShippingServiceCommandValidator (ServiceId != Guid.Empty, Model not null), Shipping/DeactivateShippingServiceCommandValidator (ServiceId != Guid.Empty), Shipping/GetShippingServiceQueryValidator (ServiceId != Guid.Empty) — with stable FulfillmentValidationCodes (fulfillment.validation.*) and reusable FulfillmentFluentRules, primitive transport/input shape only and no business/domain rules, no grid-policy duplication, no bulk or shipping semantic duplication; the same exactly five NO_VALIDATOR_REQUIRED requests stay validator-free with no ceremonial validators (2 NO_INPUT: ListAdminFulfillmentsQuery, EnsureShippingCatalogSeedCommand; 1 AUTH_SCOPED_QUERY: ListSellerFulfillmentsQuery whose SellerPartyId comes only from IFulfillmentSellerAuthorizer; 2 OPTIONAL_PRESENTATION_LOCALE: ListShippingServicesQuery, ListEnabledShippingMethodsTreeQuery); discovery uses only the existing AddToobaCqrsFoundation/AddValidatorsFromAssembly with MediatR still 12.5.0 and no manual validator invocation in endpoints or handlers; the dead Host alias residue src/backend/Host/Tooba.Host/FulfillmentReturnsGridAliases.cs (7 global aliases with zero production consumers) is deleted with no compatibility alias replacement, and only the two Host guards that explicitly allowlisted it (HostFolderStructureTests RootCsAllowlist + the added absence guard, HostCartResidualGuardTests CartNamingAllowlist) were updated; new exhaustive FulfillmentValidatorCoverageGuardTests proves the exact 15 endpoint-reachable inventory = 10 VALIDATOR_REQUIRED resolvable to their exact concrete validators via foundation DI + 5 no-validator-required with none registered, endpoint constructions match the manifest exactly, all 15 are real IRequest types, endpoints are ISender-only with no IValidator/ValidateAsync, relevant handlers never invoke validators, the exact Seller/Customer/Admin/Shipping folder layout with no Common/Helpers/Utils/Managers, MediatR 12.5.0, dead Host alias absent and Fulfillment still NOT structure-certified/in uncertifiedHttpOwningModules; focused in-memory FulfillmentValidatorBehaviorTests additionally prove the reject/accept shapes, GUID_EMPTY-only customer rule, non-policing of trusted authorizer values, non-duplication of bulk/grid/shipping business rules; Fulfillment -> Host remains ZERO and exactly three thin Host security adapters remain; Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT and frontendFrozen = true; structure certification remains PENDING; next = TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001; evidence docs/evidence/TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001/fulfillment-precert-repair.md)
- Fulfillment Host evacuation: TB-TMAR-FULFILLMENT-HOST-EVACUATION-001 (all three Fulfillment-specific Host authorizers evacuated to module-owned implementations in Tooba.Fulfillment.Endpoints: HostFulfillmentAdminAuthorizer.cs -> Admin/FulfillmentAdminAuthorizer, HostFulfillmentCustomerAuthorizer.cs -> Customer/FulfillmentCustomerAuthorizer, HostFulfillmentSellerAuthorizer.cs -> Seller/FulfillmentSellerAuthorizer, each removed only after a complete Content Disposition Map; generic platform seams added in BuildingBlocks.Security (IAdminPanelAccess, ISellerPanelAccess, IPlatformEffectiveAccessReader + PlatformPermissionGrant/PlatformAccessOwnerKind/PlatformAccessScopeKind) implemented generically in Host (HostAdminPanelAccess, HostSellerPanelAccess, HostPlatformEffectiveAccessReader) with zero Fulfillment references; guest actor authority de-duplicated to the single shared Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId constant so Fulfillment no longer touches Order.Application; Fulfillment -> Host = ZERO, Fulfillment -> AccessControl = ZERO, Fulfillment -> Order.Application = ZERO, endpoints reference only Application + BuildingBlocks + Order.Contracts + Cart.Contracts; module-owned authorizers resolve via FulfillmentEndpointModule.AddFulfillmentEndpointPresentation (Host registers only generic seams); semantic parity proven by focused mock-only tests (admin tenant/fallback/deny/unavailable, customer owner/wrong-owner/guest-secret/dev-header/guest-actor override, seller order.handle global/category/ceiling/unrelated) with validator coverage unchanged 15/10/5; structure certification remains PENDING)
- Fulfillment structure certification: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001 (Fulfillment is now ARCH-COMPLETE-002 STRUCTURE_CERTIFIED after Host evacuation: live structure satisfies APPLICATION_CAPABILITY_FOLDERS (Commands/Queries/Errors/Models/Ports/Shipping/Validators), ENDPOINTS_CAPABILITY_FOLDERS (Admin/Customer/Seller/Shipping, root = FulfillmentEndpointModule.cs), INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS (Adapters/Bridges/DependencyInjection/Directories/Errors/Gateways/Handlers/Messaging/Observability/Persistence/Queries/Shipping) and CONTRACTS/ERRORS/DOMAIN capability folders; FulfillmentArchitectureGuardTests replaced the old loose StartsWith namespace acceptance with exact path-derived namespace equality across Domain/Contracts/Application/Endpoints/Infrastructure (EF Persistence/Migrations exempt) and added explicit root allowlists plus flattened-file absence for every project (Application/Domain/Contracts/Infrastructure root = empty, Endpoints root = FulfillmentEndpointModule.cs only) and a NO_NAMESPACE_ALIAS_WORKAROUND guard rejecting TypeForwardedTo and foreign-module Application/Infrastructure/Domain aliases while accepting legitimate self-module import aliases; validator coverage stays 15 endpoint requests = 10 VALIDATOR_REQUIRED present + 5 NO_VALIDATOR_REQUIRED with MediatR 12.5.0 and ISender-only endpoints; Host Fulfillment-specific files/types remain ZERO and Fulfillment -> Host remains ZERO with AccessControl behind the neutral IPlatformEffectiveAccessReader seam and Order.Contracts-only ordering; the manifest now certifies Fulfillment and drops it from uncertifiedHttpOwningModules while structureLock.certifiedModules = Order, Cart, StoreContext, Offer, Payment, Settlement, Fulfillment; Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT and frontendFrozen = true; Host evacuation now proceeds folder-by-folder starting at AccessControl; evidence docs/evidence/TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001/fulfillment-structure-certification.md)
- Accepted SoT stamp: 552c928c9d21211698868df73f498d1ac57c0e58
- Next task: TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001
- Gate: HOST_FIRST_FOLDER_BY_FOLDER_AFTER_FULFILLMENT_STRUCTURE_CERTIFICATION
- Payment pre-cert hygiene: TB-TMAR-PAYMENT-PRECERT-HYGIENE-001 (bounded pre-certification extraction/semantic hygiene before Payment structure certification: the four dead zero-production-consumer Payment Application ports StorefrontCheckoutPaymentAccessDto + IStorefrontCheckoutPaymentAccessPort + IPaymentProofMediaPort + IPaymentUnpaidRetrySupplyPort + IPaymentAdminOrderEnrichmentPort together with the now-unused AdminPaymentOrderEnrichmentDto removed from Tooba.Payment.Application/Ports/PaymentStorefrontBoundaryPorts.cs while live ICheckoutActorPolicyPort + IPaymentGatewayCatalogPort + IPaymentWebhookSignatureVerifier stay; legacy internal Tooba.Payment.Infrastructure/Adapters/PaymentHostContractBridge.cs renamed to PaymentContractBridge.cs with the class renamed PaymentHostContractBridge -> PaymentContractBridge keeping namespace Tooba.Payment.Infrastructure.Adapters and implementing all three of IPaymentAdminGateway + IPaymentCustomerGateway + IPaymentHoldSettingsGateway, registered by PaymentModule as PaymentContractBridge with no type-forwarding/compat alias/shim; expected Payment refund faults are typed — FailClosedPaymentRefundGateway now throws ContractOperationException("payment.refund.gateway.unconfigured") and PaymentDirectory.CloseOrStartRefundForOrderCancelAsync catches ContractOperationException by ex.Code only (preserves RefundPending for admin action on that code, MarkRefundFailed(ex.Code) for other stable payment.* codes, no InvalidOperationException text classification); the Payment↔Wallet order-payment contract boundary is typed — WalletDirectory.SpendForOrderPaymentAsync used by IWalletOrderPaymentPort throws ContractOperationException(stableCode) with unchanged codes instead of InvalidOperationException(code) and WalletPaymentGateway.VerifyAsync catches ContractOperationException and returns GatewayVerification(false, null, "WALLET_SPEND_REJECTED") while unknown faults still propagate; the localized Persian presentation fallback "مشتری توبا" plus the "—" reservation-label defaults in the Payment Application admin grid composition were replaced with locale-neutral string.Empty; PaymentDirectory.cs line count unchanged at the accepted 971-line baseline (no expansion) with a guard preventing growth; guards strengthened (old bridge file/type absent, dead port types absent, no .Message Contains/StartsWith/== classification in Payment production, typed Wallet boundary, no Persian literal in the admin grid query, zero Payment -> Host dependency, structure certification still PENDING); selected from the accepted Payment audit's named pre-cert debts only — Payment NOT structure-certified here, no validators added, PaymentDirectory not split, Settlement untouched, Checkout PAUSED_AT_SAFE_W5_CHECKPOINT, frontendFrozen = true)
- Accepted SoT stamp: 22849a17e31aa3357277c3979ac351a3f3f9f8e5
- Payment pre-cert directory split: TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001 (behavior-preserving decomposition of the 971-LOC four-interface PaymentDirectory god-file before ARCH-COMPLETE-002 certification: Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs now implements ONLY IPaymentDirectory (476 physical LOC, target <700) and owns only InitiateAsync, VerifyAsync, GetAsync, GetLatestForCheckoutAsync, HasSucceededPaymentForCheckoutAsync, RegisterProofAssetAsync, SubmitManualEvidenceAsync, RetryManualAfterRejectionAsync; PaymentReconciliationDirectory.cs implements ONLY IPaymentReconciliationDirectory and owns ReconcileStalePendingAsync using PaymentDbContext stale-pending/attempt reads plus the canonical IPaymentDirectory.VerifyAsync with exact cutoff/order/batch/attempt/processed-count semantics preserved; PaymentAdminDirectory.cs implements ONLY IPaymentAdminDirectory and owns GetOperationalAsync, GetLatestOperationalForCheckoutAsync, ReconcileAsync (delegating to canonical VerifyAsync), ConfirmDepositAsync, RejectDepositAsync, RestoreDepositAsync, UnconfirmDepositAsync, CloseOrStartRefundForOrderCancelAsync, RestoreAfterOrderCancelRestoreAsync plus the admin-only operational snapshot mapping; PaymentExpiryDirectory.cs implements ONLY IPaymentExpiryDirectory and owns ExpireDueUnpaidAsync (transaction boundary + FOR UPDATE SKIP LOCKED + status filters + timeout ordering + batch semantics + returned CheckoutIds preserved) and ReopenExpiredForRetryAsync (actor access + gateway initiation + attempt creation + timeout assignment preserved); exactly two narrow internal collaborators under Directories/Shared (PaymentActorAccess + PaymentUnpaidTimeoutAssigner, namespace Tooba.Payment.Infrastructure.Directories.Shared) remove the real duplication with no Common/Helpers/Utils/Manager dumping ground; no facade implements multiple ports and no compatibility wrapper/type forwarding exists; three implementations are intentionally partially-initialized classes whose remaining injected dependencies are initialized in the constructor with no field initializer state; PaymentModule now registers the four focused implementations directly (IPaymentDirectory -> PaymentDirectory, IPaymentReconciliationDirectory -> PaymentReconciliationDirectory, IPaymentAdminDirectory -> PaymentAdminDirectory, IPaymentExpiryDirectory -> PaymentExpiryDirectory) with PaymentDirectory receiving a deferred Func<IPaymentAdminDirectory> to break the PaymentDirectory <-> PaymentAdminDirectory construction cycle, and the four cast-based (PaymentDirectory)sp.GetRequiredService<IPaymentDirectory>() registrations are removed; behavior parity preserved with no schema/migration change, no Message classification reintroduced, Payment -> Host dependency still ZERO, Host Payment residue still only HostPaymentAdminAuthorizer + HostPaymentStorefrontAuthorizer, PaymentContractBridge intact, focused Payment directory split/reconciliation/admin/refund/expiry tests plus full PaymentArchitectureGuardTests/TmarDurableGuardTests/TmarCompleteReferenceStructureGateTests green, Payment still NOT structure-certified; source-size baseline updated for the shrunk PaymentDirectory.cs; evidence docs/evidence/TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001/payment-directory-split.md)
- Accepted parent: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001 at dcb8b416b19d8f9db90e8162754723e4cfbbfb14 (Payment Host-residue bounded audit accepted)
- Accepted parent: TB-TMAR-CART-MULTICURRENCY-LINES-001-R1 at 1aa6ab8b99a689319f25e8c31f55c332bc3f3ab4 (Cart multi-currency ACCEPTED_WITH_ORDER_SINGLE_CURRENCY_COMPATIBILITY_GUARD)
- Offer ARCH-COMPLETE-002 bounded audit: TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001 (AUDIT-ONLY; no production code changed; Offer is COMPLETE_REFERENCE_PATTERN / HTTP_OWNING / MODULE_ENDPOINTS / MEDIATR_12_5 but NOT yet ARCH-COMPLETE-002 structure-certified; Host residue classified as KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER for Host/Tooba.Host/Seller/HostOfferSellerAuthorizer.cs, REMOVE_TO_OFFER_MODULE for Host/Tooba.Host/Storefront/StorefrontPrimaryOfferResolver.cs (buy-box selection policy, 3 production call sites in StorefrontComposer), RENAME_OR_REMOVE_GLOBAL_ALIAS for Host/Tooba.Host/OfferGlobalUsings.cs with exactly 9 unqualified consumer files, REMOVE_DEAD_RESIDUE for Host/Tooba.Host/.tmp-t014-test-out/; StorefrontOfferCandidate ownership settled as a Host presentation record whose 5 selection-relevant fields must become an Offer-owned selection input; 6 endpoint-reachable MediatR requests all handled and all ISender-based; 5 VALIDATOR_REQUIRED requests with zero Offer validators today; Application/Endpoints/Infrastructure root allowlists are clean but Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs declares namespace Tooba.Offer.Contracts; certification split into TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001 then TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001; evidence docs/evidence/TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001/offer-arch-complete-002-audit.md)
- Gate: USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE
- Closed by: TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001
- Order closed by: TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE

Final closure template lock (ARCH-COMPLETE-002)

Every future final-closure task must include all of the following gates before claiming COMPLETE_REFERENCE_PATTERN:
- Application organization audit (capability folders + namespace alignment).
- Endpoints organization audit where the module owns HTTP (capability mapping exactly once).
- Infrastructure organization audit (capability/integration folders).
- path↔namespace audit for Application/Endpoints/Infrastructure.
- root allowlist audit per project.
- FluentValidation coverage audit for every MediatR transport request.
- explicit NO_VALIDATOR_REQUIRED classification where a validator is not applicable.
- module entry in docs/architecture/tmar-module-structure-manifests.json + structureLock certification in tmar-current-state.json.
Standard: docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md

Purpose

This file is the durable recovery entry point for the Tooba architecture program.
If chat/session context is lost, this file plus repository architecture docs are the source of truth.

Primary Goal

Tooba must remain a strict Modular Monolith today and be continuously prepared for low-friction, incremental migration to Microservices later.

The goal is NOT a rewrite.

The goal is:

preserve working product behavior

enforce bounded-context ownership

eliminate cross-module structural leakage

move module communication behind stable Contracts/Gates/Events

keep Host as composition root (DI/middleware/global auth/session/tenant/platform endpoints) with module-owned HTTP Endpoints for business routes

use CQRS + MediatR for new application use-cases and for all HTTP use-cases of COMPLETE HTTP-owning modules (ARCH-COMPLETE-001 / ARCH-CQRS-001)

centralize time, ID, error/localization and cache abstractions

prevent giant-file and architecture debt growth

redesign cross-service consistency before physical extraction where shared ACID transactions exist

Worker Protocol

Architect: ChatGPT
Worker: Cursor only (tooba-worker-01)
Channel: tooba-main
Protocol: BRIDGE-WAKE-V1

Worker lifecycle:

User manually gives exact .task.md to Cursor.

Worker executes only that Task-ID.

Worker returns canonical Result through Bridge.

Worker STOPS.

No polling / no automatic next task / no Worker IDLE.

Task-file rule:
filename MUST equal Task-ID exactly.

Repository Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Protected user-work ancestor:
18ca10c9

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

If user work conflicts:
return RECOVERY_CONFLICT.

Durable Architecture Sources

Inside repository:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md (must be copied in by next TMAR task if not already present)

latest docs/evidence/<Task-ID>/recovery-sot.md

External reference copies:

D:\Users\User\source\repos\SarvNewVerRequirment\reference\Tooba-Architect-Bootstrap.md

D:\Users\User\source\repos\SarvNewVerRequirment\reference\TOOBA-MICROSERVICE-MIGRATION-NOTES.md

Repository docs override stale chat memory.

Product / TMAR State

Last Product Task:
TB-P10-T022-R21

Accepted TMAR:

TB-TMAR-ARCH-BASELINE

TB-TMAR-FND-001

TB-TMAR-HOST-W1-R1

TB-TMAR-HOST-W2

TB-TMAR-BOUNDARY-V1

TB-TMAR-BOUNDARY-V1-R1

TB-TMAR-CONTRACTS-W1

TB-TMAR-CONTRACTS-W2

TB-TMAR-CONTRACTS-W3

TB-TMAR-CONTRACTS-W4

TB-TMAR-CONTRACTS-W5

TB-TMAR-CONTRACTS-W6

TB-TMAR-FE-BASELINE

TB-TMAR-FE-F1

TB-TMAR-FE-ADMIN-W1

TB-TMAR-FE-ADMIN-W2

TB-TMAR-FE-ADMIN-W3

TB-TMAR-FE-ADMIN-W4

TB-TMAR-FE-ADMIN-W5

TB-TMAR-FE-ADMIN-W6

TB-TMAR-HOST-W3

TB-TMAR-HOST-W4

TB-TMAR-HOST-W5

TB-TMAR-HOST-W6

TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN

TB-TMAR-CHECKOUT-IMPL-W1

TB-TMAR-CHECKOUT-IMPL-W2

TB-TMAR-CHECKOUT-IMPL-W3

TB-TMAR-CHECKOUT-IMPL-W4

TB-TMAR-CHECKOUT-IMPL-W5

TB-TMAR-OFFER-REFERENCE-W1

TB-TMAR-OFFER-REFERENCE-W1-R1
TB-TMAR-OFFER-REFERENCE-W1-R3 — CQRS and contract-boundary repair in progress. Module-Recovery-State: IN_PROGRESS_REFERENCE_REPAIR. Checkout remains paused. Next: TB-TMAR-OFFER-REFERENCE-W1-R4.

TB-TMAR-TAX-REFERENCE-W1

TB-TMAR-PRICING-REFERENCE-W1

TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Frontend remains frozen until explicit Architect/User release.
Do not modify production frontend code.

Current Product Resume Gate:
SAFE_WITH_TMAR_PARALLEL

User choice:
Continue TMAR for now until user explicitly says to return to product feature work.

Next TMAR task:
USER_REVIEW_GOLDEN_WAVE

HISTORICAL / SUPERSEDED (the following inventory applicability snapshot is not current authority)

Historical recovery state — TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001:

COMPLETE_REFERENCE_PATTERN (HTTP-owning, module Endpoints + MediatR):
- Cart — TB-TMAR-CART-GOLDEN-001-R1 — 35198728bf17381eaaec5db1e8033478675397fb
- Settlement — TB-TMAR-SETTLEMENT-GOLDEN-001 — f450523d08f1d42a00e28f8972a509bc202516b0
- Fulfillment — TB-TMAR-FULFILLMENT-GOLDEN-001 — 37180cc4df674e1269c1c103647ab5c96aacf64a
- Returns — TB-TMAR-RETURNS-GOLDEN-001 — 9b4bdedf0d2301c5455cf9c0af7d69f3b37039b5
- Notification — TB-TMAR-NOTIFICATION-GOLDEN-001 — 5c947708af5c66a3031786ccdfc34a726ec8746e
- Support — TB-TMAR-SUPPORT-GOLDEN-001 — b2d3e6f7df85750b5b5d9c42b19f3fa996ba5d91
- Wallet — TB-TMAR-WALLET-GOLDEN-001 — f81c11e9b21c4bb5e05385b253db28fdb1c62402
- Payment — TB-TMAR-PAYMENT-GOLDEN-001-R2 — f73b04f516a915f7f182be94d7c6829c86d2de9f
- Promotion — TB-TMAR-PROMOTION-GOLDEN-001 — 431ca6d21b21fa3af0972a1dafa0c85003abe662
- Offer — TB-TMAR-OFFER-FINAL-REVERIFY-001 — 813184b90906489b5654694b60afc96c4803cd3d

COMPLETE_REFERENCE_PATTERN (internal-only, no HTTP endpoint ownership):
- Inventory — TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001 — INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES
- Offer — TB-TMAR-OFFER-FINAL-REVERIFY-001 — implementation 813184b90906489b5654694b60afc96c4803cd3d

Remaining:
- Inventory — NEEDS_APPLICABILITY_REVERIFY (may be internal-only)

Preserved:
- Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
- Tax = UNTOUCHED in this current repair wave
- Pricing = UNTOUCHED in this current repair wave
- Frontend = BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Machine-readable: docs/architecture/tmar-current-state.json
Recovery phrase: برگردیم به TMAR؛ TOOBA-TMAR-MASTER-RECOVERY.md و آخرین recovery-sot را مبنا بگیر.

HISTORICAL / SUPERSEDED (do not treat as current authoritative COMPLETE):
Premature Payment COMPLETE / next Promotion wording superseded by R1 Result contract (IN_PROGRESS_GOLDEN_R2_READY).
TB-TMAR-NEXT-MODULE-BATCH-001 Inventory/Promotion COMPLETE claims and next-task TB-TMAR-NEXT-MODULE-BATCH-002 are superseded by the golden wave above and ARCH-COMPLETE-001.

Inventory + Promotion reference batch (HISTORICAL / SUPERSEDED):
TB-TMAR-NEXT-MODULE-BATCH-001 — Host Inventory/Promotion DbContext removed; Contracts query/schema ports; IClock/IIdGenerator; seller SetInventoryAsync Result; Domain stable codes; historically claimed COMPLETE_REFERENCE_PATTERN — SUPERSEDED by ARCH-COMPLETE-001 reopen statuses. Evidence: docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/.

Tax + Pricing Result Pattern delta:
TB-TMAR-REFBATCH-TP-RESULT-001 — Pricing seller-write expected failures return Result.Failure (amount/offer/market/currency/overlap); Tax RESULT_DELTA_NOT_APPLICABLE (TaxOutcome remains canonical); Module-Recovery-State REFERENCE_RESULT_DELTA_COMPLETE; Tax/Pricing COMPLETE_REFERENCE_PATTERN. Evidence: docs/evidence/TB-TMAR-REFBATCH-TP-RESULT-001/.

Result Pattern Foundation + Offer Golden:
TB-TMAR-FND-RESULT-001-R1 — Offer COMPLETE was REOPENED for missing Result pattern; BuildingBlocks Result/Result&lt;T&gt; + ApiResponseFactory success/failure mapping; Offer seller CQRS/endpoints adopted; Pricing/Inventory seller-write gateways return Result; Module-Recovery-State RESULT_PATTERN_FOUNDATION_COMPLETE; Offer-State COMPLETE_REFERENCE_PATTERN. Evidence: docs/evidence/TB-TMAR-FND-RESULT-001-R1/.

Tax + Pricing reference revalidation:
TB-TMAR-REFBATCH-TP-001 — REFERENCE_BATCH_COMPLETE architecture cleanup; Result Golden delta closed by TB-TMAR-REFBATCH-TP-RESULT-001. Evidence: docs/evidence/TB-TMAR-REFBATCH-TP-001/.

Foundation Observability/Error Presentation R4:
TB-TMAR-FND-OBSERR-001-R4 — Final integrated verification. Module-Recovery-State FOUNDATION_COMPLETE (observability/error). Offer historically COMPLETE then REOPENED by Result gap. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R4/.

Foundation Observability/Error Presentation R3:
TB-TMAR-FND-OBSERR-001-R3 — Error localization/catalog complete; Module-Recovery-State FOUNDATION_ERROR_LOCALIZATION_COMPLETE; Offer-State READY_FOR_FINAL_REFERENCE_REVERIFY. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R3/.

Foundation Observability/Error Presentation R2:
TB-TMAR-FND-OBSERR-001-R2 — Runtime tracing complete; Module-Recovery-State FOUNDATION_RUNTIME_TRACING_COMPLETE; Offer-State REOPENED_WAITING_CENTRAL_FOUNDATION. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R2/.

Foundation Observability/Error Presentation R1:
TB-TMAR-FND-OBSERR-001-R1 — Phase 1 Core observability/error presentation; Module-Recovery-State FOUNDATION_PHASE1_COMPLETE; Offer-State REOPENED_WAITING_CENTRAL_FOUNDATION. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R1/.

Offer Reference Module W1-R1:
TB-TMAR-OFFER-REFERENCE-W1-R1 — prior Offer COMPLETE REOPENED on visual evidence; physical folders/namespaces aligned; ARCH-MODULE-PHYSICAL-001; Physical-Structure-State VERIFIED_ON_DISK; COMPLETE_REFERENCE_PATTERN revalidated. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/.
TB-TMAR-OFFER-REFERENCE-W1-R2 — Offer Golden residual repair: SemanticException codes, IIdGenerator/IClock determinism, full residual scan CLEAN; COMPLETE_REFERENCE_PATTERN. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/.
TB-TMAR-OFFER-REFERENCE-W1-R3 — prior COMPLETE claim revoked; CQRS/contract-boundary repair implemented. Module-Recovery-State: IN_PROGRESS_REFERENCE_REPAIR. Next: TB-TMAR-OFFER-REFERENCE-W1-R4. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/.

Pricing Reference Module W1:
TB-TMAR-PRICING-REFERENCE-W1 — historically shipped COMPLETE_REFERENCE_PATTERN, then revalidated by TB-TMAR-REFBATCH-TP-001. The stale "wait for USER_REVIEW_OFFER" gate is removed. Evidence: docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/ and docs/evidence/TB-TMAR-REFBATCH-TP-001/.

Tax Reference Module W1:
TB-TMAR-TAX-REFERENCE-W1 — COMPLETE_REFERENCE_PATTERN revalidated by TB-TMAR-REFBATCH-TP-001. Evidence: docs/evidence/TB-TMAR-TAX-REFERENCE-W1/ and docs/evidence/TB-TMAR-REFBATCH-TP-001/.

Offer Reference Repair R4:
TB-TMAR-OFFER-REFERENCE-W1-R4 — fake CQRS and Host Offer BFF removed; owner contract gates established; Module-Recovery-State READY_FOR_FINAL_VERIFICATION; next TB-TMAR-OFFER-REFERENCE-W1-R5. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/.

Offer Reference Final Verification R5:
TB-TMAR-OFFER-REFERENCE-W1-R5 — magic offer.not_found exception seam repaired (SemanticException); seller Offer CQRS/guards green; Host Admin/Storefront OfferDbContext residual blocks COMPLETE. Module-Recovery-State: INCOMPLETE. Next: TB-TMAR-OFFER-REFERENCE-W1-R6. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/.

Offer Reference Host Persistence Closure R6:
TB-TMAR-OFFER-REFERENCE-W1-R6 — Host/foreign production OfferDbContext leaks removed via IOfferQueryGateway + Offer.Infrastructure adapter; composers/grids/storefront/merch/reservation/seeds rewired; architecture guards green. Historically COMPLETE_REFERENCE_PATTERN then REOPENED for cross-cutting observability/error presentation gap. Offer-State: REOPENED_WAITING_CENTRAL_FOUNDATION. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/.

Foundation Observability/Error Presentation R3:
TB-TMAR-FND-OBSERR-001-R3 — ErrorDescriptor catalog + SafeErrorMapper without naming heuristics; Offer .resx localization; IExceptionPresentationService; Offer seller endpoints bubble to global pipeline; Module-Recovery-State FOUNDATION_ERROR_LOCALIZATION_COMPLETE; next TB-TMAR-FND-OBSERR-001-R4. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R3/.

Foundation Observability/Error Presentation R2:
TB-TMAR-FND-OBSERR-001-R2 — Correlation middleware + request log enrichment + TracingBehavior + IModuleCallTracer + Offer gateway topology + MassTransit/outbox correlation; Module-Recovery-State FOUNDATION_RUNTIME_TRACING_COMPLETE; next TB-TMAR-FND-OBSERR-001-R3. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R2/.

Foundation Observability/Error Presentation R1:
TB-TMAR-FND-OBSERR-001-R1 — Correlation + SafeErrorMapper + ApiResponseFactory + locale resolver + ProblemDetails context; Host wired; Offer endpoints use central path; Module-Recovery-State FOUNDATION_PHASE1_COMPLETE; next TB-TMAR-FND-OBSERR-001-R2. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R1/.

Checkout Implementation W5:
TB-TMAR-CHECKOUT-IMPL-W5 — Promotion checkout seam (ICheckoutPromotionPort); Order.Application↛Promotion.Application; TX preserved; W6 READY (intentionally paused); FE freeze intact. Evidence: docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/.

Checkout Implementation W4:
TB-TMAR-CHECKOUT-IMPL-W4 — Order.Infrastructure Cancel/Restore/PaymentBridge behind Inventory.Contracts (IOrderInventoryLifecyclePort); Infra→Inventory.Application removed; TX preserved; W5 READY; FE freeze intact. Evidence: docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/.

Host Structure W1:
TB-TMAR-HOST-STRUCTURE-W1 — Host root responsibility folders + HOST-FOLDER-001/HOST-HYGIENE-001; READY_TO_PAUSE; Checkout-W4 resume READY. Evidence: docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/.

Checkout Implementation W3:
TB-TMAR-CHECKOUT-IMPL-W3 — Cart.Contracts conversion seam (ICartConversionPort); Order.Application↛Cart.Application; TX preserved; W4 READY; Orders FE STILL_WAITING_FOR_BACKEND_W4. Evidence: docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/.

Checkout Implementation W2:
TB-TMAR-CHECKOUT-IMPL-W2 — in-process Process Manager + Inventory.Contracts reservation seam; TX preserved; Order.Application↛Inventory.Application; W3 READY; Orders FE STILL_WAITING_FOR_BACKEND_W3. Evidence: `docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/`.

Checkout Implementation W1:
TB-TMAR-CHECKOUT-IMPL-W1 — durable Order-owned checkout process state + submission idempotency; TransactionScope preserved; no Saga runtime; W2 READY; Orders FE STILL_WAITING_FOR_BACKEND_W2; Architecture-Priority CHECKOUT_IMPLEMENTATION. Evidence: `docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/`.

Checkout Consistency Design:
TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN — design+locks only; Order-owned Process Manager; Checkout-Point-Of-No-Return MULTI_STAGE; READY_FOR_IMPLEMENTATION_W1; Architecture-Priority CHECKOUT_IMPLEMENTATION. Evidence: `docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/`.

Host W6:
TB-TMAR-HOST-W6 — ShippingService Create/Update/Deactivate/EnsureSeed → Fulfillment Application/Directory; Host-Exit-State READY_TO_PIVOT; Architecture-Priority CHECKOUT_DESIGN; Checkout design READY. Evidence: `docs/evidence/TB-TMAR-HOST-W6/`.

Host W5:
TB-TMAR-HOST-W5 — UnitOfMeasure Host writes → Catalog Application/Directory; Host-Exit-State NOT_READY; CONTINUE_HOST. Evidence: `docs/evidence/TB-TMAR-HOST-W5/`.

Host W4:
TB-TMAR-HOST-W4 — QuantitySettings Host write → Catalog Application/Directory; Host-write baseline shrink; CONTINUE_HOST. Evidence: `docs/evidence/TB-TMAR-HOST-W4/`.

Host W3:
TB-TMAR-HOST-W3 — StoreAppearanceSettings Host write → Catalog Application/Directory; Host-write baseline shrink; CONTINUE_HOST. Evidence: `docs/evidence/TB-TMAR-HOST-W3/`.

Frontend ADMIN-W6:
TB-TMAR-FE-ADMIN-W6 — admin-dashboard migrated; admin-api 1022→1002; exports 38→35; admin-screens 894→810; Flat exit READY_TO_PIVOT; Architecture-Priority HOST. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W6/`.

Frontend ADMIN-W5:
TB-TMAR-FE-ADMIN-W5 — admin-receipts migrated; admin-api 1069→1022; admin-screens 1036→894; Flat exit NOT_READY. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W5/`.

Frontend ADMIN-W4:
TB-TMAR-FE-ADMIN-W4 — admin-customers migrated; admin-api 1107→1069; admin-screens 1057→1036. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W4/`.

Frontend ADMIN-W3:
TB-TMAR-FE-ADMIN-W3 — admin-sellers migrated; admin-api 1145→1107; admin-screens 1078→1057. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W3/`.

Frontend ADMIN-W2:
TB-TMAR-FE-ADMIN-W2 — admin-reviews migrated; admin-api 1234→1145; admin-screens 1120→1078; pattern PROVEN. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W2/`.

Frontend ADMIN-W1:
TB-TMAR-FE-ADMIN-W1 — admin-promotions migrated; admin-api 1321→1234; admin-screens 1230→1120. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W1/`.

Confirmed Architecture Facts

Foundation:

MediatR 12.5.0

FluentValidation pipeline

IClock

IIdGenerator / UUIDv7 abstraction

SemanticError foundation

architecture freeze guards

Confirmed cross-module debt:

Cart.Domain → Offer.Domain: removed in CONTRACTS-W1

Order.Domain → Offer.Domain: removed in CONTRACTS-W1

Pricing.Domain → Offer.Domain: removed in CONTRACTS-W1

Payment.Infrastructure → Wallet.Domain: removed in CONTRACTS-W1

Payment.Infrastructure → Wallet.Application: removed in CONTRACTS-W2 (IWalletOrderPaymentPort)

Returns.Infrastructure → Wallet.Application: removed in CONTRACTS-W3 (IWalletRefundCreditPort)

Order.Application → Offer.Application: removed in CONTRACTS-W3 (SalesChannel via Offer.Contracts)

Cart.Application → Offer.Application: removed in CONTRACTS-W4 (SalesChannel via Offer.Contracts)

Order.Application → Tax.Application: removed in CONTRACTS-W4 (ITaxCalculator via Tax.Contracts)

Inventory.Application → Offer.Application: removed in CONTRACTS-W5 (Offer.Contracts)

Order.Application → Pricing.Application: removed in CONTRACTS-W5 (IPriceLookupGateway via Pricing.Contracts)

Promotion.Application → Offer.Application: removed in CONTRACTS-W6 (Offer.Contracts)

Cart.Application → Pricing.Application: removed in CONTRACTS-W6 (Pricing.Contracts + campaign authority)

Order.Application is a synchronous hub with remaining foreign Application dependencies

legacy App→App edges exist and are frozen against expansion

legacy Infra→foreign Application edges exist and are frozen against expansion

Host:

StoreLandingPage Host direct writes removed

StoreMenu Host direct writes removed

many legacy Host write sites still remain

new Host business writes/decisions are frozen

Domain:

repository-wide Domain ownership audit exists

some types are likely in wrong bounded contexts

Catalog currently contains transitional StoreAppearance / Landing / Menu / other ownership debt

no Big Bang Domain move

God files:

repository-wide source-size inventory exists

55 oversized legacy files baselined

new handwritten files >800 LOC are rejected

oversized legacy files may not grow above baseline

critical decomposition requires characterization tests first

Contracts Target

Cross-module public boundaries converge toward:
Tooba.<Module>.Contracts

Foreign Application / Domain / Infrastructure must not become public module APIs.

No 31-project Big Bang extraction.
Contracts are extracted in waves by verified coupling and extraction value.

CQRS Target

New use cases:
HTTP Endpoint
→ ISender
→ Command/Query
→ Handler in Application
→ Domain + abstractions
→ Infrastructure implementation

Business MediatR handlers must not live in Infrastructure.

Host Target

SUPERSEDES_OLD_HOST_THIN_TRANSPORT:
Older wording that Host may own generic HTTP transport/endpoint implementation for module business routes is superseded. Module-owned `Tooba.*.Endpoints` is mandatory for HTTP-owning COMPLETE modules (`HOST-MODULE-ENDPOINT-001`, `ARCH-COMPLETE-001`).

Host owns:

process startup / DI / composition root

middleware

global authentication/session/tenant/correlation

platform-level endpoints such as health/readiness

tiny security adapters for module Endpoints

explicit Development bootstrap allowlists

Module owns:

module HTTP route implementation

wire DTOs specific to that module

module success/failure presentation composition

business use-case dispatch via ISender → Application MediatR handlers

Host must not gain:

module-owned business route implementation

direct Directory calls from Host HTTP for module business use-cases

module-specific business response mapping/composers/query engines as durable ownership

business writes / SaveChanges/transactions as module business truth

domain ownership

Domain Ownership Rule

A Domain type belongs to the bounded context that owns its invariant and lifecycle.
Never place a type in a module merely because persistence was convenient.

Examples such as StoreAppearance*, StoreLandingPage*, StoreMenu*, StoreCheckout* are examples only.
Ownership rules apply repository-wide.

Errors / Locale

No hardcoded user-facing localized Domain messages in ANY language.
Domain/Application errors are semantic and stable.
HTTP boundary maps them to localized ProblemDetails.

Locale architecture must support unlimited locales.
Do not hard-code architecture around only FA/EN.

Cache

Canonical:

ICache

ICacheKeyBuilder

ICacheInvalidator

No new direct IMemoryCache bypass.
Redis remains a future provider.

Microservice Migration Critical Note

A full rewrite is NOT required.

Many peripheral modules are relatively extractable after contracts and operational readiness.

The hard area is the Checkout / Cart / Order / Inventory / Payment consistency chain.

Current shared-database TransactionScope patterns cannot be assumed to work after physical service/database separation.

Future extraction requires explicit consistency design:

Saga / Process Manager where appropriate

compensating actions

idempotency

retry semantics

duplicate delivery handling

timeouts/recovery

intermediate states

point-of-no-return

Outbox / Integration Events

DO NOT implement a Saga prematurely.

Canonical rule to add:
No NEW business workflow may depend on one ACID transaction spanning multiple bounded contexts.

Existing cross-context transactions must be inventoried and treated as extraction debt.

Feature Resume Rule

TMAR already reached:
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

Therefore product work MAY resume when the user says so.

Until the user says to return to product work:
continue TMAR tasks sequentially.

When product work resumes:

TMAR continues in parallel

all new product code must obey current TMAR locks

no new legacy debt patterns

Recovery Phrase

If chat is lost, user can say:

برگردیم به TMAR — فایل TOOBA-TMAR-MASTER-RECOVERY.md و آخرین recovery-sot را مبنا بگیر

Then:

read this file

read repository bootstrap/locks/capability map/migration notes

read latest recovery-sot

identify last accepted Task-ID

continue from next task without reconstructing from guesses

## TMAR HOST EVACUATION — CANONICAL RECOVERY OVERRIDE

Marker: `TMAR-HOST-EVACUATION-V1`

Canonical protocol:
`docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`

Current method:
- Finish active Fulfillment Host evacuation before Fulfillment structure certification.
- Then traverse `src/backend/Host/Tooba.Host` folder-by-folder/file-by-file in repository order.
- Read every Host production file completely and create a member-level Content Disposition Map.
- A file may split across multiple module owners.
- Never delete a Host production file before every live responsibility is rehomed and parity is proven.
- If a destination module lacks proper Endpoints/CQRS/MediatR/validation/foldering/contracts boundaries, repair that destination before completing the Host-file evacuation.
- Tests/guards are focused proof after ownership repair; they are not the navigation strategy.
- Final target: Host = thin platform/composition shell only.

Recent accepted recovery facts:
- Settlement ARCH-COMPLETE-002 STRUCTURE_CERTIFIED at `54b1c8ff1f6e9214a5b5c16b6103f0285bd2a37e`; SoT stamp `01d15f3cb1ad38f0e91ed65e990e32c4f9d19876`.
- Fulfillment audit R1 corrected inventory to 15 endpoint requests = 10 validator-required + 5 no-validator-required.
- Fulfillment pre-cert repair accepted at `16062d45bde71476da35f9e20622f1b6b5637fa8`; ten transport validators are present.
- `FulfillmentReturnsGridAliases.cs` was syntax-only alias residue with zero production consumers; its underlying canonical types were not deleted.
- Fulfillment Host evacuation COMPLETE at task `TB-TMAR-FULFILLMENT-HOST-EVACUATION-001`: all three Host-specific authorizers (`Admin/HostFulfillmentAdminAuthorizer.cs`, `Customer/HostFulfillmentCustomerAuthorizer.cs`, `Seller/HostFulfillmentSellerAuthorizer.cs`) were rehomed to module-owned implementations in `Tooba.Fulfillment.Endpoints` after a full Content Disposition Map, with generic Host seams `HostAdminPanelAccess`/`HostSellerPanelAccess`/`HostPlatformEffectiveAccessReader` and neutral BuildingBlocks contracts; `HOST_FULFILLMENT_SPECIFIC_FILES = ZERO`; evidence `docs/evidence/TB-TMAR-FULFILLMENT-HOST-EVACUATION-001/fulfillment-host-evacuation.md`.
- Fulfillment structure certification is now the intended next task: `TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001`.

After Fulfillment, do NOT automatically continue by uncertified-module list. Start Host traversal with AccessControl, then AddressBook, then subsequent Host folders in repository order.

## TMAR Host Evacuation — Current Live State (AccessControl)

Latest accepted task: `TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001`
Latest accepted commit / SoT stamp: `4a6074e62fbaf557f57aa2770d76b8d14164dc72`
Current track: `HOST_FIRST_FOLDER_BY_FOLDER` — active Host folder = `AccessControl`
Latest accepted parent: `TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001` at `99d59d894a4464b57d1f53a6df9800843a76a592`

### Accepted AccessControl migration summary (module-owned)

- Permission catalog module-owned.
- Admin platform role reads/writes/clone/archive/permissions module-owned.
- AdminSeller complete role + permissions family module-owned.
- Seller complete role + permissions family module-owned.
- Seller ceiling + assignments module-owned (`TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001`, `46c3777c`).
- AdminSeller ceiling + assignments + effective module-owned (`TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001`, `b9dda0d7`).
- Admin platform assignments + effective module-owned (`TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001`, `99d59d89`).
- Admin + Seller user search and Seller effective module-owned (`TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001`, `4a6074e6`).

### Contracts-only user-search boundary (no foreign Application/Domain leak)

- `Tooba.Identity.Contracts/IActorContactLookup` (batch contact projection).
- `Tooba.Identity.Contracts/IActorIdentifierResolver` (new; neutral email/phone/username -> user id; implemented by Identity as `ActorIdentifierResolverAdapter`, registered in Identity-owned DI).
- `Tooba.OperatorProfile.Contracts/IActorDisplayLookup` (batch display projection).
- `AccessControl.Application` references Identity.Contracts + OperatorProfile.Contracts only — ZERO `Identity.Application`/`Identity.Domain`/`OperatorProfile.Application`/`OperatorProfile.Domain`; Host `AccessControlEndpoints.cs` no longer imports the foreign Application/Domain namespaces.

### Current residual Host AccessControl routes/files

`src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs` still owns:

- Admin scope-resources: categories, brands, products, warehouses (deferred), stores (deferred), order-segments (deferred).
- Seller scope-resources: categories, brands, products, warehouses (deferred), stores (deferred), order-segments (deferred).
- Admin demo-preview.
- Residual shared helpers only as actually still used (`RequireSellerAsync`, `Trace`, `MapError`).

Separate Host folder files still present:

- `AccessControlEndpoints.cs`
- `AccessControlDevelopmentSeed.cs`
- `AccessControlDemoSnapshot.cs`

`Program.cs` still has legacy AccessControl Host mapping/bootstrap residue until final cleanup.

### Known test debt (not a production regression)

`Tooba.Host.Tests/AccessControlFoundationTests.AccessControl_module_boundary_static_checks` is stale: it still expects old Host route/group text (`/v1/admin/sellers/{sellerId:guid}/access-control`, `/me/capabilities`). Those routes were correctly evacuated in previously accepted tasks and the assertion would already have failed at accepted parent `99d59d89`. Record as TEST-MAINTENANCE DEBT; repair only if explicitly scoped and tiny, otherwise a separate focused test-maintenance task.

### Next implementation task

`TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001` — evacuate Admin + Seller scope-resources family from Host with a proper Catalog Contracts/shared-neutral seam rather than moving `ICatalogLookupGateway` from Catalog.Application into AccessControl. The stale foundation-test assertion may be repaired in that task only if explicitly scoped and tiny.

### AccessControl honest state

AccessControl remains `IN_PROGRESS`; NOT `COMPLETE_REFERENCE_PATTERN`; NOT ARCH-COMPLETE-002 STRUCTURE_CERTIFIED; Host residue is NON-ZERO. It is NOT added to the certified-module list.

### Global locks preserved

- Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`; frontend `FROZEN`.
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`.
- Structure-certified modules remain: Order, Cart, StoreContext, Offer, Payment, Settlement, Fulfillment.

