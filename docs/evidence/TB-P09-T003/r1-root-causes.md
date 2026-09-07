# TB-P09-T003-R1 root causes

1. **Operations menu empty/useless in grid**: absolute dropdown clipped by AG Grid / overflow ancestors → portal + fixed positioning.
2. **Restrictive column maxWidth** on Orders `orderColumns` blocked manual enlarge.
3. **Human labels**: retry_refund / shipment FA labels used technical wording (`refund`, محموله).
4. **Empty ops on terminal rows**: legitimate (Delivered+returned); actionable ReadyToFulfill rows had ops once menu visible.
5. **Note delete rule missing**: no soft-delete / view-ack previously.
6. **List return/refund visibility**: `SellerOrderStatus` has no Returned; list Status cannot show مرجوعی without new DTO/column (Architect decision — not auto-added).
7. **Manual/card-to-card Admin confirm**: not modeled in Payment Admin ops — gap documented, not faked.
