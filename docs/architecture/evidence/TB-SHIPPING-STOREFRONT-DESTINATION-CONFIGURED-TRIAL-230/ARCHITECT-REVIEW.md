# Architect review — TB-230 — 2026-10-27

Reviewed main 537cfcd5ed26b16bb2880b8549f3af8d8ef6d98b, source diff since12fd91397001847cd8e606f6064f017afbbcdcb5.
Architect VIEWED committed1440 configured cards,390 configured cards and390 selected-date PNGs, read source
refresh/tax/geography fixes and RESULT. No local runtime/tests executed by Architect.

Verdict: POSITIVE CONFIGURED READ ACCEPTED within observed scope; not complete read-failure integrity,
Visual ACCEPT, checkout/selection/commit or Marketplace certification.
Images now show actual priced card/product lines, date chips, interval, second unassigned group and
read-only guard notice. 120000freight+5000packaging are from a real selected DEV snapshot; source money
formatter uses exact text. Tuesday2026-10-27 ->min2workingdays Thursday29, Friday30 absent as configured.
Uniform week means different weekday intervals were not live-proven; R1 harness covers independence.
Normal authenticated buyer read is established; no more generic auth/positive helper loop is needed.

Narrow fixes confirmed:
- Preview uses existing submit's IR-NAT constant, not cart Market IR. This is reuse of existing tax policy,
  not approval to hardcode a new country tax rule.
- Geography omits literal languageId=null; server installation default is used.
- Destination changes now refresh with verified IDs, request generations, preserve typed fields and hide
  old cards while pending; header no longer advertises legacy preparation as captured receiving promise.

SOURCE-CONFIRMED REMAINING DEFECT — FAILURE PATH:
refreshProjection stores previous projection until a new success. Its finally always clears pending for
the current request even when loadStorefrontCart/loadShippingProjection throws. The outer catch only sets
error; capturedPreviewForCards then reveals the OLD successful projection for the NEW destination.
appliedDestinationKey is advanced before success and no explicit same-key retry exists. Thus a transient
failure can restore stale price, then same destination is skipped. Existing four tests only exercise
pure generation/key primitives, not this current-failure transition.
230-R1 must close this specific failure/retry state before handing a trustworthy read UI to user.

Two scoped visual corrections visible in reviewed PNG:
IRR120000.00 currently prints fractional .00 though Rial UI requirement is integer; suppress all-zero fractional
display ONLY (raw wire unchanged); never round nonzero fraction. RTL interval reads reversed on screen;
render the existing interval inside bdi/dir=ltr without changing stored times.

Evidence limitation: RESULT says new draft capture/select created AND says the trial's only publication POST
was stale409 with state identical before/after. Preserve this chronology ambiguity; no new publication write
or atomic publication claim is certified by this read review. Actual immutable selected snapshot/state was
read and quoted; that is sufficient for this bounded read fact.
Known earlier decoder/cross-owner-label limits and missing Offer API proof remain, no blanket acceptance.
Task data: DEV draft01a17d0d-e033-7000-af43-49a1daa28822, post:standard,heavy; snapshot01a17d0e-0419-7000-b575-f507d2f89abc,
source2/selection1. User post/NULL draft untouched perworker. No order produced.
Source controls/recovery in230 are accepted as scoped evidence, not user Visual ACCEPT. Selection/commit
must stay not_ready until compact order evidence+revalidation parity is implemented and tested.
