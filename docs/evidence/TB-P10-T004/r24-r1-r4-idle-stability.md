# TB-P10-T004-R24-R1-R4 — Idle stability

Five-second idle windows (enough to catch 1s–3s refetch loops):

| State | auth/me | cart/current | merge | auth/me 401 |
| --- | --- | --- | --- | --- |
| authenticated Home | 6→6 | 7→7 | 1→1 | 4→4 |
| authenticated Shipping | 8→8 | 14→14 | 1→1 | 4→4 |
| anonymous after logout | 8→8 | 14→14 | 1→1 | 4→4 |
| Payment after COMMIT | 15→15 | 28→28 | 2→2 | 8→8 |

No continuous growth. No polling workaround.
