# TB-282-R16 COMPLETION LEDGER

| Gate | Method | Observed |
| --- | --- | --- |
| Pre-fix fail | mounted R16 | ✖ ~2400ms / unstable typing path |
| Stabilize callbacks | useCallback + refs | queryAdapter deps mode/reload/filter only |
| Typing no refetch | mounted char-by-char | queryReads flat; selection/focus hold |
| Cancel then confirm | mounted 2+1 | 0 reject on cancel; 2 eligible POST; skipped never sent |
| Filter still fetches | review select change | queryReads increments |
| Served controlled | next-dev + fetch wrap | baseline=afterType=1; sel ۳; rejectPosts 0 |
| Zero backend | — | FE-only; live writes blocked in served control |
