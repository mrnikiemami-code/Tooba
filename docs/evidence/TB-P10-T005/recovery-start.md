# TB-P10-T005 — Recovery start

| Field | Value |
| --- | --- |
| repo | D:\Users\User\source\repos\SarvNewVer |
| remote | https://github.com/mrnikiemami-code/Tooba |
| branch | main |
| HEAD | bda1041a25fa2a5cf607ed1e412a02f0a1ef2211 |
| origin/main | bda1041a25fa2a5cf607ed1e412a02f0a1ef2211 |
| HEAD == origin/main | yes |
| Bridge | http://127.0.0.1:17321 health ok |
| claimed | TB-P10-T005 / 2ec3e3ea-9126-478a-99c5-926cbafa3508 / tooba-main |
| expected previous HEAD | bda1041a25fa2a5cf607ed1e412a02f0a1ef2211 |
| tracked divergence | none |
| local uncommitted (not this Task) | login-return / account-menu UX leftovers — not staged |

`git status --short` at wake showed dirty login/account files and historical `.tmp-*` untracked files. Those are not T005 and were left untouched in the commit set.
