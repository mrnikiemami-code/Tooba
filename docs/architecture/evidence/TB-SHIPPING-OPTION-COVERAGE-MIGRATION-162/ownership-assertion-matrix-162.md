OWNERSHIP / STRICT-vs-LEGACY ASSERTION MATRIX — TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162

Executed against the REAL migrated scratch PostgreSQL through the owner's own code
(Tooba.Shipping.Infrastructure/ShippingRateQuoteLookup -> Tooba.Shipping.Domain/ShippingRatePolicyEngine).
Source: owner-code-quote-proof-162.txt — 16 assertions, 16 PASS, 0 FAIL
(HARNESS_FAILURES=0). This is a database + owner-code proof, not a unit-test claim.

Fixture on the upgraded schema
-----------------------------
tipax        : rate NULL=185000 (provider), standard=205000, express=260000
               declaration standard=Restricted(1), express=Unrestricted(0)
               grant: legacy service-level rows (country, area, cityX, cityX/service-area inactive)
                      + option-scoped grant standard @ cityX
strictonly   : rate standard=170000; declaration standard=Restricted; ZERO own grants
tpost        : rate standard=150000; NO declaration
legacyonly   : rate NULL=100000; only an option-scoped grant (standard @ cityX)
cond         : rate standard=175000 conditioned weight [1000,2000); NO declaration

Assertion matrix
----------------
# | assertion                                                              | mode         | expected                                      | actual                                        | result
--|------------------------------------------------------------------------|--------------|-----------------------------------------------|-----------------------------------------------|-------
1 | restricted standard allowed in its own city X                          | StrictOwned  | Quoted, quoted, 205000 IRR                    | Quoted / shipping.quote.quoted / 205000 / IRR | PASS
2 | restricted standard denied in city Y                                   | StrictOwned  | NotCovered, method_restricted, 0, null         | NotCovered / shipping.coverage.method_restricted / 0 / null | PASS
3 | unrestricted express quoted in city Y (sibling restriction ignored)     | StrictOwned  | Quoted, 260000                                | Quoted / 260000 / IRR                         | PASS
4 | unrestricted express quoted in city X                                   | StrictOwned  | Quoted, 260000                                | Quoted / 260000 / IRR                         | PASS
5 | provider-level legacy grants do not authorize the restricted method     | StrictOwned  | NotCovered, method_restricted                  | NotCovered / method_restricted                | PASS
6 | restricted with ZERO own grants denies                                  | StrictOwned  | NotCovered, method_restricted                  | NotCovered / method_restricted                | PASS
7 | absent declaration is unrestricted and priced (owned rate present)      | StrictOwned  | Quoted, 150000                                | Quoted / 150000 / IRR                         | PASS
8 | absent declaration, no rate at all                                      | StrictOwned  | fail closed, service.not_priced, 0, null       | UnknownService / shipping.service.not_priced  | PASS
9 | conditioned rate without trusted measurement                            | StrictOwned  | MeasurementRequired, 0, null                   | MeasurementRequired / measurement_required    | PASS
10| conditioned rate with weight 1500 inside its tier                       | StrictOwned  | Quoted, 175000                                | Quoted / 175000 / IRR                         | PASS
11| legacy fallback still requires a service-level grant                    | LegacyFallback | NotCovered, coverage.missing                 | NotCovered / shipping.coverage.missing        | PASS
12| legacy fallback prefers the option rate, declaration ignored            | LegacyFallback | Quoted, 205000                              | Quoted / 205000 / IRR                         | PASS
13| legacy fallback uses the express rate, declaration ignored              | LegacyFallback | Quoted, 260000                              | Quoted / 260000 / IRR                         | PASS
14| legacy fallback still leaks the provider rate to an unpriced option     | LegacyFallback | Quoted, 185000 (documented TB-159 leak)      | Quoted / 185000 / IRR                         | PASS
15| strict refuses that same provider rate for the unpriced option          | StrictOwned  | UnknownService, owned_option_not_priced, 0     | UnknownService / owned_option_not_priced / 0  | PASS
16| strict refuses provider rate for a service with only a NULL-level rate  | StrictOwned  | UnknownService, owned_option_not_priced, 0     | UnknownService / owned_option_not_priced / 0  | PASS

Notes
-----
- Every denial returns amount 0 with a NULL currency, i.e. "not quoted" is never encoded as "free".
- The only quoted-zero path remains ShippingReasonCodes.QuotedFreeAboveThreshold (not exercised here
  because no free-above threshold was seeded); zeros above are all NotQuoted outcomes.
- Mode column values verified directly in the database: standard=1 (Restricted), express=0 (Unrestricted).
- Earlier harness drafts contained three expectations that were wrong about the product, not defects:
  (a) a service with no rate rows at all answers service.not_priced (not owned_option_not_priced),
  (b) legacy fallback prefers an existing option-specific rate over the provider rate,
  (c) legacy fallback therefore shows 205000/260000 for tipax rather than the provider rate.
  Those expectations were corrected, and the genuine provider-rate leak is asserted separately (#14/#15).
