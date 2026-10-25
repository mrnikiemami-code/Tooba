PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1
Parent-Task: TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 3296121d7e34d4f692dcfd0f86c794b269e66a1d
Mode: BOUNDED_EXACT_RUNTIME_SOURCE_CORRECTION_AND_LOSSLESS_BRIDGE_DECISION
Timebox:10min; stop newanalysis6; final4 evidence/commit/push. NO build/test/runtime/browser.
Read TOP AGENTS/recovery +212/ARCHITECT-REVIEW.md first.
Proposed213 NOT authorized. This is targeted correction, NOT another generic audit. User wants speed.

ROOT-VERIFIED CONTRADICTIONS — DON'T SEARCH FOR WHETHER THEY EXIST
Shipping.Infrastructure/DependencyInjection/ShippingModule.cs AddServices registers:
IShippingPackagingFeeLookup->ShippingPackagingFeeLookup,
IShippingShipmentCostLookup->ShippingShipmentCostLookup,
IShippingOperationalQuoteLookup->ShippingOperationalQuoteLookup.
Infrastructure/Operations/ShippingOperationalQuoteLookup.cs loads own four sets, delegates
Domain.Operations.ShippingOperationalQuoteEvaluator.Evaluate. Implementations EXIST.
Read these actual adapters/evaluator and relevant focused tests. Retract "contracts only/unimplemented".
Operational Contracts explicitly document carrierpickup/dispatch window, NOT delivery promise.
Tenant isolation via ShippingDbContext configured from ICurrentCommerceContext+database resolver exists.
Do not equate applicability(service/option/geography/currency) with actor/seller authorization.

DELIVER ONE CORRECTED PLAN, <=80 lines + references
1 Exact existing component reuse map, including registrations and the already-existing evaluator/cost split.
2 Prove semantic compatibility or reject conversion: authored per-row packaging vs scope-only packaging
resolver; merchandise bounds; delivery min/max vs prep/lead; CalculationMode Free/Manual/CarrierApi;
editor week authority vs pickup window; ownerStore/Marketplace/Seller. Read approved rule decision text,
not only similar CLR fields. Missing policy must be explicitly named.
3 No lossy publication: unsupported field => reject ENTIRE operation before mutation, not activate subset
and return warnings. Existing flags don't give draftrevision provenance or owned rollback.
Onepolicy/currency is invalid when rows/groups have different packaging fees. Don't create duplicate
quoteauthority, flatten seller scope, deactivate unrelated runtime rows, or claim nested command handlers
are automatically one unitofwork.
4 Choose ONE minimal next executable slice based on ACTUAL source:
either a genuinely lossless conversion with proven provenance+one atomic transaction,
or Shipping-owned published revision snapshot read through Contracts with full owner identity/row
conditions/components and EXTENSION of existing Shipping authority. Direct published read != Order
reading drafttables. No automatic choice merely because worker212 proposedcopy.
Do not invent unread approved activation semantics. If essential policy is undecided state exact blocker
and choose safe necessary code step independent of it. Avoid another generic sourceaudit next.
Next slice must identify exact files, transaction/concurrency/owner checks, expected minimal focusedtests
and precise user-visible progress/remaining work. No executable task envelope from worker.

BOUNDS
Docs/evidence only. No src/config/flag/DB/migration/build/test/browser/probe, no Bridge script execution.
No gateenable/TB185resume, no defaultOFF/PendingAllocation change, no new seed/draft, no sharedrestart.
Standing localdev authorization persists, but this task is sourceonly and nothing needs permission.
No replay190–211. Preserve actual user data/revision/unsaved tabs, Host5088/FE3000.

RESULT
Persist task verbatim and docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212-R1/RESULT.md
Append212 correction without rewriting history. Update TOP recovery+AGENTS (don't inject controlchars
from shell string escapes; use literal file text). Exacttiming, scopedcommit/push/fetchparity.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. Don't implement213 or make storefront completion claims.
