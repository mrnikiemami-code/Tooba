# ARCHITECT REVIEW — TB-SHIPPING-ADMIN-SALES-READY-UI-246-R3

Status: **TECHNICAL PASS — USER FUNCTIONAL / VISUAL ACCEPT PENDING**

Reviewed source: `7048dde0365054b6319e1ce72b443acd121ff772..f15b37beedcb50bb3ff56b2c2813928c49ae9e45`  
Reviewed evidence envelope: `256565b406d7bd36515ce4e685a9f0b2e81c85f8`

## Decision

The reported `shipping.authoring.region_selection_overlap` gap is closed within the issued frontend scope.

- One pure authority classifies exact, ancestor and descendant collisions using server-proven `ResolvedAreaAncestry`; labels and prefixes are never authority.
- Country, province, city and service-area ancestry is enforced in both insertion orders across every proposed group.
- Sibling destinations on separate branches remain lawful.
- Missing ancestry fails closed.
- The destination modal refuses an invalid Add before `onAdd` or group mutation and keeps the staged path.
- The complete proposed tree is revalidated before Save; overlaps disable Save, identify the conflicting chips/groups and clear when either side is removed.
- The known server code maps to actionable Regions copy; raw technical code remains only in diagnostics.
- Backend exclusivity and payload/business rules are unchanged.
- Unsaved merchant edits were retained; the live exercise performed no Save/PUT and left draft revision 2 unchanged.

Focused R3 facts passed 11/11, the combined focused set passed 39/39, typecheck passed and the production build completed. Canonical FE3000 was rebuilt.

## Evidence note

The worker header names final Evidence-SHA `256565b...`, while `RECEIPTS.md` retains an interim evidence SHA `73b2eca...`. The review uses the final evidence envelope above. This is a documentation-envelope mismatch, not a source or runtime defect.

## Remaining user decision

R2/R3 is not visually accepted by inference. The user must review the canonical page and confirm that:

1. adding a country already covered by a child region, or a child under an already selected parent, is refused before Save;
2. the message names the conflict without exposing a raw code in the primary alert;
3. removing either conflicting destination immediately enables the lawful path;
4. the rebuilt compact layout is acceptable.

Canonical route:
`http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`

Active implementation task: **NONE**  
automaticNextImplementationTask: **NONE**
