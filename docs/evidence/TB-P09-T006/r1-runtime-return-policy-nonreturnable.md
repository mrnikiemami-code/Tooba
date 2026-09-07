# R1 Runtime — Non-Returnable Offer

- Offer `01a0429e-7f0a-7000-9301-dbffbb89713e` → NonReturnable
- Fresh checkout `01a07bd0-4244-7000-bc96-abc25c9c5f29`
- OrderLine `01a07bd0-4259-7000-b2f5-f8a22ed1c4fa`
- Snapshot: `isReturnable=false`, `returnWindowDays=0`, label/deadline `غیرقابل مرجوعی`, `returnStatusCode=non_returnable`
- Defect fixed in R1: EF `HasDefaultValue(7)` was treating WindowDays=0 as unset; `HasSentinel(-1)` persists 0 for NonReturnable
- Normal UI uses FA `غیرقابل مرجوعی` (not raw `NonReturnable`)
