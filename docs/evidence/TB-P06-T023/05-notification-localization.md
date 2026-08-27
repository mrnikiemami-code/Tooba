# 05 — Localization (TB-P06-T023)

Strategy: persist semantic `Type` + safe JSON payload; resolve title/body at read time via `NotificationCopy.Resolve(type, payloadJson, locale)`.

- Default locale for APIs: `fa`
- Query `?locale=en` supported
- No HTML stored
- Category derived at read (`order` for commerce events) for Shopeiva filter chips
