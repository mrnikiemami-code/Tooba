# TB-TMAR-STORY-AMSC-001-W3-R2 — Bounded Route-to-Dispatch Set-Equality Proof

- **Skill:** `tooba-architecture-certify` (§6a hard gate — `BOUNDED_DURABLE_ROUTE_REQUEST_SET_EQUALITY_PROOF`)
- **Mode:** `BOUNDED_DURABLE_ROUTE_REQUEST_SET_EQUALITY_PROOF`
- **Target:** `src/backend/Modules/Story` (test/evidence only)
- **Starting HEAD:** `47284caad070b28ae79e803946dd19aed4b1678c` (W3-R1)
- **Parent task:** `TB-TMAR-STORY-AMSC-001-W3-R1`
- **Branch:** `main`
- **Production files changed:** **0**
- **Verdict:** `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` — set-equality proof closed

---

## 1. The gap being repaired

The pre-existing Story AMSC guards are honest but not set-equal:

| Guard | What it actually proves | Blind spot |
| --- | --- | --- |
| `StoryModuleAmsc001W3CertGuardTests` | 25 route mappings, 25 `sender.Send(` call sites, 16 `AbstractValidator` classes, 25 `IRequest`/`IRequestHandler` | counts only |
| `StoryModuleAmcW5ValidatorGuardTests` | the **set** of newly constructed `*Command`/`*Query` names equals a 25-row manifest | set membership only |
| `StoryModuleAmcW1/W3/W4/W6` | folder/namespace/result/validator shape | not route↔dispatch provenance |

A within-set dispatch replacement — swapping one route's dispatched request for another request that
already belongs to the same 25-type set — preserves the route count, the `Send` count **and** the type-set
membership. None of the above guards can see it.

## 2. What this wave adds

`src/backend/Host/Tooba.Host.Tests/Architecture/StoryModuleAmsc001W3R2SetEqualityGuardTests.cs`
(10 `[Fact]`s, test-only). It derives the **actual** dispatched request of every shipped route from the
endpoint source and asserts exact equality of the full `audience + verb + full route + handler → request`
map. The parser is a pure function over source text (no production change, no on-disk mutation needed).

Supported dispatch shapes, both verified on disk and exercised synthetically:

- `Send(new T(...), ct)` — inline construction;
- `var x = new T(...); Send(x, ct)` — variable dispatch traced to its local construction.

Fail-closed behaviour (throws rather than assuming compliance):

| Condition | Result |
| --- | --- |
| handler with no `ISender.Send` | `InvalidOperationException` |
| handler with **more than one** `Send` call site | `InvalidOperationException` |
| `Send(<unrecognised expression>)` | `InvalidOperationException` |
| variable whose construction cannot be traced | `InvalidOperationException` |
| route-registration-looking `.MapX(` outside the 5 known verbs (`MapMethods`, …) | `InvalidOperationException` |
| a `.Map*` call not matched by the route regex | `InvalidOperationException` |
| member body not extractable | `InvalidOperationException` |

## 3. Derived exact map — 25 rows (from disk, not from the manifest)

