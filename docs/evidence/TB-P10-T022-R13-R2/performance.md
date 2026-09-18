# Performance — R13-R2

- Single Embla instance per section (no duplicate hidden carousels).
- Embla select/reInit events only; no permanent RAF loop; no resize polling; no autoplay.
- No product API/schema change; no N+1.
- Dependency: embla-carousel-react 8.5.2 (+ embla-carousel core).
