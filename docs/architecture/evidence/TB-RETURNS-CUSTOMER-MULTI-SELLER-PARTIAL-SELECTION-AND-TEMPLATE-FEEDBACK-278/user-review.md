# User review — TB-278 (customer multi-seller partial selection + real template feedback)

Status: worker PASS within the issued 278 scope. **Not** user functional/visual acceptance. `279` is not implemented.

## What is ready to look at

1. **Customer order page — partial selection.**
   `/fa/customer-panel/orders/{checkoutId}` now offers «مرجوعی» from each seller order's own eligibility. A partially
   delivered seller order with one eligible line is actionable; a fully returned line is shown with its own blocker and
   a disabled checkbox.
2. **One shared modal, deliberate choices.** Open it and check that:
   - nothing is preselected, no quantity field exists until a line is ticked;
   - each line shows the real product, the unit («عدد» / «گرم») and «حداکثر قابل مرجوعی» from the server;
   - a whole-piece line refuses `0.5` locally with «این تعداد با واحد این قلم سازگار نیست» and never sends;
   - each line can carry its own reason, with an optional shared reason for the whole act;
   - items left unticked are absent from every request body.
3. **Two sellers in one checkout.** Selecting socks from seller A and a damaged item from seller B produces two separate
   child cases (two seller orders), not one mixed case, and the correct item stays untouched.
4. **Honest recovery.** If one child's answer is lost, the retained card shows «… ثبت‌شده» and «نتیجه نامشخص»; «ادامه»
   re-sends only the unresolved child with identical bytes; the recorded child is never re-sent.
5. **Template toast.** Command outcomes now appear as the application's existing `react-toastify` toast (top-right, RTL,
   colored, 3 s) — not the passive grey card the previous screenshots showed. Field-level problems stay inline next to
   the field instead of becoming toasts.
6. **Admin side unchanged in behaviour.** `/fa/admin/returns` commands now use the same template toast; approval stays a
   decision only and the first refund stays its own reviewed command (277 work preserved).

## What was deliberately not changed

- No live return case was created; both existing cases (`01a1e6b0-…`, `01a1e9c5-…`) remain read-only settled history.
- No approval/rejection reconsideration, no legacy multi-item partial authority, no per-delivery-slice allocation, no
  refund-amount policy — all 279.
- No schema change, no migration, no provider/PSP/Wallet/Inventory call.
- Seller-side Returns feedback: this app has no seller Returns mutation screen, so nothing was converted there.

## Known limits of this delivery

- The mounted modal suite runs in jsdom: it proves selection/validation/submission/recovery, not browser geometry, RTL
  pixel layout or the real customer session.
- The read-side eligibility marks a line with an unanswered owning precision as not selectable (conservative); the write
  path would still accept a whole quantity there.
- Two different reasons on the same line are two children sharing one server-side remaining ceiling; the server can
  lawfully refuse the second by capacity.
- If you want a live customer-session walkthrough, that needs a real customer login/checkout — not performed here.
