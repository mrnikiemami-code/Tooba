# TB-282-R11 CORRECTION ADDENDUM

Corrects R10 PARTIAL items from ARCHITECT-REVIEW:

1. AdminDelete replay sellerPartyId=null vs recorded shop → fixed via occurrence-ledger replay/finish.
2. Active→«در ویترین» when effectivelyPublic=false → removed; past-end windows → منقضی.
3. List delete action added; pending delete recovery closes detail route after successful replay.
4. Validator EndAt >= → >; requireEnd when «بدون پایان» unchecked.
5. R10 “mount.test” was date/copy only → R11 real mounts for delete/preview/media recovery.
6. Next Html/404: diagnosed (compile OK; prerender /404 Document Html) — not hand-waved; next-dev served.
