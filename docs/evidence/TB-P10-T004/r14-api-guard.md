# TB-P10-T004-R14 — API guard

New initiation (new idempotency key) after Succeeded:

- 409 `payment.already_succeeded`
- detail: پرداخت این سفارش قبلاً با موفقیت انجام شده است.
- no new Payment or Attempt row

Same-key replay of the Succeeded Payment: 200 existing ids, no new attempt.

Composer also checks checkout Paid **or** HasSucceeded before calling directory.
