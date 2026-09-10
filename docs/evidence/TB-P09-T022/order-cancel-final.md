# Order cancel final — TB-P09-T022

Pre-dispatch active package:

- Whole-order `cancel` voids active Consolidated Package first (`VoidActivePackagesForCheckoutCancelAsync`)
- Membership locks released; existing T016 pre-dispatch shipment cleanup / inventory release continues
- Payment/refund via existing flow

Post-central-dispatch:

- Whole-order cancel remains blocked
- No regression to T020-R2 restore/reservation rebinding expected
