# TB-P04-T004 — Product Workspace blueprint

Not implemented as a screen. Composition target for a later envelope.

| Section | User goal | Source domains | Editing | Cross-domain | Permission | Mobile | Loading/error/conflict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Core | Identify the sellable product | Catalog | Inline section save | Feeds variants/SEO | view/edit catalog | stacked fields | section spinner; concurrency on sku |
| Category / Brand | Place in taxonomy | Catalog | Select + confirm | Affects attributes | catalog classify | picker sheet | stale taxonomy reload |
| Attributes | Capture typed facts | Catalog | Per-attribute edit | Variant matrix | catalog edit | accordion | validation per field |
| Variants | Offer sellable SKUs | Catalog | Row inspector | Offer/inventory | catalog edit | cards | conflict on sku uniqueness |
| Media | Show the product | Content/media | Upload/reorder | Publication | content edit | gallery | upload failure retry |
| Offer / Seller | Who sells it | Offer / Party | Contextual, not a Party CRUD | Pricing/inventory | offer edit | summary card | seller status warning |
| Pricing | Show money | Pricing | Money editors | Tax/promo snapshot | pricing edit | stacked money | stale price reload |
| Tax | Classify tax | Tax | Select class | Pricing | tax assign | select | jurisdiction change conflict |
| Inventory | Availability | Inventory | Adjust via commands | Offer | inventory execute | counts | reservation conflict |
| SEO | Discoverability | Content/SEO | Fields | Publication | seo edit | stacked | slug conflict |
| Content | Long copy | Content | Editor | Publication | content edit | editor sheet | draft vs live conflict |
| Publication | Make visible | Catalog/content | Primary command | All sections | publish execute | sticky command | confirm + failure |
| Audit | What changed | Audit | Read-only | All | audit view | timeline | empty vs error |

Product Workspace must compose these sections in one shell. It must not become one menu item per module.
