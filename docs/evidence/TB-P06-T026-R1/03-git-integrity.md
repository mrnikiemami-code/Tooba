# 03 — Git integrity

Task: TB-P06-T026-R1

Predecessor: `9ec56d15069cf080940829d93cbf01beef7d8091`

At claim: `HEAD == origin/main`, branch `main`, tracked tree clean (only unrelated untracked local logs/scripts).

After this Repair commit: require `HEAD == origin/main` and tracked clean (documented in RESULT).

`git diff --check`: clean (CRLF warnings only).
