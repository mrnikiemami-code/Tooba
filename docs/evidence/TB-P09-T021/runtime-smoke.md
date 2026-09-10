# Runtime smoke — TB-P09-T021

Script: `docs/evidence/TB-P09-T021/_runtime.mjs`  
Raw: `docs/evidence/TB-P09-T021/runtime-raw.json`

## Results

| Scenario | Result |
| --- | --- |
| A Create multi-seller package | PASS — `MP-01A0893AFE80` on checkout `01a0893a-f729-7000-a63c-954f02ddbfe5` |
| Member lock (direct dispatch) | PASS — HTTP 400 (`order.operation.invalid` after capability suppress) |
| B Rebuild cancel+create | PASS — new package `01a0893b-0104-7000-a6c3-7a83014d42a1`, old Cancelled |
| C Central dispatch | PASS — package + both member shipments Dispatched; whole-order cancel blocked |
| D Central deliver | PASS — package + members Delivered; central tracking `CENTRAL-T021-A2` |
| E Independent no-package flow | PASS — checkout `01a0893b-04e8-7000-9dc6-6b8573e09c44` direct dispatch 200, packages=0 |

Customer fulfillments endpoint returned 404 for guest-created checkout (no customer actor ownership); package tracking preference remains recorded on package and Admin projection.
