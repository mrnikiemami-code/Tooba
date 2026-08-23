# TB-P04-T002 — Token map

## Reference (not semantic)

| Token | Role |
| --- | --- |
| `--ref-brand` | Study note (teal sample), not primary-by-copy of Shopeiva red |
| `--ref-danger` | Raw red note; product danger is `--color-danger` |

## Semantic color

background, surface, surface-elevated, foreground, muted, border, primary, primary-foreground, secondary, secondary-foreground, success, warning, danger, info, focus.

Light on `:root`. Dark on `.dark`.

## Space / radius / shadow / z / motion / type / density

`--space-1..8`, `--radius-sm|md|lg`, `--shadow-sm|md`, `--z-header|overlay|modal`, `--motion-fast`, `--type-display|title|body|caption`, `--density-control`.

## Mapping rule

Feature code must use semantic Tailwind aliases (`bg-danger`, `text-muted`), not `#E53935`.
