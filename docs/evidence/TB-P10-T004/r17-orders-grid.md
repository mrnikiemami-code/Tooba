# R17 Orders grid

Column «رزرو موجودی» with compact labels: فعال #n / پایان‌یافته #n / نهایی‌شده / آزادشده #n / —.

`MapPageAsync` calls `GetProjectionsAsync` once per page after `GetStatusesAsync`. No `GetProjectionAsync` / `ListEventsAsync` per row.

Filterable via `reservation` + `ApplyReservationFilterAsync` (batched). Saved view key unchanged.
