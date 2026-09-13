# R17 performance

- Order Detail: reservation included on existing order GET (`GetProjectionAsync` + `ListEventsAsync` + one `GetStatusAsync`).
- Orders/Payments grids: `GetProjectionsAsync` once per page. No per-row HTTP.
- Countdown is local (`remainingSecondsFromServer`). One refresh at 00:00. No polling loop.
