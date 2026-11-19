# TB-282-R1 ARCHITECT REVIEW — 2026-11-19

Source reviewed at 77cb3d9bf12c7ec9cbd70c28e0cc9515c1937789 (feature commit 98d3e1fcdaf74227e22034325cbdef639b0d5d78); actual final origin/main d0c09e2ed340bdf82c9a6e4ae8eaeb8f7ed96d58.
Verdict: PARTIAL / REPAIR REQUIRED. Retain product progress, not full F1–F5 acceptance. No new live writes or independent Windows execution by Architect.

## Retain
Stable new/detail route adapters, name picker, more edit/slide/action controls, preview confirm, actionCodes use, full seller grid conversion, scoped local saved views, public CtaHref and attempt-distinct platform object keys are implemented.
Gate0 now takes explicit ownedStoryIds; scoped unit proof accepted, real PG skip remains a disclosed limit. No need to replay 281 or unrelated backend suites.
Product /products/[slug] is an actual app route; resolved public product path is valid. Do not reopen that destination format.

## B1 — Recovery UI still does not recover
Pending card button only toast.info + openDetail; it never dispatches stored method/path/body. The next ordinary click mints another key and is blocked by the old stored pending entry. Thus the promised exact retry is not usable.
runDurableStoryMutation accepts an arbitrary send closure instead of dispatching frozen envelope. create stores {title,key,sellerPartyId} but API expands a different DTO; upload_add stores bodyText=null and neither frozen caption/duration/CTA nor add body. Child marks done without storing returned asset id; retries cannot resume only add after lost add response.
storeStoryIntent allows changed payload under the same key, namespaces storage without actor, checks no complete actor/path/body/action binding and hides malformed entries in listPendingStoryIntents. The executor merely checks actor string is nonempty, not current scope. It can clear after ok=true with missing/malformed data.
Asynchronous digest precedes the mutex. Mutable body state/headers can be rebuilt. Old v1 evidence is ignored. These are source failures in F1, not cosmetic limitations.

## B2 — Merchant date and lookup authority
Schedule inputs are raw text, label YYYY-MM-DDTHH:mm with example 1404-08-28T10:00. new Date(value) parses this as Gregorian year 1404 in browser timezone. The hardcoded Tehran label does not convert it to the accepted zone. This can schedule a merchant's intended future date in the wrong century/instant.
Seller directly calls loadAdminLanguages (admin API/headers); admin-for-shop calls loadSellerOffers with the seller API/session, not an authorized admin read. Platform product options are empty when shop is null. CatalogStoryProductTargetAdapter still tests the winning product card's SellerPartyId rather than actual A offer eligibility. The required lookup ownership path is not complete.

## B3 — Seller/grid integration and test boundary
Same-tab shop/actor writes have no subscription in StoryManagementScreen. Re-reading storage only during render is not scope-change handling. Inline grid callbacks/new router objects may contribute to the reported grid act storm; root cause is not established.
disableLiveGrid is a production component test-only bypass. Five tests do not mount VendorShell/grid or exercise network-loss recovery/upload chain/date/player. The supposed negative control test only asserts a normal overwrite rejection; no temporary broken implementation is executed.
Tests also import another feature's test helper (order-carrier-execution); use neutral existing test infrastructure, not product-feature dependency.
Skipping a heavy grid in an isolated editor test can be legitimate, but it is not full-panel proof. Need actual grid/query and shell boundary proof plus served read-only checks.

## B4 — Platform scope and runtime
UploadForPlatformStoryAsync still writes AssetOwnerOccurrences with SellerPartyId=Guid.Empty (platformShop) despite explicit no fake seller scope decision. Its lookup key also omits actor while deterministic asset id includes actor. Preserve asset-id/metadata identity and attempt-unique storage keys, remove invented seller ledger scope; no schema needed.
RECEIPTS provides focused tests only. No post-final tsc, Release build, Host publish, Next production build or actual served identity/DOM measurements are evidenced. user-review URLs say 'after serving'. Do not send owner to a stale binary and call this readiness.

## Continuation
One 282-R2 closes B1–B4 in the same product scope, retains completed editor/grid/player pieces. No new product phase, migration, live mutation, seed, permission fixture, key operation or baseline sweep. R1 historical PASS stays intact with explicit correction addendum.
Returns acceptances/payout backlog remain protected. No Architect/user/commercial acceptance yet.
