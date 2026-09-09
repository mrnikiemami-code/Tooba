# Discovery — TB-P09-T019

Existing `/admin/returns` + `POST /v1/admin/returns/query` reused T001/T005/T006/T007 Return/Refund domains.

Admin grid defects: GUID columns, mixed Return/Refund status, search by GUID, nav «بازپرداخت», English Bad Request, no LOCK-RET.

Already correct: eligibility evaluator, OrderLine snapshot, split-delivery clocks, approve/reject/retry ops, T016 cancel refund, settlement AdjustFromRefund.

Out of scope: Orders redesign, second Return/Refund subsystem, Received lifecycle (does not exist), USER visual acceptance, TB-P09-T020.
