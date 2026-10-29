# USER REGRESSION AFTER ARCHITECT ACCEPTANCE — 2026-10-29

The user reproduced a remaining practical defect on canonical shipping: choosing province after selecting delivery day/hour makes the page appear to refresh and loses both choices.

Source-confirmed causes:

1. the province handler directly calls `refreshProjection`;
2. the destination coordinator issues another tracked refresh for the same state transition;
3. destination-key changes unconditionally clear all choices before the authoritative response can reconcile them;
4. pending cards disappear, presenting the double re-render as a full-page reload.

The prior acceptance is reopened only for this user-reproduced destination transition. `TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3` is issued. Accepted navigation latency/native-click/timeout behavior remains closed.
