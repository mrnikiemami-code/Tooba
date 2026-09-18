# SSR / Hydration Contract — R13-R2

- Product list rendered as Embla slide children (title, image markup, price, PDP links) in initial HTML from Next SSR of client components.
- Hydration adds Embla drag/snap + selectedIndex depth transforms + keyboard rail controls.
- No client re-fetch for rail products.
- minHeight reserved on container to limit CLS.
- Offscreen images remain lazy per StorefrontProductCardView.
