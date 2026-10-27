PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-FULL-OWNER-FLOW-AND-PREVIEW-CONTRACT-235
Parent-Task: TB-SHIPPING-CAPTURED-PUBLIC-COMMIT-EDGE-234
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: IMPLEMENT_REQUIRED_OWNER_PATHS_ON_EXISTING_VERTICAL_FLOW
HARD45 / source28 / final17. One correction. Actual hardstop, no silent extension. No Bridge/enqueue/wake/post-result script.
Read234/ARCHITECT-REVIEW.md + SHIPPING-COMPLETE-OWNER-AND-COUPON-DELIVERY-SCOPE-2026-10-27.md + applicable instructions.
This is IMPLEMENTATION, not another survey/new calculator. No236envelope.

USER REQUIREMENT — BOTH EDITIONS AND EACH REAL BUSINESS OWNER
SingleStore platform fulfillment => active exact store tenant (Store owner).
Marketplace platform fulfillment => Marketplace owner, NO tenant/seller key.
Marketplace seller fulfillment => Seller owner keyed by actual Offer.SellerPartyId (BUSINESS identity, not login user).
Marketplace mixed cart => each real responsibility/type/seller partition keeps its OWN method/capture/row/date/money.
No blanket Marketplace/Seller refusal on required supported paths. SingleStore must not reinterpret an external Seller
assignment as its Store. Unknown responsibility/owner/edition/inactive/residual tenant fails before quote/write.
Existing source: ShippingCapturedOwnerScopeResolver ALREADY accepts all3coherent owner shapes; current planner
BuildPartition already has these branches, but writer and final acceptance hardcode Store. REMOVE that artificial
limitation by sharing one small typed Order-owned owner-resolution policy across existing planner/writer/acceptance,
not by duplicating logic. Derive from trusted edition/tenant + verified228assignment/seller, NEVER browser owner key.
Domain ShippingGroupOwner already supports all3forms; no new schema/owner table/user-id ownership/backfill.

FIRST bounded source check: record actual SellerPartyId/store-business relationship and existing seller-authorizer
scope used by this repo (StoreCommerceContext is commerce metadata, not a new StoreId registry). Do not invent or
silently equate a separate StoreId with SellerPartyId. Reuse actual seller/business authority; if cardinality cannot
express independently owned stores, report the exact model blocker rather than claim unsupported isolation.
This is not permission to build a new store framework or generic audit.

A IMPLEMENT THE COMPLETE OWNER PATH
- Existing quote/selected lookup must get exact full owner and binding. Selection builds Store/Marketplace/Seller
  evidence from the server candidate; Seller evidence must equal that group's actual selling party.
- Final submit rederives each group's owner from actual current assignments. Compare stored owner EXACTLY, then read
  current pointer/capture/quote for THAT owner. No global Store-only tenant resolution, no rewriting marketplace
  ownership as each seller, no trusting evidence owner as its own validator.
- One batch assignments/type/catalog, one219 per DISTINCT FULL owner+binding, one216 per selected group; same amount/
  calendar/evidence/atomicity/idempotency machinery. Buyer can quote permitted cart methods from different sellers
  without being that merchant; merchant Manage permission is a separate boundary, not buyer quote authority.
- Close the known cross-owner label map NOW: methodCode alone cannot key labels across Marketplace/Seller captures
  sharing one service/option. Scope by typed full owner+binding/candidate obligation; each group keeps its OWN DB
  translations. Same label on different bindings still lawful. No label parsing/new names or client scope picker.
- Existing seller/admin authorizers remain FIRST on management operations. Prove own seller Manage vs View-only and
  foreign seller draft denial through existing route/delegate harness, Marketplace own platform and SingleStore
  own tenant. No new permission catalogue/bypass or invented global Admin delegation. Don't claim an authorized
  owner-set admin feature unless actually implemented/proven.
- Runtime/checked-in gates remain unchangedOFF. Coupon still explicitly unsupported in THIS slice only; full shared
  coupon context remains a required NEXT delivery slice, not a final exclusion. No price/slot engine change.

B CLOSE234 RESPONSE ISSUES IN SAME VERTICAL FLOW (source-confirmed)
1 Active captured payment preview currently emits CartVersion0 and generic product titles because facade maps
   StubCartPage(empty). Reuse the real authorized presentation cart ALREADY read by ShippingService (do not discard
   it); preserve actual version/titles/seller labels with one quote, no fictitious cart. No external metadata read
   AFTER durable commit; persisted responses remain from Order facts.
