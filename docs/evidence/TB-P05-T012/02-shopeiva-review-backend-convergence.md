# Shopeiva review ↔ backend convergence

Primary Shopeiva sources:

- `productDetails/productDetails.jsx` rating strip;
- `productTabs/productTabs.jsx` review tab/count;
- `productReviews/reviewStats.jsx`;
- `productReviews/reviewItem.jsx`;
- `productReviews/reviewForm.jsx`;
- `productReviews/productReviews.jsx`;
- `vendor/panel/reviews/reviewsList.jsx` moderation visual reference.

| Shopeiva feature | Backend before T012 | Backend after T012 target | Live binding / decision |
| --- | --- | --- | --- |
| PDP rating strip | NOT AVAILABLE | Published aggregate | visible only when count > 0 |
| Review tab badge | NOT AVAILABLE | Published count | no fake count |
| Score + five stars | static sample | backend average | average remains backend authority |
| Distribution bars | client sample calculation | Published 1–5 counts | percentages are presentation over backend counts |
| Public review cards | `sampleReviews` | safe Published list | no private/internal identity |
| Verified badge | sample always true | Order proof snapshot | visible only when backend returns true |
| Review form | local toast | authenticated submit | successful submit remains Pending |
| Empty state | unavailable placeholder | real zero-review state | no AggregateRating |
| Product-card stars | fake defaults removed in T010 | batched real aggregate | only when count > 0 |
| Product JSON-LD | AggregateRating absent | conditional real aggregate | UI/SEO values must match |
| Vendor moderation list | fake local array | Admin queue/actions | accepted Admin shell + Data Grid, server authorization |

## Minimal UI additions

No new PDP tab or review visual language is introduced. Existing Shopeiva
structure is ported into the accepted Tooba PDP, using Persian text and Tooba
blue action tokens. Unsupported likes/helpful controls, avatars, review media,
and replies are omitted rather than simulated.

The Admin route is the only necessary surface addition because Shopeiva has no
Tooba Admin source. It follows the accepted Shopeiva Vendor + Tooba Data Grid
adaptation.

## Locked honesty

Never copy:

- Shopeiva `sampleReviews`;
- vendor `reviewsData`;
- ProductCard defaults `rating=4.5`, `reviews=120`;
- client-created `verified: true`;
- local average as business authority;
- AggregateRating before a Published backend aggregate exists.
