# Unit foundation

`catalog.units_of_measure` + `unit_of_measure_translations.language_id` → existing `localization.languages`.

Seeded: pcs / kg / g with FA+EN labels. No new language enum.

Product owns `UnitOfMeasureId`. Variant does not duplicate.
