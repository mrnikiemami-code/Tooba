# 14 — New UI native-fit map (TB-P06-T018)

## Principle

If Shopeiva already has the capability chrome → reuse same pattern.  
If Tooba needs a subset Shopeiva does not express the same way → derive from closest Account/Vendor settings patterns and accepted Tooba panel shells.

## Wave 1 components

| Tooba surface | Closest Shopeiva source | Geometry / pattern reused | Notes |
|---|---|---|---|
| Customer settings live sections | Shopeiva Account settings / profile cards | Rounded white cards, icon tile, primary CTA, dashed unavailable sections | Profile bridge CTA; unavailable sections dashed border |
| Customer locale preference control | Storefront locale preference patterns + Account form controls | Existing cookie preference UX; panel form density | No foreign modal language |
| Seller settings operational page | Shopeiva Vendor settings / dashboard cards | Vendor shell card/table density; read-only operational blocks | No fake edit modal |
| Nav hide (all panels) | Existing panel shells | Same sidebar item geometry; simply omit deferred items | No “disabled grey” stubs left in primary nav |
| Admin settings hide | Admin shell groups | System group omits settings item | Route may remain deep-link honest |

## Patterns explicitly reused

- Card / form: `rounded-2xl`, `border-gray-200`, icon in tinted square, `font-black` titles  
- Buttons: Tooba blue primary (`#2563EB`) already accepted as minor brand deviation  
- Unavailable: dashed border + muted “فعلاً در دسترس نیست” (no fake primary save)  
- Transitions: existing hover/focus on nav and CTAs  

## Visual regression check

If any Wave 1 surface looks appended/foreign → treat as VISUAL REGRESSION and repair before ACCEPT. Captures pending in `15`.
