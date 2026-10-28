PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — visible authenticated test handoff
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: OPEN_VISIBLE_NORMAL_BUYER_SESSION_ONLY_NO_IMPLEMENTATION
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## ONLY outcome — let the user enter without the OTP/phone wall

The user opened the plain3103URL in their own browser and asks what phone number to enter. R5 testbuyer is EMAIL-based, not a registeredphone account. Root acknowledges plainURL was insufficient handoff. User now authorizes making the task test session actually accessible. Focusbox5 minutes, not another development wave. No screenshots/visualreview/sourceedit/tests/build/schema/role/authpolicy/OTP change.

Existing FE http://127.0.0.1:3103 / Host5190 is running, Marketplace/tb239-marketplace/tooba_marketplace_tb239.
R5 normal buyer userId01a18247-54d3-7000-9f2f-219ac7de9e6d, taskprofileD:\temp\tb240r5-chrome/CDP9343.
Private randompassword was retained in taskTEMP only. Current unsubmittedreviewcart01a18251-2042-7000-8b37-bec94335eab5 belongs to this session. The target is a VISIBLE installedChrome window on http://127.0.0.1:3103/fa/shipping with real /api/auth/me200 and that known user session if available. Do not tell the user to guess a phone/OTP or provide a fake bypass.

## Execute, do not return another instruction for the user

1. Verify FE3103/Host5190 health without restarting. Resolve ONLY the task-owned R5 profile/process by exact user-data-dir/commandline, never enumerate/select arbitrary userbrowser/tabs. This task explicitly permits interacting with that TASKsession to complete the user's requested login handoff.
2. If that profile can open a headed window without closing/altering existing tabs or losing unsaved taskform state, open a new visible task-owned window/tab there using installedChrome, sameprofile, targetURL. A headlessCDPtarget or a plainURL alone is NOT visible handoff.
3. If R5 is running headless and cannot create a visible window, LEAVE it and its unsavedform alone. Start a separate new VISIBLE task-owned profile (e.g.D:\temp\tb240-login-handoff) and authenticate the SAME normalbuyer through EXISTING BFF GET/api/auth/csrf then POST/api/auth/login with correctdouble-submit header from thatprofile/apporigin; use its privately retained taskcredential. Search only known task-ownedTEMP/artifacts, not usercredentialstores or arbitrary userfiles. No rawcookie/JWT injection or devactor.
4. If that privatecredential truly cannot be recovered, do NOT reset an existingaccount or close itsbrowser. One NEW task-owned normalbuyer via publicregister + existingBFFlogin is expressly authorized as fallback; keep randompassword privately outsidegit. No admin/elevation. Report whether this is the samebuyer/knowncart or a freshbuyer/emptycart; never claim existingcart transferred when it didnot. Keep setup minimal, no newproducts/rates/seed. User can then shopnormally.
5. Verify actual /api/auth/me returns authenticatedtrue + same intendeduser, normalshippingroute is200 and no login redirect/phonewall. Leave a VISIBLE window at thatroute. Report exact visiblewindow/profile and actualusername/email (nonsecret) so the user can identify the rightwindow; NEVER output password/token/cookie/guestsecret. No screenshots.
6. Once visible window is handed off, do NOT enumerate/read/automate/reload/close it or itswatchedsource afterward. No tests or further taskcreation. Do not claim completehandoff if only a headlesspage or healthcheck exists.

NEVER touch USERoriginal3000/5088, handedoff3100/5188+tb238-handoff-chrome, R4profiletb240r4-review-chrome/9342, or arbitraryuserbrowser; no kill-by-name, no closing existingR5headlessif unsavedform may belost. No HMR/source modifications anywhere.
No backendrebuild/restart, accountreset, businessSQL/migration/reseed, authbypass or changes to production phone/OTP flow. This is ONLY normal visiblyloggedinbrowserdelivery, not a fix/certification of SMS/OTP feature.
Originaldeliverydeadline remains MISSED; no newpromise or erasedtimehistory. Sourcefunctionalcheckpoint stays accepted in previous scope, fullthreeownerUI/PSP/VisualACCEPT not newly claimed.

## Minimal evidence/recovery

Record sanitizedRESULT/RECEIPTS under docs/architecture/evidence/TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6/, receivedTASK verbatim there and docs/ai/tasks/TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6/TASK.md. Only docs allowed.
Update ALL3TOPs after execution with whether VISIBLE usable authenticatedwindow really exists, profile/URL, same/freshbuyer and cartstate, then ActiveNONE/automaticNextImplementationTaskNONE. Preserve priorhistory/no-screenshot/userwork rules.
Normalexactpathcommit/pushHEAD:main/fetchparity, no amend/force/reset/stash/rebase. No sending password/message/email/SMS. STOP_FOR_ARCHITECT, no automaticnewtask.

## RESULT CONTRACT — one canonical result, no probe

ActualFULLSHAs orNONE. Code-SHA NONE if no sourcechange. Appendtask-endmarker as final transmittedline.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — visible authenticated test handoff
Mode: OPEN_VISIBLE_NORMAL_BUYER_SESSION_ONLY_NO_IMPLEMENTATION
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actualfullissuecommit
Base-SHA: actualfullreceiptbase
Code-SHA: NONE
Evidence-SHA: actualfullevidencecommit
automaticNextImplementationTask: NONE

VISIBLEwindowprofile+URL, actualauthenticatedmeuser/email(no secrets), same/freshbuyer/cartstate,
no phonewall/headless-onlyclaim, realelapsed, preservedalluserwork, no codechange.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
