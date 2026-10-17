LEGACY ROW PRESERVATION MATRIX — TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162

Seeded legacy coverage grants (pre-TB-161 shape, no option_code column existed at baseline):

id (suffix) | service     | country | area | city | service_area | active | stage
------------|-------------|---------|------|------|--------------|--------|------
1111        | tipax       | C       | -    | -    | -            | t      | baseline -> UP -> DOWN -> UP
2222        | tipax       | C       | A    | -    | -            | t      | baseline -> UP -> DOWN -> UP
3333        | tipax       | C       | A    | X    | -            | t      | baseline -> UP -> DOWN -> UP
4444        | tipax       | C       | A    | X    | SA           | f      | baseline -> UP -> DOWN -> UP
5555        | tooba-post  | C       | -    | -    | -            | t      | baseline -> UP -> DOWN -> UP

Observed per stage
------------------
BASELINE (after 5 pre-existing migrations, before TB-161)
  coverage_grants rows           : 5   (4 active)
  option_code column present     : NO (information_schema count = 0)
  shipping.method_coverages      : ABSENT (to_regclass = null)
  rate_policies rows             : 3

AFTER UP (20261016211322_OptionScopedCoverageRestriction)
  coverage_grants rows           : 5   (identical ids, scopes, activation)
  option_code on every legacy row: NULL  -> NO backfill to 'standard'/'express'
  rate_policies rows             : 3   (charges 185000 / 205000 / 260000 IRR unchanged)

AFTER DOWN (back to 20261015071836_ShippingPackagingPolicy, with the pinned Down fix)
  coverage_grants rows           : 5   (identical ids, scopes, activation)
  option_code column present     : NO
  shipping.method_coverages      : ABSENT
  ux_coverage_grants_service_scope restored (UNIQUE, NULLS NOT DISTINCT)
  rate_policies rows             : 7   (the 4 scenario rates added after Up remain; amounts unchanged)
  option-scoped grant rows       : DISCARDED (documented non-lossless downgrade of new-format data)

AFTER RE-APPLY (UP again)
  coverage_grants rows           : 5   (identical ids, scopes, activation, option_code = NULL)
  shipping.method_coverages      : PRESENT, 0 rows (declarations were discarded by the rollback)
  option-scoped grant rows       : 0 (discarded by the rollback; not recreated by Up)
  unique indexes rebuilt         : ux_coverage_grants_service_option_scope,
                                   ux_method_coverages_service_option (both NULLS NOT DISTINCT)

Verdict
-------
Legacy service-level rows are preserved with byte-identical identity, scope and activation across
Up -> Down -> Up. No silent widening (no promotion to service-level), no row loss among legacy rows, and
no monetary change. Option-scoped grants and declarations are the only data a rollback discards, which is
the expected and now explicitly coded downgrade semantics.
