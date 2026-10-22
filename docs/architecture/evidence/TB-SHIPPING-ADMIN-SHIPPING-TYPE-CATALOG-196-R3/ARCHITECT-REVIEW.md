# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3
Date: 2026-10-22
Test checkpoint: 33c056deeade4d532fa42a87fff2fc2c21c32301
Observed HEAD: cc8059688982d5ed647119b8377735cf009821ec (later worker-result documentation only).
Verdict: PARTIAL_ACCEPTED_EVIDENCE / REPAIR_REQUIRED for overstated test claims.
No production defect identified in this test-only slice. TB-196 remains NOT ACCEPTED.

## Accepted bounded evidence
Read the entire new endpoint test source and compared the later documentation-only commit. Real module endpoint delegates, module authorizer registration, body reader, MediatR handlers/validators, store with EF InMemory and canonical factory/presenter are used. The authorization/context rejection and unauthorized malformed-body zero-read facts are meaningful. Selected lifecycle/status/code immutability and query-language invariance evidence is useful. Reported 42/42 is Worker execution, not an architect rerun.
Store/language-adapter registrations are manually reproduced in the harness, while endpoint-presentation registration is invoked directly. This is production types in a test composition, not independent proof that every production Infrastructure DI registration remains correct. Fixture languages are fa default plus en, not the requested single non-fa default. Full registry matrix remains pending.

## Three exact evidence defects
R196-R3-01: UpdateBody always serializes translations:[] despite the lifecycle comment/report claiming repeated identical NONEMPTY text. FixedClock also never advances, so equal UpdatedAt alone cannot detect a buggy restamp. Build the identical nonempty patch from actual GET text and use an advancing controlled clock; assert raw text, root revision and actual stored timestamp unchanged.

R196-R3-02: Post_rejections... takes a string "_" that is unused; POST/PUT validation tests assert HTTP400 but not validation.failed or exact stable child codes. LanguageLookups==0 supports non-arrival at the current first handler I/O but is not a separately measured HandlerReached counter. Add exact root/child code assertions from authoritative validators and a real after-validation pipeline arrival counter if retaining that claim. Do not map codes from exception prose.

R196-R3-03: Route classification switch assigns request names manually from route strings. The tests compare actual route strings and resolve validators, but never assert CountingSender.SenderRequests for EACH delegate or handler resolution. Thus they do not prove endpoint-to-request-to-handler mapping; a changed dispatch type could leave the classification test green. Classify observed request types from four real invocations, match real IRequestHandler/validator registrations, and retain both-direction route equality. Add a negative that changes observed request mapping while preserving the SAME four route strings; route-count/export negatives alone do not prove mapping integrity.

## Next task only
TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4, parent196-R3: test/evidence-only repair of those three facts with the existing harness, cap20minutes. No production change, new full harness, layout changes, broad suites or registry/listener/DB work. Existing useful evidence remains historical; append corrections rather than representing 42 green cases as proof of absent assertions.
After review, remaining independent dynamic-registry lifecycle, real-listener tracing/DI, exact-constraint negative and relational/snapshot/race gates still need separately bounded work. No Shipping assignment/UI/quote/activation/deployment; TB-185 paused.
