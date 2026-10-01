# localization — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1

## FoundationErrorResourceSet

Owns: `validation.*`, `platform.*`, **and** `admin.*`.

Missing resource values still fall through to other `IErrorResourceSet` / contributors (Owns without value does not hard-fail).

## Resources

EN: `FoundationErrors.resx` — five admin.* keys  
FA: `FoundationErrors.fa.resx` — five admin.* keys  

Keys: actor.missing, tenant.missing, authorization.unavailable, authorization.denied, dev.unavailable.

Titles resolve to localized prose, not machine codes / SafeTitleFallback-only when resources exist.
