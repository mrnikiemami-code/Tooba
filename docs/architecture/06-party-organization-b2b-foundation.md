# Tooba — Party, Organization & B2B Foundation Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T007
```

Documentation only. Full B2B remains after first sellable release; this document preserves seams so Identity, Pricing, Order, and SpiceDB need not be redesigned.

Related: `03-data-ownership-and-module-contracts.md`, `04-identity-authentication.md`, `05-spicedb-authorization.md`.

## A. Core Concepts

| Term | Meaning |
| --- | --- |
| Identity | Login principal (T005). Not a profile or company. |
| Party | Business-domain person or organization. Not credentials. |
| Person | Human Party specialization. |
| Organization | Business Party specialization. Not a User row. |
| Customer | Role/context (who buys), not the universal root entity. |
| Seller | Marketplace commercial participant (Seller module). Backed by a Party; not the Party itself. |
| Business Account | Candidate commercial relationship wrapper (see K). |
| Membership | Party is member of Organization (business SoT). |
| Delegation | Scoped authority to act for another Party/Organization. |
| Actor | Principal performing the action (usually Identity/Person). |
| Buyer | Party on whose behalf the purchase is made. |
| Payer | Party financially responsible, if distinct from Buyer. |
| Sales Channel | Commercial context (not Identity). |
| Contract / Agreement | Commercial terms Pricing/Order may reference. |
| Credit Account | Future credit/terms facts (not Catalog, not Identity). |

```text
Identity != Party
Party != Authentication Account
Organization != User
Buyer != Acting User
Buyer != necessarily Payer
```

Do not use “Customer” as the root of all parties.

## B. Party Model Direction

Party is generic, with Person and Organization as specializations/capabilities. Inheritance vs table strategy is not locked.

Party owns conceptually:

```text
business identity
legal/display name
business contact/profile references
party status
party type
commercial relationships
organization membership source-of-truth where appropriate
```

Party does **not** own authentication credentials, SpiceDB as profile dump, prices, or orders.

## C. Person vs Identity

A human Party may exist without an active login Identity: guest buyer, imported corporate contact, recipient, invoice contact, support-created customer.

An Identity may authenticate before a rich Party profile exists.

Link lifecycle (conceptual): create/link/unlink/relink under policy and audit. **Not** a required permanent 1:1. Multiple Identities for one Person or delayed Person creation remain possible; exact cardinality is `NEEDS_LATER_P00_DETAIL`.

## D. Organization

Organization is a business Party that can later exhibit B2B behavior. Room for: legal/business name, registration/tax references, addresses, contacts, billing settings, memberships, buyer/finance roles, contracts, credit/payment terms, approval policies.

No country-specific legal rules in this task.

## E. Membership

Organization membership is a **business** relationship, not a login.

Conceptual facts: Party/Person is member of Organization; status; business role/context; validity; invitation/onboarding.

Party/Organization remains SoT for membership **lifecycle**. SpiceDB represents **authorization consequences**. Do not duplicate the permission matrix in membership rows.

## F. Delegation / Acting On Behalf Of

```text
Identity / Person
acts for
Organization
```

with scoped authority.

Analyze: membership-based authority; explicit delegation; temporary delegation; approval-limited delegation.

Authorization stays in SpiceDB. Delegation **lifecycle records** may belong to Party/Organization. Not implemented here.

## G. Actor vs Buyer vs Payer

Critical for later Order:

```text
Actor = authenticated/operating principal performing the action
Buyer Party = person or organization on whose behalf the purchase is made
Payer Party = party financially responsible, where distinct
```

Examples: employee orders for company; assistant for manager/org; parent pays for recipient; corporate procurement under company account.

Order must preserve these distinctions. No Order schema here.

## H. Sales Channel

Separate commercial context `SalesChannel`. Candidate later values (not locked): DIRECT, MARKETPLACE, B2B, CORPORATE, AFFILIATE, API.

May affect pricing, promotions, checkout policies, order metadata, authorization, reporting.

Not owned by Identity. Likely commercial/platform context (Market/commercial overlay), consumed by Pricing/Order via contract — `NEEDS_LATER_P00_DETAIL` for exact owner module.

## I. Customer Concept

Customer is a role/capability/context, not blindly a root entity.

Analyze: Person customer; Organization customer; Guest; Registered; Seller purchasing as buyer.

Every buyer need not be a logged-in natural person.

## J. Addresses & Contacts

Support person, organization, shipping, billing, seller business addresses, and **order snapshots**.

Order must snapshot historical shipping/billing. Historical truth must not depend on later mutation of Party address records. No schema.

## K. B2B Account / Commercial Account

Candidate concept separate from Organization identity:

```text
Commercial Account / Customer Account / Buyer Account
```

May hold: account status, credit terms, credit limit reference, payment terms, sales channel eligibility, price book/contract references, account manager.

Do not merge into Organization legal identity prematurely.

```text
CANDIDATE
```

Exact need: later B2B detail.

## L. Contract / Agreement Boundary

Preserve: price agreement, commercial contract, discount agreement, SLA, payment terms, validity window, market/channel scope.

Contracts do not own Product truth. Pricing consumes contract references/context. Order snapshots commercial outcome/contract reference where needed. No contract product.

## M. Credit / Payment Terms

Future: credit limit, net terms, invoice payment, exposure, approval before exceeding credit.

Not Identity, not Catalog.

Likely future boundary among Commercial Account / Credit, Payment, Order. Exact ownership:

```text
NEEDS_LATER_P00_DETAIL
```

## N. Approval Workflows

Preserve: purchase request, manager/amount/department/finance approval.

No approval engine now. Checkout/Order must later support Request-to-Buy / Approval **before** final placement where policy requires.

## O. Party & Marketplace Seller

A Seller may be backed by Organization Party, or later Person Party.

Seller/Marketplace owns seller **commercial** lifecycle. Party owns **business** identity. SpiceDB owns seller-scoped **authorization decisions**.

Do not merge Seller and Organization into one module.

## P. Tenant / Store Relationship

Single-Store tenant/store ≠ Organization automatically.

A store may be commercially owned by an organization, but Tenant/Store is **deployment/platform** context (T003).

Do not collapse `TenantId == OrganizationId` unless later explicit business model maps them. Use explicit references/contracts.

## Q. Identity Linking

Conceptual flows:

```text
register identity -> create/link Person Party according to policy
invite user to organization -> resolve/create Party -> link Identity -> membership
external IdP login -> internal Identity -> Party linkage
guest checkout -> Party/customer representation without full Identity
```

No implementation.

## R. Duplicate / Merge Concerns

Preserve future: duplicate detection, merge, link correction, identity relinking, organization merge/split.

Primary keys and login identifiers are not the only business-identity semantics.

## S. Privacy / Sensitive Business Data

- Party may contain PII;
- organization may contain regulated/legal identifiers;
- authentication secrets stay outside Party;
- SpiceDB is not a dump of full profiles;
- audit sensitive changes;
- minimize sensitive data in telemetry/events.

No extra compliance claims.

## T. Domain Ownership Matrix

| Concept | Owner | References | Consumers |
| --- | --- | --- | --- |
| Login identifier | Identity | — | Authn |
| Person profile | Party | IdentityId (optional link) | Order, Support |
| Organization identity | Party | — | Seller, B2B, Admin |
| Membership lifecycle | Party/Organization | Party ids | SpiceDB (consequences) |
| Permission | Authorization (SpiceDB) | subject/resource ids | All gated ops |
| Seller status | Seller / Marketplace | Party id | Offer, Admin |
| Buyer party | Order snapshot + Party SoT | Party id | Pricing context, Order |
| Acting identity | Identity + Order snapshot | IdentityId | Audit, SpiceDB |
| Payer party | Order/Payment snapshot | Party id | Payment |
| Contract reference | Commercial/Contract (future) | org/account ids | Pricing, Order |
| Credit terms | Commercial Account / Credit (candidate) | account ids | Checkout, Payment |
| Tenant identity | Platform | Host → tenant | All modules via context |

No cross-module table joins.

## U. B2C / Marketplace / B2B Scenarios

### B2C Registered Buyer

```text
Identity → Person Party → Buyer Party
```

### Guest Buyer

```text
no persistent login required → customer/party representation per later checkout policy
```

### Marketplace Seller Operator

```text
Identity → Person Party → seller/organization relationship → SpiceDB permission
```

### B2B Buyer

```text
Identity → Person Party → Organization membership → acts-for Organization
→ Buyer Party = Organization
→ Actor = Identity/Person
```

### B2B Approval

```text
employee creates request → manager/approver acts → organization remains Buyer Party
```

## V. Pricing Context Integration

Party/B2B supplies pricing **context** without Pricing reading Party tables.

Conceptual input: BuyerPartyId, OrganizationId, CommercialAccountId, ContractId, SalesChannel, Market, Quantity.

Pricing resolves via contracts/projections. Direct Party–Pricing DB joins are forbidden.

## W. Order Context Integration

Order must be able to persist/snapshot: BuyerPartyId; ActingIdentityId / Actor; PayerPartyId if different; SalesChannel; Organization/CommercialAccount reference; contract/price context reference.

Fields not locked. Historical truth survives later membership change.

## X. Authorization Integration

SpiceDB may involve Identity, Party, Organization, Seller, Order, Tenant/Store.

Party/Organization lifecycle **emits** authorization-relevant changes. Authorization is not SoT for legal/business profile.

## Y. Edition Overlay

| Capability | Marketplace | Single-Store | Future B2B |
| --- | --- | --- | --- |
| Person Party | SHARED_CORE / REQUIRED | SHARED_CORE / REQUIRED | SHARED_CORE |
| Organization Party | REQUIRED (sellers; optional buyers) | OPTIONAL (owner org) | REQUIRED |
| Membership / acts-for | OPTIONAL (seller staff) | OPTIONAL | REQUIRED |
| Guest buyer | REQUIRED | REQUIRED | OPTIONAL |
| Seller module overlay | REQUIRED | OPTIONAL (implicit seller) | OPTIONAL |
| Commercial Account / Credit | FUTURE | FUTURE | FUTURE |
| Approval / Request-to-Buy | FUTURE | FUTURE | FUTURE |
| Sales Channel | SHARED_CORE | SHARED_CORE | SHARED_CORE |

Single-Store must not drop the Party abstraction because it has one store/seller.

## Z. Decision Summary

### RECOMMENDED_FOR_ADR

1. Party separate from Identity.
2. Person and Organization as business-party concepts.
3. Organization not modeled as User.
4. Membership lifecycle owned by Party/Organization business domain.
5. Authorization consequences handled by SpiceDB.
6. Actor != Buyer Party.
7. Buyer Party != necessarily Payer Party.
8. Sales Channel independent commercial context.
9. Seller separate from Party/Organization identity.
10. Tenant/Store separate from Organization.
11. Order snapshots historical party/commercial facts where needed.
12. Pricing consumes party/contract context through contracts, never joins.
13. Future B2B approval/delegation preserved.
14. Guest/customer scenarios do not require every buyer to be an authenticated person.

### NEEDS_LATER_P00_DETAIL

- Identity–Person cardinality
- SalesChannel owning module
- Commercial Account vs Organization split
- Credit ownership among Account/Payment/Order
- Guest Party persistence policy
- Duplicate/merge process

### DEFERRED

- Implementation, schemas, APIs
- Membership/seller/B2B/approval/credit/contract products
- SpiceDB schema
- Final ADR
- Shopeiva
