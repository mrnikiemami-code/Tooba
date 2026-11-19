# RESULT — TB-282-R15

Status: **PASS**
Decision: `REJECTED_FETCH_TRANSPORT_UNCERTAIN_BULK_RESULT_DIALOG_AND_EXACT_RETRY_ZERO_BACKEND_ZERO_LIVE_WRITE`
NOT Architect ACCEPT, NOT owner acceptance.
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW

## Expected base
- Expected-Base-SHA: `8c6fe36325f181aba213fb655d735cc48c7384c7` (ancestor)
- Claim HEAD: `afb20cc8c7427efebbe6243db29e11f7bc3ac370`
- Bridge row: `89dc664c-952a-448b-9834-117b4ef4a139`

## Before / after
| Before | After |
| --- | --- |
| `sendJsonRequest`/`upload` `await fetch` outside try → rejected TypeError escapes batch; no result Dialog | Catch at wire boundary → `failed`/`story.transport.rejected` status 0; batch maps to `unknown`; Dialog shows all rows |
| R14 partial used empty 200 only | Real `TypeError("Failed to fetch")` on second selected row |

## Pre-fix observation
Mounted R15 rejection fact **failed** before patch (~630ms; result Dialog never appeared / wait aborted). Recorded `D:\temp` wake log + Architect defect description.

## Proof
- R15 mount **2/2**; R14 mount **4/4**; `tsc` 0
- Host5088 health 200; Next3000 next-dev stories 200
- ZERO backend / live Story-Media write

Code SHA: `877080f043de029d6cc9ffb0261702f7975d64b2`

User-Review-URLs: `/fa/admin/stories`, `/fa/vendor-panel/stories`, `/fa`

