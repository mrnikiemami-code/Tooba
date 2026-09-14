# TB-P10-T017-R1 — Anonymous `/api/auth/me`

Capture (`r1-runtime-report.json`): 7 requests, all 401.

Routes: Home, PLP, PDP, Landing, Cart, plus DarkOnly PDP/Landing. One anonymous session resolution per navigation (T004-R24-R1-R4). Not a retry storm. Console “Failed to load resource: 401” is the same expected anonymous `/api/auth/me`.
