# Public projection

`GET /v1/storefront/pages/{slug}` on Published pages includes enabled sections only, SortOrder then PageSectionId.

Each section: pageSectionId, sectionType, sortOrder, typed config, ProductCollection `items` (id/slug).

Draft remains 404. No section renderer/builder on FE.
