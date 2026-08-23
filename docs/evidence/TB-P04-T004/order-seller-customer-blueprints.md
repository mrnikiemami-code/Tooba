# TB-P04-T004 — Order / Seller / Customer blueprints

Not implemented as screens.

## Order Workspace

- Summary: identity, money, status health strip.
- Buyer / acting user: Party identity seam, not a user-admin screen.
- Seller-scoped orders: filter context, not a second app.
- Lines: embedded grid → line inspector.
- Pricing/promotion/tax snapshot: read-mostly after capture.
- Inventory reservation: command + conflict if stock moved.
- Payment: status + retry command, no payment-module CRUD menu.
- Status timeline: activity feed.
- Future fulfillment: placeholder seam only.
- Audit: separate from activity.

## Seller Workspace

Seller is an organization, not a User.

- Organization identity / status/compliance seam.
- Offers, catalog associations, pricing, inventory: related grids inside the shell.
- Orders: seller-scoped embedded grid.
- Performance summary: read model.
- Users/memberships: Party membership seam.
- Audit: organization-level.

## Customer Workspace

- Identity linkage and Party context.
- Orders grid.
- Addresses/contact seam.
- Support notes / activity.
- Preferences.
- Future org/B2B: documented only.

## Other bounded blueprints

- Content Studio: editorial shell for pages/blocks/media; not a CMS rewrite here.
- Tenant Settings Workspace: tenant/brand/tax/payment configuration seams; no executable theme scripts.
- Return Case Workspace: case summary, lines, evidence, decision commands. Do not invent final Return domain semantics.
