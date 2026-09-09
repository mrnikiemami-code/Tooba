# R1 snapshot immutability

Existing `"order".order_lines` (qty 1/2/3, places 0, no unit snapshot) captured before policy change.

Then GlobalRoundingMode=Ceiling and Product Step 0.25→0.50. Same five historical rows unchanged.

NEW cart after change: 1.37 → 1.50 (Ceiling + Step 0.50).

Focused test: `Historical_order_line_snapshots_stay_fixed_when_later_policy_normalizes_differently` — OrderLine 1.25 / places 2 / step 0.25 stays while later policy normalizes 1.37→1.50.

No historical rewrite.
