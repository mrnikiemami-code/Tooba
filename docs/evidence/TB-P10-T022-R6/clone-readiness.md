# Clone-Readiness — TB-P10-T022-R6

Cloning not implemented.

Every Product/Category/Brand operational table now has a Template counterpart with matching fields/relations (see exact-parity-matrix.md).

Clone mapping:

- drop TemplateId on roots
- remap PKs/FKs (Template ids → new operational ids)
- copy translations/media references/history/assignments/axes/values/mega-menu identically
- TemplateAttribute*/TemplateTag remain template-scoped and map 1:1 into operational attribute_definitions/tags (or reuse shared catalog rows by code)

No schema-shape blocker remains for future cloning inside Product/Category/Brand families.
