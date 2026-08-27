# 08 — Story capability matrix (TB-P06-T019-R1)

| Capability | Admin | Seller |
|---|---|---|
| List | All authorized | Own only |
| Create | Yes | Yes (Draft) |
| Edit Draft / Rejected | Yes | Own only |
| Manage items / media / CTA | Yes | Own (editable states) |
| Submit for review | Not needed | Yes (None / Rejected) |
| Approve | Yes | **No** |
| Reject (reason required) | Yes | **No** |
| Publish / Activate | Yes (after eligibility) | **No** |
| Schedule | Yes (after Approved for seller-origin) | **No** |
| Disable | Yes | **No** (UI + no seller route) |
| View origin / seller owner | Yes | Implicit own (columns hidden) |
| Change SellerPartyId | N/A (admin creates admin-origin) | **No** |

Source: `story-capabilities.ts` + backend directory/endpoints.
