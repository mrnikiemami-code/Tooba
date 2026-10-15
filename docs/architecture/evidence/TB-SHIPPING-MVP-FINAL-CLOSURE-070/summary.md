# TB-SHIPPING-MVP-FINAL-CLOSURE-070 — evidence index

| file | what it proves |
| --- | --- |
| `harness/check.csproj`, `harness/Program.cs` | focused, read-only harness that constructs the real `GeographyPlaceLookup` over the real `GeographyDbContext` on `tooba_alpha` |
| `log-resolver-trace.txt` | country/area/label rows + byte-level comparison + the exact observer call returning `Resolved` |
| `log-resolver-probe.txt` | seven live storefront projection requests, real-UTF-8 vs Latin-1 body encoding, same-cart identity |
| `log-observer-070.txt` | the host observer lines: 5 × `Observed` (5/5 `Match`) for the intact-text/validated-id shapes, 2 × `DestinationNotComparable` for the mangled-body shapes |
| `payloads-probe.json` | the seven served payloads, byte-identical to each other (legacy projection invariant) |
| `log-admin-option-roundtrip.txt` | option source from `/v1/admin/shipping-methods`, read/edit/toggle through the exact UI payloads, `shipping.service_option.unknown` refusal, final fixture state |
| `ui-tipax-option-dialog.png` | the tipax dialog with the real option `<select>` and per-option grouped rows |
| `ui-tipax-option-roundtrip.png` | the final UI state after a genuine UI edit and a genuine UI active toggle |
| `log-post-restore.txt` | restored host log after the shadow override was removed (zero observer lines) |
| `bridge-task-row.txt` | the Bridge task row id claimed for this task |

Full narrative and the five-category delivery verdict: `docs/planning/shipping/SHIPPING-MVP-FINAL-CLOSURE-070.md`.
