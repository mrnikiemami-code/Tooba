# Error-semantics audit
No `PlatformHttpException` or exception-message classification was added to Order. `DeleteNoteAsync` still catches all `InvalidOperationException` and maps them to forbidden; typed delete outcomes and unknown-exception propagation remain incomplete. The unused `now`/clock seam was removed.
