PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212
Parent-Task: TB-SHIPPING-ADMIN-AREA-CONTRAST-AND-EDITOR-CHECKS-211
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 51f65febfc15bdf1c9fdbaade5215def2ca88a3e
Mode: BOUNDED_SOURCE_ONLY_EXISTING_RUNTIME_BRIDGE_CONTRACT
Timebox:12min, stop new analysis8, final4 evidence/checkpoint/scopedcommit/push.
No test/build/browser/runtime/DB/probe. No correction loop or generic new audit.
Purpose: NEXT real implementation must connect new draft editor to existing Storefront shipping authority
without parallel quote engine, silent live activation or module ownership violation.
User requests quick complete usable shipping. This is one short source decision, not another QA round.

READ
TOP AGENTS +canonical recovery +211/ARCHITECT-REVIEW.md and relevant existing architecture skill/decisions.
190–211 accepted; no replay. Explicit local nonproduction authorization persists, no new approval question.

KNOWN ROOT-VERIFIED FACTS (don't rediscover broadly)
Order.Application/Storefront/Services/StorefrontShippingService.cs:
LoadEligibleMethodsAsync constructs legacy candidates via registry/options/catalog THEN invokes
StorefrontShippingOwnedRateResolver.ResolveAsync when composed. If evaluated, authoritative quotes
replace legacy prices; canonical-only methods included with active gate. ApplyOneLevelDisplayNames
is display-only; BuildGroupHorizons currently uses seller prep+methodLeadDays and static day windows.
Order.Application/Storefront/Shipping/OwnedRate/StorefrontShippingOwnedRateResolver.cs:
Consumes Shipping.Contracts.Rates.IShippingRateQuoteLookup + Geography.Contracts. GateOFF zero calls.
StrictOwnedOption; Parcel=null because physical measurements have no trustworthy source; currency check,
missing rate fail-closed. Source signatures already exist; don't build a second calculator.
211's "runtime config-only" is incomplete; "read-only activation/publish" contradictory. Don't choose
Fulfillment.Contracts to own Shipping draft publication. No new Activated flag approved by prose.
TB185 paused/defaultOFF/PendingAllocation remains; don't enable/repurpose historical gate.

EXACT ANALYSIS OUTPUT (one concise doc/RESULT, no large proposal set)
1 Follow actual IShippingRateQuoteLookup implementation + Shipping rate/schedule read/write ownership.
Identify authoritative binding owner(Store/Marketplace/Seller), method service/option, market/currency,
destination and measurement inputs. Show exact members, not zero-reference-search absence claims.
Follow current candidate identity and where new draft presentation/type can be read without rehoming authority.
2 Compare authored group/rate/week/settings against those existing runtime structures.
What can existing algorithm represent exactly; what cannot (weight/count/amount, freight+packaging,
independent payment/calculation setting, weekly hours/timezone, type/language)? No invented fallback/default.
Check selection/commit revalidation parity with projection (not UI-only price).
3 From approved decision docs, resolve whether SAVE is authoring-only (current) and what explicit
publish/activation boundary already exists/is approved. Distinguish mutation from read.
Choose ONE concrete minimal bridge design reusing Shipping-owned authority +Contracts-only edge.
Publishing immutable/current-revision snapshot vs directdraft reads must be an explicit justified decision;
don't imply unsaved/unpublished edits should become customer-visible. No new broad workflow framework.
If an essential product policy is genuinely undecided, name precise question and safest bounded next
implementation that doesn't depend on guessing; don't invent activation/overlap/calendar/price policy.
4 Provide NEXT executable slice: exact files/contracts/transaction boundary, owner+revision checks,
projection/selection/commit consumers and focused tests. State source-backed acceptance and remaining
slices (backend connection, customer UI reuse, real local end-to-end). No time guarantee for whole delivery.
Use existing storefront ShopeivaDOM/CSS/cards unchanged. No generic discovery task after this one.
Output compact table+one plan; ideally <=100 lines beyond source references.

BOUNDS
Docs/evidence ONLY, no src/package/config/flags/schema/migration/data/runtime/browser changes.
No build/test/heavygit sweep/Bridge probe/post-result script execution.
No customer mutation/no dev seed. Don't "fix" unrelated old fallback behavior in this analysis.
Don't certify Storefront functional until actual connection and local e2e proven.
Append211 finding correction with existing gated quote path; preserve history.

DELIVERY
Persist task verbatim plus docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-STOREFRONT-BRIDGE-CONTRACT-212/RESULT.md
Update TOP recovery+AGENTS concise exact next implementation; scopecommit/push/fetch parity, real timing.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. Do not emit executable envelopes yourself.
