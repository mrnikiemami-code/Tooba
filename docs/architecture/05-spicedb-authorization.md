# Tooba — SpiceDB Authorization Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T006
```

Documentation only. Do not treat this file as SpiceDB schema DSL, an SDK choice, or a hosting plan.

Related:

```text
docs/architecture/03-data-ownership-and-module-contracts.md
docs/architecture/04-identity-authentication.md
```

## A. Core Separation

```text
Authentication answers: Who is this principal?
Authorization answers: May this subject perform this action on this resource?
```

SpiceDB is the authorization **decision** system. Identity must not duplicate it with a fixed application role matrix (`IsAdmin`, `Role=Seller`, …).

Party/Organization owns **business** relationship lifecycles. SpiceDB stores/projects **authorization-relevant** relationships needed for checks. SpiceDB is not the system of record for catalog copy, prices, or order lines.

## B. Authorization Vocabulary

| Term | Meaning |
| --- | --- |
| Subject | Who is asking (Identity, Party acting for Organization, service principal, …). |
| Resource | What is being accessed, at a chosen granularity (not every table row). |
| Relation | Named link (member, owner, seller_operator, …). Candidate semantics, not schema. |
| Permission | Computed allow/deny for an action on a resource (`may_view_order`). |
| Caveat / Contextual Condition | Extra authorization-relevant constraint (tenant status, assurance), not pricing logic. |
| Tuple / Relationship | A stored subject–relation–resource fact (or derived equivalent). |
| Authorization Check | Decision request at a use-case boundary. |
| Authorization Write | Mutation of relationships, driven by business facts. |
| Authorization Projection | Optional derived view for lists/UI; not business write-model. |

Business names map onto these concepts. SpiceDB does not become the owner of business data.

## C. Subject Model

Candidate subject types:

```text
Identity
Party
Organization
Service Principal
System Actor
API Client
```

**Direction:** the authenticated principal is an **Identity**. Business acting-as is a **Party** (and, for B2B, a Party **in the context of an Organization**). Checks should carry both: who logged in, and which Party/Organization they currently act as — authorized via relationships, not by swapping accounts.

Do not conflate Identity with Organization. Future B2B:

```text
human Identity acts on behalf of Organization
```

via membership/delegation relations, not duplicated logins.

Machine subjects (service principal, API client) are distinct from human Identities (see Q).

## D. Resource Model

Example resources:

```text
Tenant / Store
Organization
Seller
Catalog Product management scope
Offer
Order
Customer profile
Content item
Landing page
Promotion
Support ticket
Media asset
Admin operational capability
Report / Analytics view
```

Not every row is a SpiceDB resource.

**Granularity criteria (use a resource when):**

- access must be scoped (this seller, this order, this tenant);
- relationships (owner/member/assignee) matter;
- list/filter by permission is a real UX/security need;
- extraction/audit of that permission is likely.

**Do not model as resources when:**

- the fact is a local field of an already-owned aggregate (e.g. a price cell);
- the question is a business rule (eligible to cancel) rather than “who may call this operation”;
- volume would explode without adding security (every SKU cell).

Catalog Product **management scope** is not the same as Catalog Product identity: sellers do not gain canonical product-write from seller relations (T002/T003/T004).

## E. Relationship-Based Access Control

Candidate relation semantics (not schema):

```text
member
owner
manager
editor
viewer
seller_operator
finance_operator
support_agent
customer_self
organization_buyer
organization_admin
```

Prefer scoped relationships over universal global roles:

```text
user A is manager of seller X
```

is not:

```text
user A is global seller_manager
```

Global relations are exceptional (bootstrap/super-admin policy), not the default.

## F. Tenant / Store Scope

Single-Store shared deployment:

- tenant/store is already resolved by trusted host routing (platform);
- every check includes that store/tenant resource (or a resource parented to it);
- a permission on store A never authorizes store B;
- relationship namespaces/prefixes/object ids must encode isolation so tuples cannot collide across stores.

Marketplace:

- do not invent a fake Single-Store tenant;
- use marketplace, seller, and resource relationships in that dedicated deployment.

Candidate isolation approaches (schema deferred):

- object id prefix / type namespacing per tenant;
- tenant as parent resource that permissions inherit through;
- separate SpiceDB prefixes per deployment (Marketplace vs a given Single-Store host’s store).

Exact schema:

```text
NEEDS_LATER_P00_DETAIL
```

## G. B2B Organization Authorization

Conceptual relationships (future, not implemented):

```text
Identity -> Party
Party -> Organization membership
Organization -> buyer/admin/finance roles
Organization -> contracts/accounts
Organization -> orders
delegated acting authority
approval hierarchy
```

Permissions that must remain possible:

```text
may_create_order
may_approve_order
may_view_org_orders
may_manage_org_users
may_view_credit
may_use_contract_price
```

`may_use_contract_price` is **authorization to request** a Party/contract-scoped quote. Pricing still owns the quote. SpiceDB does not compute prices.

Do not implement B2B now.

## H. Seller / Marketplace Authorization

Seller-scoped candidates:

```text
seller owner
seller admin
catalog contributor
offer manager
inventory operator
order operator
finance viewer
support operator
```

Seller permissions do **not** imply ownership of canonical Catalog Product truth. Offer/inventory/order operations stay within those modules’ ownership. A catalog contributor may be allowed to propose/bind listings, not to rewrite global product identity.

## I. Admin Authorization

Do not design Admin as:

```text
IsAdmin = true
```

Capability/relationship directions:

```text
catalog moderation
seller approval
order support
payment operations
content publishing
promotion management
analytics access
security administration
tenant operations
```

Least privilege: grant the relation on the needed resource/scope. Global superuser/bootstrap exists only as explicit exceptional policy (see W), not the default.

## J. Customer Self-Service

```text
view own profile
edit own addresses
view own orders
cancel eligible own order
view own support tickets
manage own sessions
```

“Own” is a **relationship** (e.g. `customer_self` on Party/Order) established from trusted server-side facts after authentication — never from client-supplied ids alone. Eligibility to cancel remains an Order business rule after the permission check.

Sessions: Identity owns session lifecycle; SpiceDB may gate “manage own sessions” but does not store session tokens.

## K. Ownership vs Authorization Truth

Business domains remain SoT for membership/ownership facts (Party, Seller, Order buyer, …).

SpiceDB holds what the decision engine needs.

Synchronization direction:

```text
business fact changes
→ integration/outbox event
→ authorization relationship update
```

or a synchronous write in the same use-case when the next check must not see a lag (e.g. revoke seller admin).

Do not pick one consistency model for every relation. Security-sensitive revocations lean synchronous or fail-closed until sync is confirmed (see L, S).

## L. Authorization Consistency

| Concern | Direction |
| --- | --- |
| Strongly consistent check | Prefer when SpiceDB is the live decision store for that permission |
| Eventually updated relationship | Acceptable for low-risk grants (e.g. adding a viewer) if UX tolerates delay |
| Permission revocation latency | Treat as security-sensitive; minimize; fail closed if uncertain |
| Security-sensitive changes | Disable, remove membership, tenant suspend: do not serve stale allow |

Do not rely on stale local caches for security-critical authorization unless bounded by explicit later policy.

Exact per-relation consistency:

```text
NEEDS_LATER_P00_DETAIL
```

## M. Check Placement

Checks belong at **application/use-case boundaries** and at sensitive domain operations. UI hiding is not a security boundary.

Examples: API/application command/query entry; admin operation; seller operation; B2B action; sensitive customer operation.

UI may call `Check`/`Lookup` for visibility. The server repeats the check on the command.

## N. Authorization Service Contract

Conceptual internal contract:

```text
IAuthorizationService
CheckAsync(subject, permission, resource, context)
BulkCheckAsync(...)
LookupResourcesAsync(...)
```

Names are conceptual. Business modules consume this contract, **not** the SpiceDB SDK.

Enables in-process adapter now, remote later, fakes in tests, provider evolution — same ACL pattern as T004.

No code in this task.

## O. Bulk / List Authorization

Admin/Seller/B2B lists must not N+1 `Check` per row.

Patterns:

```text
bulk check
lookup resources
pre-filtered projections
authorization-aware query composition
```

SpiceDB looks up **ids/permissions**. The owning module still loads business data via its own store/projection. Do not move order lines into SpiceDB to make a grid fast.

List authorization is first-class for professional dashboards.

## P. Contextual / Caveated Permissions

Possible future conditions:

```text
market
sales channel
order state
time window
MFA assurance level
tenant status
contract validity
```

Do not encode pricing formulas or checkout workflow in SpiceDB. Caveats only when the **authorization question** depends on them (e.g. require higher assurance for payment ops).

Exact caveat strategy:

```text
NEEDS_LATER_P00_DETAIL
```

## Q. Service-to-Service / Machine Authorization

Future subjects: service principal, API client, integration identity.

Do not reuse human user sessions as machine identity. Authenticate machine credentials in Identity (or a dedicated machine-auth path); authorize with SpiceDB using machine subject types.

## R. Auditability

Safe audit context:

```text
subject
permission
resource
decision
tenant/store
correlation/trace
reason/context where appropriate
```

Do not log full relationship dumps or secrets.

Differentiate:

```text
authorization decision telemetry
relationship mutation audit
business audit
```

Identity security events remain Identity-owned (T005). Authorization decision/mutation audit is authorization-owned.

## S. Failure Behavior

Fail closed for:

```text
SpiceDB unavailable
malformed resource
unknown subject
unknown permission mapping
tenant mismatch
relationship sync uncertainty in security-critical path
```

Do not silently allow on infrastructure failure. Controlled exceptions only if later explicit policy proves a path is safe (e.g. public catalog read that is not authorization-gated). Default: deny.

## T. Caching

Conservative:

```text
positive-result cache
negative-result cache
relationship/version tokens
short TTL
revocation impact
```

No Redis required initially. No cross-tenant cache keys/leakage. Security-sensitive operations may bypass or tightly bound caches. Negative caches must not hide a newly granted permission longer than policy allows; positive caches must not survive revocation.

## U. Testing Strategy — Architecture Level

Future implementation must support:

```text
permission matrix tests
relationship graph tests
cross-tenant denial tests
revocation tests
seller isolation tests
B2B delegation tests
customer self-scope tests
admin least-privilege tests
bulk/list authorization tests
```

No tests written in this task.

## V. Relationship to Domain Boundaries

```text
Identity -> authenticated principal
Party -> business parties/memberships
Seller -> seller membership/business roles
Content -> content ownership/workflow
Order -> order resource/business ownership
Authorization -> decision infrastructure / policy graph integration
```

No module bypasses authorization by querying another module’s tables (T004).

## W. Migration / Bootstrap Concerns

Future needs:

```text
initial super-admin
tenant owner provisioning
seller owner provisioning
organization owner provisioning
relationship backfill
schema version rollout
permission migration
```

Do not solve with hardcoded permanent superuser in application code. Bootstrap is an explicit, auditable provisioning path that then relies on the same check machinery.

## X. Decision Summary

### RECOMMENDED_FOR_ADR

1. SpiceDB as authorization decision foundation.
2. Authentication separate from authorization.
3. Business domains remain source of truth for business relationships.
4. SpiceDB represents authorization-relevant relationships/permissions.
5. No fixed role matrix duplicated in Identity.
6. Tenant/store isolation in authorization scope.
7. Seller-scoped relationship model.
8. B2B organization/delegation readiness.
9. Application/use-case boundary checks, not UI-only checks.
10. Internal authorization abstraction hides SpiceDB SDK.
11. Fail closed on authorization infrastructure failure.
12. Bulk/list authorization is first-class.
13. Authorization audit/telemetry without secrets.
14. Machine subjects distinct from human identities.

AI retrieval and tools are authorization-aware: SpiceDB remains the decision system; the assistant must not bypass checks or query internal tables. See `docs/architecture/17-ai-assistant-rag.md`.

### NEEDS_LATER_P00_DETAIL

- Exact SpiceDB object types, relations, and permission computation
- Tenant namespacing/schema isolation
- Per-relation sync vs eventual consistency
- Caveat catalog
- Cache TTL / consistency tokens
- Bootstrap/super-admin procedure
- Hosting/topology of SpiceDB

### DEFERRED

- SpiceDB schema DSL, deploy, SDK
- Middleware/checks/code
- Party/Organization/B2B/seller role implementation
- Production hosting choice
- Final ADR
- Shopeiva as requirements