2 Preview core may validate a NEW root while command retains OLD aggregate shipping/date. Return a small typed
   acceptance receipt (groups + actual cart version + actual selection revision) from shared core as needed; wrapper
   final host can still return groups. Compose read snapshot from ACCEPTED groups/payable amounts/selected dates,
   not stale command. Return current root revision as exact TEXT in additive preview response, needed by frontend.
   No invented aggregate date across independent groups, no duplicated math policy.
3 Persisted groups: OrderLineIds are NOT CartLineIds. Add optional explicitly named OrderLineIds in response;
   CartLineIds null/absent for historical groups unless genuine mapping exists. Draft/preview keep real CartLineIds.
4 Never fill missing historical Snapshot.ShippingGroups from CURRENT stored draft plans. Missing remains absent;
   original Order shipping/price is authoritative. No historical repricing/post-commit lookup/throw for absent group.
These are necessary frontend contract repairs, not a new history endpoint or UI redesign.

FOCUSED PROOF — REAL EXISTING PATH, NOT ONLY OWNER-RESOLVER UNIT FACTS
Reuse accepted CapturedFinalSubmitIntegrationTests/StorefrontPublicCommitEdgeTests harness and real OrderDbContext
on one disposable231target15433/order_tb231/tb231; identity beforeDDL, normal migrations, existing image,
remove/portclosed. Controlled foreign Contracts labelled honestly; no live-owner/HTTP claim from them.
1 Marketplace cart with at least TWO seller business identities, including Marketplace-owned platform group AND
   Seller-owned group(s), SAME catalog binding but owner-specific captures/labels/rows/dates/fees. Actual read plan
   -> selection -> public payment preview -> public commit -> persisted Order owner/evidence; own-member isolation,
   correct full-owner219cache call count and one goods quote per evaluation. Don't manually inject accepted owner
   to bypass a missing selection writer branch.
2 SellerA vs SellerB same service: own snapshots/labels remain distinct; swapping owner/pointer/evidence rejected
   with zero reservation/order; explicit permission own View/Manage/foreign denial under existing authorizers.
3 SingleStore tenantA/B control: exact owner keys separate, wrong tenant/inactive/unset/residualMarketplaceTenant
   refuses. Do not call same-DB fixtures proof of per-tenant physicalDB isolation; edition/connection factory owns it.
4 Preview response actual cart version7, real DB-sourced controlled title/seller metadata, correct aggregate from
   accepted groups; deliberately replace selection between outer facade read and fresh core read via TEST boundary,
   assert response is one coherent accepted revision/money/date set or explicit conflict, never a hybrid.
5 Public commit/replay response OrderLineIds in own field, no OrderIds in CartLineIds, and absent historical groups
   NOT rebuilt from current draft. Original order unchanged after publication/selection changes.
Run new exact owner-flow/contract repair class(es) + directly affected existing public edge/selection classes ONCE,
affectedOrder/Shipping endpoint projects only if changed. No200/25/68/124/36/28/31/50/119/188/194/285/310/312/full
Order/Shipping/Host/solution/guard replay. Don't replace functional proof with parser/negative-only facts.

PRESERVATION / HANDOFF
NO frontendHMR/browser/HostFE runtime restart/rebuild/live business write/shared migration/schema/baseline edit.
USER VISIBLE reviewwindow is USER unsavedwork: no enumerate/read/automate/reload/close. LocalpreviewONcount7
unchanged; 231/232 sharedNOTAPPLIED/EFsnapshotgap retained. No actual PSP/Payment/Settlement policy change.
Task verbatim+RESULT, ALL3recoveryTOPs. Record precise owner matrix and actual scope gaps. User final release
STILL requires coupons + existing frontend + complete desktop/mobile trial, not "SingleStore only accepted".
Known230emptykey/inflight/OfferAPI/227residual/225-R1lostLOCALwork retained; remove cross-owner label gap only
if actually repaired/proven. TB185paused/defaultOFF/PendingAllocation.
Start at ISSUE commit before edits; later Architect docs can be merged without rewriting shared/source history.
Exactpathscommit/push/fetch parity, no amend/force/reset/stash/rebase; CRLFdrift untouched. Actualtime/corrections;
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
