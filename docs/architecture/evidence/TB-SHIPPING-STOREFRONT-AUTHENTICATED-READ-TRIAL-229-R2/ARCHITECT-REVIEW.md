# Architect review — TB-229-R2 — 2026-10-27

Reviewed main 12fd91397001847cd8e606f6064f017afbbcdcb5; docs/evidence-only result after R1 implementation.
Verdict: normal buyer authentication + live200/unavailable API observation ACCEPTED within reported limits.
Configured-method price/calendar observation and card VISUAL review are NOT accepted yet. No parity task.

Architect read RESULT and source refresh wiring, and VIEWED the committed1440x900 two-line and390x844
two-line PNGs. Both show HERO/address form, not the captured group cards; therefore they are not visual
evidence of the cards' layout. Desktop top also visibly says3 minimum delivery days from legacy preparation.
Worker DOM observations for two/single cards and no overflow are reported, not independently rerun.
The positive99/198 money/calendar remains R1 harness proof only.

Authentication diagnosis is supported by reported correlated trace: middleware catches downstream
SemanticException and logs it as Commerce resolution failure even after tenant resolution. Legitimate normal
register/BFFpassword login/me succeeded. No new auth source/bypass required. Task-owned account/cart and
previewON runtime exist; reuse them rather than repeat account setup or write another CDP driver.
R2 took105min vs20; no process acceptance.

SOURCE-CONFIRMED OPEN CONNECTION DEFECT (current storefront-shipping.tsx):
Initial refreshProjection() passes no destination. Province callback also calls refreshProjection without
the fourth destination argument. destinationIdentifiers is derived after Geography reads; no effect refreshes
captured preview when that identity changes. Only the legacy onSelectMethod path passes destinationIdentifiers,
but captured mode suppresses that legacy method selector. Therefore filling a destination can leave the new
preview permanently destination_required. Correct this actual UI path before claiming a configured trial.
Header shippingDaysLabel still uses maxSellerPreparationDays+legacy lead days even in captured mode; suppress
that legacy promise rather than representing it as the captured method's customer receiving minimum.

230 is narrowly: wire current verified destination changes, suppress legacy header promise, configure ONE
task-owned DEV manual method through normal APIs if no usable existing positive method, observe one priced
group alongside an independent unavailable group, screenshots scrolled to ACTUAL group cards. No order writes.
User draft/rates/selected pointer must remain untouched. No inferred physical facts. Compact order evidence
and selection/commit remain mandatory/unimplemented. Marketplace and known R1 decoder/label-map limits
remain open; SingleStore unavailable rendering is not Marketplace or complete shipping certification.
