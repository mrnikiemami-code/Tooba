# TB-P10-T004-R4 — Manual Runtime

See `r4-runtime-raw.json` steps A/B/C.

- Manual evidence submit → Pending + evidenceSubmittedAt (AwaitingAdmin)
- Committed guest proof GET remains 200 after new empty Cart created
- New empty Cart secret cannot authorize old Payment
- Refresh with committed proof still authorized
- Admin confirm later → Succeeded on customer reopen
- No rapid Host 401 loop when ownership uses committed proof
