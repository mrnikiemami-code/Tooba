# 15 — Unsafe CTA rejection (TB-P06-T017)

## Rule

`StoryRules.ValidateCta` rejects targets starting with:

- `javascript:`
- `data:`
- `vbscript:`

(case-insensitive). Allowed CTA types only: `none`, `product`, `category`, `article`, `internal`, `external`.

## Domain

Create/Update story or item with forbidden scheme → `InvalidOperationException` («هدف CTA ناامن است.»).

## Host

Admin mutation catch maps message containing «ناامن» → HTTP **400** `errorCode: story.cta.rejected`.

## Test proof

`StoryFoundationTests` asserts:

```csharp
AdminCreateAsync(..., ctaType: "external", ctaTarget: "javascript:alert(1)")
  → Throws InvalidOperationException
```

No public storefront path accepts arbitrary CTA writes; customers cannot inject CTAs (AddStory omitted).
