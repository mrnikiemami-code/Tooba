# R1 guest/customer runtime — TB-P09-T021-R1

Script: `docs/evidence/TB-P09-T021/_r1_runtime.mjs`  
Raw: `docs/evidence/TB-P09-T021/r1-runtime-raw.json`

## Access mechanisms proven

| Case | Mechanism | Expect |
| --- | --- | --- |
| Guest | `X-Tooba-Guest-Secret` only | 200 + central preferred |
| Guest actor seam | `X-Tooba-Dev-Actor-User-Id: StorefrontGuestActorId` | 200 |
| Owned | Dev actor matching placement | 200 + central preferred |
| Invalid actor | unmatched Dev actor | 404 |
| Invalid secret | wrong guest secret | 404 |
| No package | guest secret on independent order | 200, preferred null |

## Lifecycle checks

- Cancel+rebuild → preferred package number is the new active package
- Dispatch → `preferredCustomerPackageStatus=Dispatched`
- Deliver → status Delivered + member fulfillments Delivered + central tracking primary
