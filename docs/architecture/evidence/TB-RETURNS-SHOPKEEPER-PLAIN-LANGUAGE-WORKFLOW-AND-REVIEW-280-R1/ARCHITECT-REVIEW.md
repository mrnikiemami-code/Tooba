# Architect review — TB-280-R1

Date: 2026-11-19 (Asia/Tehran)
Verdict: ACCEPT within the issued UX1/UX2/UX3 frontend closure. READY FOR OWNER REVIEW; user visual/functional acceptance NOT CLAIMED.
Reviewed head: bb03fbd54416e8c4a436ae40a176b5d715649b11.
Implementation: 882ecc19b2100175ce6cc9325767f5b9f9421081; evidence: a28b98fc1fd4ef4c60b0f918b23e7f3bc5b9e21f.
Issued base: 4ce50d00690f3abdb058289f4a82d63275d136fb (docs-only issuance on expected 9075e2fa4325a2de60cbd17ebfe908d9d9ee74ed).

## Independent review basis

Reviewed the actual GitHub compare, complete changed workspace and new mounted test source, retained test diffs, proof map and Persian user handoff. main parity with the worker's final head was verified before this docs-only review commit. Architect did NOT execute the Windows tests/builds, access the localhost browser/database, or perform a live business action. Worker-reported 7/7 new scenarios, 34/34 mounted total, 49/49 unit tests, tsc/build success and served BUILD_ID xUhUnq_b5Okl6j0RYf2Jl are worker receipts, assessed against actual implementation/assertions rather than independently rerun.

## Findings closed

| Parent finding | Source correction | Counterexample assessed | Verdict |
| --- | --- | --- | --- |
| UX1 requested money shown during approved/bound-money confirmation | firstRefundReviewMoney uses explicit null checks on approvedAmount (zero retained), otherwise only the existing reviewed-plan money fact; unavailable review disables confirm and handler refuses. boundAttemptReviewMoney selects exactly one attempt with the server-published id. Start/resume/retry dialogs and their handlers share these display gates. | M1 requested3000/approved2000, approved0, missing amount refuses; M2 pending attempt1500 instead of requested3000, mismatched failed id unavailable and zero POST. Retained approval-vs-start test remains. | CLOSED within existing owning read/action contract |
| UX2 aggregate accepted assigned to usable; zero-ceiling condition fields and immediate errors | beginResolution reads acceptedUsable/acceptedDamaged separately. showUsable/showDamaged render positive-condition ceilings only. Zero-received sibling shows a notice plus meaningful required reason; quantities remain accounted in the five-field body. Existing exact quantity authority and owning write rules retained. | M3 genuine per-condition implicit damaged-only baseline opens clean, usable input absent, acceptedDamaged1 body; M4 mixed fractional baseline clean, Persian over-ceiling refuses, lawful1.5/1.25 body has exact complements; M5 zero-received sibling remains rejectedUnreceived1 with operator reason and no fake inputs. | CLOSED |
| UX3 headline sums pieces and grams | hasAnyReceivedGoods is a boolean presence test; describeReceivedGoodsSummary displays each product's received/requested amount and own unit. No sumUnitsOf operator aggregate remains. | M6 pcs1 and grams500 show each product and own unit, no501 summary; M7 zero-received primary and settled read-only remain. | CLOSED |

Retained test changes were inspected: the 277 first-refund fixture now supplies stored approvedAmount (assertions retained); the 279 local over-ceiling copy expectation follows the short merchant wording while keeping the zero-POST assertion. No owning backend, schema, transport route/body, refusal token set or retained-envelope implementation changed in this delta. The new handlers check review availability before constructing/sending an intent. No client money field was added. Original version/identity binding and server action authority remain in place.

## Honest boundaries

The accepted result closes these THREE concrete product findings; it is not all Marketplace/Returns recertification or proof of real PSP/Wallet/Inventory effects. The live case still has received0, so positive-money and mixed-condition dialogs have mounted real-component proof with controlled HTTP/storage, not a live saved-data walkthrough. jsdom proves behaviour and transport, not pixels. Worker reports read-only DOM/text/layout checks at390/1440 and zero live POST; Architect did not repeat them. The owner must judge actual readability and complete the intended manual workflow.

The new test's positive resume and mismatched retry examples share the same selection helper; these are not a claim that every malformed server response has been exhaustively tested. Server-published capabilities and accepted owning guards remain the authority; no new financial algorithm is certified here.

279-R3 fixed-scope ACCEPT remains unchanged. Parent280 PARTIAL evidence remains historical and is resolved for UX1/UX2/UX3 by this repair; do not rewrite old reports. No further implementation task is issued or recommended for these closed findings. automaticNextImplementationTask NONE. Key state unchanged and separate; no key operation or live business write is authorized by this review.

## Owner handoff

Review URL: http://127.0.0.1:3000/fa/admin/returns/01a1eefb-ea96-7000-838d-9a0b37661c8b
Queue: http://127.0.0.1:3000/fa/admin/returns

برای تست صاحب فروشگاه: ابتدا دریافت واقعی کالا، سپس بررسی مقدار سالم/معیوب و دلیل، بعد تأیید مرجوعی؛ تأیید نباید پول جابه‌جا کند. در مرحلهٔ بازگشت وجه، مبلغ تأییدشده را ببینید. اگر بخشی رد شده، تحویل آن به مشتری جدا ثبت می‌شود. بازکردن و انصراف چیزی ثبت نمی‌کند؛ دکمهٔ نهایی ثبت/بازگشت وجه اقدام واقعی است. User acceptance remains pending until the owner reports the outcome.
