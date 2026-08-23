# Tooba — Party, Organization & Membership Foundation

Status:

```text
COMPLETE — Architect accepted TB-P02-T003
```

Task:

```text
TB-P02-T003
```

```text
Identity/User != Party/Organization
Membership != Authorization
```

This document locks the P02 Party implementation. P00 design remains in `docs/architecture/06-party-organization-b2b-foundation.md`. Identity remains `37-identity-authentication-foundation.md`. SpiceDB remains `38-spicedb-authorization-foundation.md`.

## Identity / User vs Party

`UserAccount` is the login principal. It has no `PartyId`, `OrganizationId`, or commercial role columns.

`BusinessParty` is the business-domain person or organization. It does not store login email/phone credentials.

Linkage is explicit (`user_party_links` in schema `party`) using an opaque `UserId`. There is no cross-module FK to Identity tables.

A User may have memberships in multiple organizations. An organization may have multiple user memberships. Cardinality is not forced 1:1.

## Person vs Organization

`PartyKind.Person` and `PartyKind.Organization` are the only kinds now. Seller/Agency/Customer are not User roles and are not a closed `SellerOnly` enum on Organization.

Organization capabilities (`party_capabilities.capability_code`) are extensible strings (`seller`, `agency`, `corporate_buyer`, …). One organization may hold multiple future commercial capabilities. Those capabilities are not implemented as products in this task.

## Membership

`memberships` is first-class business data: MembershipId, UserId, PartyId, Status, RelationCode, CreatedAt.

`RelationCode` expresses business association (`member`). It is not a final permission model and is not a `Role`/`Permission` column.

```text
Membership exists
→ Outbox integration event
→ projection handler
→ SpiceDB schema determines permission
```

## Organization relationships

`organization_relationships` is a typed extensible seam (`parent_of`, `operated_by`, `represents`, plus future codes). Full B2B rules, seller onboarding, and agency portals are out of scope.

## Persistence / tenant

Party owns `PartyDbContext` and PostgreSQL schema `party`, including module Outbox. No shared Identity DbContext. No cross-module joins.

Marketplace: Party rows live in the marketplace database. Single-Store: Party rows live in that tenant’s database, resolved from commerce context, not Host.

Tenant A Party/Membership data must not appear in Tenant B’s database.

## Party DB vs SpiceDB

```text
Party DB = business source of truth for Party/Membership/organization metadata
SpiceDB = authorization relationship projection
```

SpiceDB is not the master for organization names or capabilities. There is no distributed transaction. Party `SaveChanges` does not call SpiceDB.

Projection path:

```text
Party local transaction
→ module-owned Outbox
→ party.membership_established.v1
→ PartyMembershipProjectionHandler
→ IAuthorizationTupleWriter
```

Handler uses Tooba authorization types only. Authzed.Net stays in Host.

Foundation SpiceDB schema version `2` adds `definition party { relation member: user; permission view = member }` so projected tuples have a home. Catalog/Order permissions are not added.

## Public contracts

`PartyReference`, `OrganizationReference`, `MembershipReference`, `UserPartyLinkReference`, `OrganizationRelationshipReference`, `IPartyLookupGateway`, `IPartyDirectory`. EF entities are not the module public API.

## Deferred

Full B2B workflows, Seller onboarding, Agency portal, Customer dashboard, contracts, credit terms, Catalog, Pricing, Tax, Cart, Order, Payment, Shopeiva, Data Grid, Design System, commercial UI.
