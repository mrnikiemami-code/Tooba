TB-SHIPPING-OWNER-STRICT-OPTION-RATE-160

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-OWNER-STRICT-OPTION-RATE-160
Parent-Task: TB-SHIPPING-STOREFRONT-RATE-AUTHORITY-159
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: cff0f19d36ab73870345a3f542f01c459894a1a6
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_SHIPPING_OWNER_STRICT_OPTION_QUOTE_SEMANTICS
Timebox: HARD 45 minutes; stop honestly at limit
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

Objective

Repair the Shipping-owned pricing authority contradiction identified in TB-159. Add an explicit opt-in strict option identity request mode to the existing Shipping rate quote contract/engine, so a quote for tipax:standard can NEVER consume a provider-level tipax / OptionCode=null rate or tipax:express rate. Keep current legacy/default quote semantics byte-compatible when strict mode is not requested. This task ONLY repairs owner semantics and proves them; do NOT yet bind Order/storefront to enabled authority.

Canonical decisions and required reading
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md
docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md including locked addendum: one independent financial authority per canonical method; provider-level legacy rows are LegacySharedUnresolved, never implicit method price.
docs/architecture/evidence/TB-SHIPPING-STOREFRONT-RATE-AUTHORITY-159/OWNER-AUTHORITY-BLOCKER-159.md and RESULT.md.
Actual source ShippingRatePolicy.MatchesOption, ShippingRatePolicyEngine.Quote, IShippingRateQuoteLookup, its request/response contracts, existing implementation and tests, and ShippingMethodFinancialStatusEvaluator.
Authorized scope
In Shipping.Contracts introduce an additive, clearly named opt-in flag / strategy for strict method-option matching on the EXISTING quote request (default legacy). Avoid breaking existing callers or serializations.
In Shipping.Application/Domain owner engine, when strict is requested AND a concrete option is supplied: only an owned row whose service AND normalized nonempty option match exactly may qualify. No service-level null-option fallback, no sibling option fallback, no stale/disabled price, no first-match ambiguous price. Cover eligibility/coverage lookup semantics, preventing any coverage belonging to a sibling/provider row from accidentally legitimizing a method price; examine actual coverage ownership and report honest limits.
Strict failures return the existing typed fail-closed result (NotCovered/NoMatchingRate or appropriate established code), NEVER 0/free/default options. Do not invent conditional measurement or a quote when the authoritative inputs are absent. No Results.Json, no Host decision authority, no direct cross-module implementation reference; Contracts only.
Keep optionless methods and all legacy/default requests identical in outcomes. If strict eligibility needs more than a Boolean, use a minimal unambiguous contract owned by Shipping, not an Order-side workaround.
Add focused owner tests with real evaluator/engine and meaningful fixture rows: (a) provider-level only strict tipax:standard fails; (b) tipax:express-owned cannot price tipax:standard; (c) standard-owned works for standard and cannot price express; (d) mixed shared+owned chooses strictly owned; (e) strict without owned coverage fails closed; (f) conditioned rate without measurements fails closed; (g) optionless/legacy behavior unchanged; (h) known ambiguity/conflict handling. Ensure explicit strictness cannot silently downgrade.
Run relevant focused Shipping tests and Shipping build, plus lightweight compilation/test of downstream consumer contracts. Distinguish pre-existing reds with provenance. No broad slow suites.
Commit scoped source/tests and concise evidence under docs/architecture/evidence/TB-SHIPPING-OWNER-STRICT-OPTION-RATE-160/, fast-forward push origin/main, state exact code/evidence SHA, HEAD parity, tests, and remaining coverage/measurement prerequisites.
Explicit exclusions / safety
No Order quote path wiring, no storefront pricing change, no feature gate enablement, no admin redesign, no fixed/free/COD/provider quote invented, no shipping tax/settlement share/PSP changes.
No schema migration unless a real strictness invariant is impossible without one; then STOP/PARTIAL with proof rather than improvise.
No changes to shared :3000, :5088, :5432, tooba_alpha, store-alpha or running apps/containers; no restarts, destructive commands, unapproved stashes, reset, rebase, force-push, or unrelated dirty path edits.
Case D origin remains blocked. Keep Shipping as authority, Admin provider isolation intact, and legacy compatibility verified.
PASS criteria

Strict mode cannot borrow provider-level or sibling rates; strict-owned quote requires genuine eligible owned rows and coverage; missing measurement fails closed; legacy mode remains compatible; tests/build green in scope; evidence and pushed HEAD confirmed. If any proof missing, report PARTIAL/BLOCKED rather than PASS. STOP_FOR_ARCHITECT; NEVER automatically begin task 161.
END_TOOBA_TASK
