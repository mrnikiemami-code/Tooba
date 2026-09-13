# R20-R1 runtime

| Check | Result |
| --- | --- |
| A FA Cart manual AwaitingAdmin long countdown | PASS `23:47:20` + review copy; no Pay; no `1439` |
| B Admin Order Detail manual-review countdown | PASS `23:46:49` on `01a098ef-2d53-7000-aec6-5d95e6c9e4fe` |
| C Admin operational history FA | PASS `ردشده توسط درگاه پرداخت`; no raw `GATEWAY_REJECTED` |
| D EN smoke | PASS cart `23:46:36` + EN aria-label; Admin `Rejected by payment gateway` |
| E Network | PASS `pending-payments` resource count 1 on cart load; countdown local |

Runtime kept: Host `:5088`, FE `:3000`.
USER_VISUAL_ACCEPTED=NO
