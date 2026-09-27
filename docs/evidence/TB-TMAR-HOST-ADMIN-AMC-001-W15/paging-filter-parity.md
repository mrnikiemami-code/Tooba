# Paging / filter parity — W15

Preserved exactly from prior Host + CatalogDirectory behavior:

| Rule | Behavior |
|---|---|
| Endpoint skip null | → 0 |
| Endpoint take null | → 50 |
| skip normalize | `Math.Max(0, skip)` |
| take normalize | `Math.Clamp(take <= 0 ? 50 : take, 1, 100)` |
| section blank/null | no filter |
| section nonblank | Trim then exact `Section ==` |
| TotalCount | after section filter, before Skip/Take |
| Order | OccurredAt DESC, then HistoryId DESC |
| Response | includes normalized Skip/Take |
