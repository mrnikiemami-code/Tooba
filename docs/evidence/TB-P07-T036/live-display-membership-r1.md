# Live Display Membership (TB-P07-T036-R1)

Source: `live-r1-proof.json` section B (**8/8 pass**)

## Sequence
1. Temp product with Primary elsewhere.
2. `POST …/categories/additional` for target L3 (گوشی هوشمند) → display-only.
3. Primary unchanged after add.
4. `DELETE …/categories/additional/{categoryId}` → remove.
5. Primary still unchanged.
6. Delete temp product.

Proves Category→Products semantics: Additional only; never sets Primary.