| # | Audience | Verb | Full route | Handler | Dispatched request |
| --- | --- | --- | --- | --- | --- |
| 1 | storefront | Get | `/v1/storefront/stories` | `GetPublicStoriesAsync` | `GetPublicStoriesQuery` |
| 2 | seller | Get | `/v1/seller/stories` | `SellerListAsync` | `ListSellerStoriesQuery` |
| 3 | seller | Get | `/v1/seller/stories/{id:guid}` | `SellerGetAsync` | `GetSellerStoryQuery` |
| 4 | seller | Post | `/v1/seller/stories` | `SellerCreateAsync` | `CreateSellerStoryDraftCommand` |
| 5 | seller | Put | `/v1/seller/stories/{id:guid}` | `SellerUpdateAsync` | `UpdateSellerStoryCommand` |
| 6 | seller | Post | `/v1/seller/stories/{id:guid}/submit` | `SellerSubmitAsync` | `SubmitSellerStoryCommand` |
| 7 | seller | Post | `/v1/seller/stories/{id:guid}/items` | `SellerAddItemAsync` | `AddSellerStoryItemCommand` |
| 8 | seller | Put | `/v1/seller/stories/{id:guid}/items/{itemId:guid}` | `SellerUpdateItemAsync` | `UpdateSellerStoryItemCommand` |
| 9 | seller | Delete | `/v1/seller/stories/{id:guid}/items/{itemId:guid}` | `SellerRemoveItemAsync` | `RemoveSellerStoryItemCommand` |
| 10 | seller | Put | `/v1/seller/stories/{id:guid}/items/reorder` | `SellerReorderItemsAsync` | `ReorderSellerStoryItemsCommand` |
| 11 | admin | Get | `/v1/admin/stories` | `AdminListAsync` | `ListAdminStoriesQuery` |
| 12 | admin | Post | `/v1/admin/stories/query` | `AdminQueryGridAsync` | `QueryAdminStoryGridQuery` |
| 13 | admin | Get | `/v1/admin/stories/{id:guid}` | `AdminGetAsync` | `GetAdminStoryQuery` |
| 14 | admin | Post | `/v1/admin/stories` | `AdminCreateAsync` | `CreateAdminStoryCommand` |
| 15 | admin | Put | `/v1/admin/stories/{id:guid}` | `AdminUpdateAsync` | `UpdateAdminStoryCommand` |
| 16 | admin | Put | `/v1/admin/stories/reorder` | `AdminReorderAsync` | `ReorderAdminStoriesCommand` |
| 17 | admin | Post | `/v1/admin/stories/{id:guid}/enable` | `AdminEnableAsync` | `EnableAdminStoryCommand` |
| 18 | admin | Post | `/v1/admin/stories/{id:guid}/disable` | `AdminDisableAsync` | `DisableAdminStoryCommand` |
| 19 | admin | Post | `/v1/admin/stories/{id:guid}/schedule` | `AdminScheduleAsync` | `ScheduleAdminStoryCommand` |
| 20 | admin | Post | `/v1/admin/stories/{id:guid}/approve` | `AdminApproveAsync` | `ApproveAdminStoryCommand` |
| 21 | admin | Post | `/v1/admin/stories/{id:guid}/reject` | `AdminRejectAsync` | `RejectAdminStoryCommand` |
| 22 | admin | Post | `/v1/admin/stories/{id:guid}/items` | `AdminAddItemAsync` | `AddAdminStoryItemCommand` |
| 23 | admin | Put | `/v1/admin/stories/{id:guid}/items/{itemId:guid}` | `AdminUpdateItemAsync` | `UpdateAdminStoryItemCommand` |
| 24 | admin | Delete | `/v1/admin/stories/{id:guid}/items/{itemId:guid}` | `AdminRemoveItemAsync` | `RemoveAdminStoryItemCommand` |
| 25 | admin | Put | `/v1/admin/stories/{id:guid}/items/reorder` | `AdminReorderItemsAsync` | `ReorderAdminStoryItemsCommand` |

The audience prefix is derived from each file's own `MapGroup` declaration (one per audience; the
storefront surface maps directly on `app`), and the route surface is owned solely by
`StoryEndpointModule.MapStoryModuleEndpoints` — the composition entry itself declares **zero** routes.

Counts asserted: **25** routes, **25** `ISender.Send` call sites (exactly one per route), **25 distinct**
actual request types, **exactly one** real `IRequestHandler<,>` and exactly one `record` declaration per
request, and **zero** orphan/duplicate/unmapped route.

## 4. Request → validator matrix (16 `VALIDATOR_REQUIRED` + 9 `NO_VALIDATOR_REQUIRED`)

| Classification | Requests |
| --- | --- |
| `VALIDATOR_REQUIRED` (16) | `ListAdminStoriesQuery`, `QueryAdminStoryGridQuery`, `CreateAdminStoryCommand`, `UpdateAdminStoryCommand`, `ReorderAdminStoriesCommand`, `ScheduleAdminStoryCommand`, `RejectAdminStoryCommand`, `AddAdminStoryItemCommand`, `UpdateAdminStoryItemCommand`, `ReorderAdminStoryItemsCommand`, `CreateSellerStoryDraftCommand`, `UpdateSellerStoryCommand`, `AddSellerStoryItemCommand`, `UpdateSellerStoryItemCommand`, `ReorderSellerStoryItemsCommand`, `GetPublicStoriesQuery` |
| `NO_VALIDATOR_REQUIRED` (9) | `GetAdminStoryQuery`, `EnableAdminStoryCommand`, `DisableAdminStoryCommand`, `ApproveAdminStoryCommand`, `RemoveAdminStoryItemCommand`, `ListSellerStoriesQuery`, `GetSellerStoryQuery`, `SubmitSellerStoryCommand`, `RemoveSellerStoryItemCommand` |

