# Fashion preview route

- Route: `/template-preview/fashion`
- Layout: `app/template-preview/layout.tsx` (robots noindex; no Admin chrome)
- Page: `app/template-preview/fashion/page.tsx`
- Renderer: `StorefrontLandingSections` → `adaptLandingSectionToComposition` → `renderSharedLandingSection`
- Shell: `StorefrontShell`
- Preview-safe: link/submit capture prevents cart/auth navigation side effects
- Origin marker: `fashion-template-preview-pilot` (`data-demo-origin`)
