# Recovery start
- branch: main
- HEAD: f58744c51485a667f222d48be70337de7b7cc935
- origin/main: f58744c51485a667f222d48be70337de7b7cc935
- HEAD == origin/main: True
- expected tip ancestor: f58744c51485a667f222d48be70337de7b7cc935
- 18ca10c9 ancestor: verified via prior pipeline
- git status: untracked .tmp* and user work preserved; no destructive git
- Frontend production: untouched (ARCH-FE-FREEZE-001)
- TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
