# backward-compatibility — TB-P10-T022-R13-R1-R1

Unchanged from R13-R1 adapters:

| Legacy | Adapter |
|--------|---------|
| `displayHeightPx` | Maps to Medium/Large/ExtraLarge |
| obsolete variant keys | Hidden for new edits; resolve for existing |
| published slider | Continues to render via single HeroSlider |

This repair only adds sessionStorage pending-wizard reopen on create remount — no schema/config change.