- The dispatched set derived from the endpoints equals the union of the two halves exactly, with no
  orphan, duplicate or unclassified request and no phantom classification.
- The halves are disjoint; the union is exactly 25.
- Concrete `AbstractValidator<T>` targets are read from `Stories/Validators/StoryValidators.cs`
  (**16** classes) and equal the `VALIDATOR_REQUIRED` half one-to-one.
- **Non-circular registration proof:** a real `ServiceCollection` is built through the canonical
  `AddToobaCqrsFoundation(typeof(IStoryDirectory).Assembly)`. Each of the 16 requests resolves to the
  **expected concrete validator type**; each of the 9 exempt requests resolves to `null`; every
  classified request is a real `IBaseRequest`; and `ValidationBehavior<,>` is installed exactly once in
  the pipeline.

### Non-circular input-provenance for the 9 exemptions

| Exemption | Provenance (verified on disk) |
| --- | --- |
| `GetAdminStoryQuery` | `Guid` route id under `:guid`; `TenantId` from the tenant seam |
| `EnableAdminStoryCommand` | `Guid` route id under `:guid`; `TenantId` from the tenant seam |
| `DisableAdminStoryCommand` | `Guid` route id under `:guid`; `TenantId` from the tenant seam |
| `ApproveAdminStoryCommand` | `Guid` route id under `:guid`; actor from the authorization seam |
| `RemoveAdminStoryItemCommand` | two `Guid` route ids under `:guid`; `TenantId` from the tenant seam |
| `ListSellerStoriesQuery` | no input beyond `TenantId` + `sellerPartyId` from the authorization seam |
| `GetSellerStoryQuery` | `Guid` route id under `:guid`; seam-derived tenant/seller |
| `SubmitSellerStoryCommand` | `Guid` route id under `:guid`; actor from the authorization seam |
| `RemoveSellerStoryItemCommand` | two `Guid` route ids under `:guid`; seam-derived tenant/seller |

The guard asserts, for all three endpoint files: every `{...}` route segment ends in `:guid`; the tenant
id is resolved fail-closed through `StoryHttpErrors.ResolveTenantId(ICurrentTenant tenant)`; seller/admin
actor and `sellerPartyId` come only from `auth.RequireAuthorizedAsync(...)`; no
`X-Tooba-Dev-Actor-User-Id` / `Request.Headers` shortcut exists; and every constructor argument of every
exempt request is a route-bound identifier or a server-derived value — never a client-bound body.

### Caller-controlled optional input is not exempted

`GetPublicStoriesQuery(Guid TenantId, string? Locale, string? Market)` carries two **caller-controlled
optional** strings. Optionality is not safety, so the request is `VALIDATOR_REQUIRED`, and
`GetPublicStoriesQueryValidator` applies shape-only rules (`StoryRules.LocaleMaxLength` = 16,
`StoryRules.MarketMaxLength` = 32, empty allowed) with stable codes `story.locale.invalid` /
`story.market.invalid`. This is the W1 closure re-verified, not re-opened.

## 5. Negative mutation proof (in memory)

The mutation swaps the `SellerListAsync` dispatch:

```text
- new ListSellerStoriesQuery(tenantResult.Value, sellerPartyId)
+ new GetSellerStoryQuery(tenantResult.Value, sellerPartyId, id)
```

`GetSellerStoryQuery` is already a member of the 25-type set, so:

- route count unchanged (9 in the seller file, 25 overall);
- `sender.Send(` count unchanged (9 / 25);
- the legacy distinct constructed-name **set** is unchanged, because the mutation moves a *construction
  site* rather than adding or removing a name: `ListSellerStoriesQuery` is still constructed (now from
  `SellerGetAsync`'s sibling path in the mutated text — the name set is a set, not a multiset) and
  `GetSellerStoryQuery` was already constructed by `SellerGetAsync`;
- **the exact route → request map assertion fails** (`/v1/seller/stories` now maps to
  `GetSellerStoryQuery` instead of `ListSellerStoriesQuery`), which is the new gate.

The guard asserts the legacy count **and** legacy set checks still pass on the mutated text, then asserts
the new exact-map check fails — demonstrating both the blind spot and the repair. The shipped source is
re-read afterwards and still passes the exact map, so no production byte was touched and nothing is
committed. No on-disk mutation was needed (the parser is pure over text).

## 6. Preserved truths

- `storyAmsc001W3` certification truth unchanged (`STORY_AMSC_001_CERTIFIED`,
  `COMPLETE_REFERENCE_PATTERN`, `ARCH-COMPLETE-002`, `CERTIFIED`, `HTTP_OWNING`, `MODULE_OWNED`, 0 Host
  routes, 25 requests, `EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED`, 0 unmapped, ZERO
  foreign coupling, microservice-extractable).
- `storyAmsc001W3R1` recovery record not rewritten.
- Historical AMC-001 lineage (`storyModuleAmc001` … `storyModuleAmc001W6Cert`) preserved verbatim.
- Manifest **NOT_TOUCHED** (no false claim was found); schema/migrations **UNCHANGED**; frontend frozen.
- Repository-global Host root checkpoint exactly preserved: `lastAcceptedTask =
  TB-TMAR-HOST-ROOT-FINAL-CERT-001`, `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`,
  `workflowStop = USER_REVIEW_HOST_ROOT_FINAL_CERT_001`, `automaticNextImplementationTask = NONE`.
- `structureLock.certifiedModules` still lists `Story` exactly once.
- Guards weakened **NONE**; baselines widened **NONE**.

## 7. Durable guard

`StoryModuleAmsc001W3R2SetEqualityGuardTests` (10 `[Fact]`s):

1. `Shipped_routes_and_actual_dispatched_requests_match_the_exact_25_row_map`
2. `Dispatched_request_set_equals_the_16_required_plus_9_exempt_partition_with_no_orphan_duplicate_or_unmapped_route`
3. `Concrete_validator_targets_from_disk_equal_the_sixteen_required_requests`
4. `Validators_are_discoverable_through_the_real_cqrs_registration`
5. `Exempt_requests_rest_on_guid_route_constraints_server_derived_actors_and_no_client_input`
6. `Caller_controlled_locale_and_market_are_not_exempted_and_are_transport_validated`
7. `Every_route_dispatches_through_exactly_one_sender_send_and_no_host_owned_story_route_exists`
8. `Replacing_one_dispatched_request_while_preserving_counts_and_type_set_fails_the_exact_map_gate`
9. `Untraceable_variable_dispatch_multi_dispatch_and_orphan_handler_fail_closed`
10. `Sot_records_the_w3r2_set_equality_proof_without_touching_the_certified_truth`

## 8. Verification

```text
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj
  -> Build succeeded. 0 Error(s)

dotnet test --filter "FullyQualifiedName~StoryModuleAmsc001W3R2SetEqualityGuardTests"
  -> 10/10 passed

dotnet test --filter "FullyQualifiedName~Architecture"
  -> exact pre-existing failure-name set identical to the clean starting HEAD
     (no newly-red guard; the declared-red family is unchanged)
```

`git diff` confirms **zero** production change: the change set is guard-only plus task/evidence/SoT and
module-local Master Recovery metadata.

## 9. Stop gate

```text
VERDICT: COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 — set-equality proof closed
Route-Request-Exact-Map-State: EXACT_25_ROUTES_25_SENDS_25_REQUESTS_25_HANDLERS
Request-Validator-Matrix-State: EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED
Exemption-Provenance-State: NON_CIRCULAR_ALL_NINE
Mutation-Negative-Guard-State: WITHIN_SET_SWAP_FAILS_EXACT_MAP_WHILE_COUNTS_AND_SET_PASS
Production-Code-Changed-State: ZERO
Stop gate: USER_REVIEW_STORY_AMSC_001_W3_R2
automaticNextImplementationTask = NONE
```

Worker PASS is not Architect ACCEPT. No further module or follow-up task is started automatically.
