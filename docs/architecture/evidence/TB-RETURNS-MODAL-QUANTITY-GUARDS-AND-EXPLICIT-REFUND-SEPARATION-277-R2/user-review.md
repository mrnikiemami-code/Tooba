# TB-277-R2 — user review

## What changed for you as an operator

Nothing new to learn. The operator surface is the same review workspace with the same dialogs, the same unit-bounded
quantity fields and the same separation of «تأیید» from «اولین بازگشت وجه». One behaviour was made safer:

**If a receipt action's answer never arrives, you will no longer lose it because of a temporary look-up problem.**

Until now, if the server answered «دقت واحد این قلم قابل تأیید نبود», the page treated that as "the action definitely did
not happen" and dropped the saved request. But the server uses that same message for two different situations:

- the quantity you entered really does not fit the product's unit (e.g. `0.5` of a counted piece), **or**
- a *separate* lookup could not read the unit data at that moment.

In the second case the earlier attempt may still have been recorded, and the page had thrown away the only proof of it.
Now:

- a possibly-sent receipt stays on the page as «یک اقدام ناتمام برای این پرونده ثبت شده است» with the big «تکرار همان
  اقدام» button;
- retrying re-sends **exactly** the same reviewed request, so the server answers the occurrence it already stored instead
  of creating a second one;
- the card disappears only when the server gives a real answer for that same occurrence.

## How to check it (read-only)

Open the returns review page. The only live case is a **completed** one, so it offers no action to click:

http://127.0.0.1:3000/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490

Nothing was written to that case, no new case was created, and no refund/stock action was performed for this task.

## What to judge

- The uncertain-outcome card still reads clearly and the button is still reachable (min-height 44px).
- The refusal message for a genuinely wrong unit precision is still plain Persian and still appears in the dialog/notice
  (its wording is unchanged).
- Nothing else about receiving, approving, rejecting, the first refund, retry or physical settlement changed.

Not claimed: user visual acceptance, financial acceptance, or that the modal geometry was re-photographed — the review
was source, mounted (jsdom) and served-chunk evidence only.
