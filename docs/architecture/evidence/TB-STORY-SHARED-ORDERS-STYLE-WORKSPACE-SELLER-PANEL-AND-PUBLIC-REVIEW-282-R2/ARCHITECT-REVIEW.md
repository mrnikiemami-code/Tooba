# TB-282-R2 ARCHITECT REVIEW — 2026-11-19

Reviewed code 2085d39d5641c387008edccaff5294b18ca8f4ae; actual origin/main 9776ab9623aadb2d58f001113ce8269039b11a94. Docs-only parity commits retained.
Verdict: ACCEPT scoped B2 role lookup routes/Tehran date conversion, B3 removal of production grid bypass/scope event, and B4 current-schema platform identity progress; overall product closure remains PARTIAL due S1/S2 below.
Worker execution/build/runtime receipts are attributed evidence, not independent Windows/browser execution by Architect. No live writes performed by this review.

## Retained
Recovery button now actually dispatches frozen bytes. Upload/add journal carries stored child state. Actor/role/shop namespaces, same-key body refusal and role-specific Story lookup endpoints are implemented.
Jalali day/time conversion no longer interprets year1404 as Gregorian; current Tehran authoring behavior is bounded scope, not a universal configurable-zone claim.
Platform upload no longer manufactures Empty seller occurrence rows; deterministic asset-PK+stored metadata, per-attempt storage keys and actor binding are retained.
Post-final tsc/build/publish/Host+Next served evidence now exists (Host30848, Next29064, BUILD_ID j3IOV0KOGPfOZrB3IX_SK). Current URLs may be used to inspect visible UI, but full functional acceptance not granted.

## S1 — malformed success still destroys unknown evidence
story-executor.finalizeResponse accepts ANY 2xx even when response.text is empty, JSON parse failed, mapOk returns null or snapshot lacks identity/binding. runDurableStoryMutation then clears current intent. An HTTP 200 is not proof the reviewed Story command completed. This directly violates the issued malformed-success requirement.
Also validateStoryEnvelope only checks route strings are nonblank, not that method/path/action/target/body bind; listPending looks only under the new actor namespace and does not discover actual old v1/v2 prefixes. typeof sessionStorage outside try and enumeration catch=>[] do not provide the claimed unreadable barrier. Complete this same frontend ingress/outcome boundary; no new backend protocol needed.
Tests currently verify 500->valid partial create reply and button dispatch, not malformed 200 retention/known-route binding. BrokenStore in a separate test is not a production-negative-control run.

## S2 — platform picker uses a DEVELOPMENT sampler of 40
StoryProductOptionsAdapter.ListPublishedAsync depends on ICatalogDevelopmentPublishedProductSampler.TakePublishedProductIdsFromDistinctCategoriesAsync(40). It cannot search or page the real published catalogue and can return empty when the optional development dependency is absent. The UI list is therefore not an authoritative full product picker. Replace only this production read path with real owner-scoped search/page/count through Contracts; no schema or catalogue redesign.
Preserve Offer-based seller binding and real public slug navigation. Do not restore card-winner ownership shortcuts.

## Bounded continuation
Issue only 282-R3 for S1 verified response/storage boundary and S2 real catalogue options. Do not replay Media/Gate0/whole module certification or rebuild the editor. Existing unrelated critical-storefront landing finding stays declared, not an excuse for unrelated restyle.
After focused tests and current served identity, hand user a normal trial checklist. Do not claim Marketplace/user/commercial acceptance on behalf of the owner.
Returns acceptances/payout backlog/user data/key state untouched. Existing VendorShell /vendor-panel/stories remains mandatory.
