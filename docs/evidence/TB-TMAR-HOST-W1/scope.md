# TB-TMAR-HOST-W1 Scope

Slice: Store Landing / Page Composition Host direct writes only.

In-scope Host write methods (before):
- StoreLandingPageComposer.CreateAsync
- UpdateAsync
- SetStatusAsync
- SetHomeAsync (explicit BeginTransactionAsync when relational)
- AddSectionAsync
- ReplaceCompositionAsync
- UpdateSectionAsync
- SetSectionEnabledAsync
- ReorderSectionsAsync
- DeletePageAsync
- DeleteSectionAsync

Out of scope:
- StoreAppearance / StoreMenu / other Host composers
- Domain ownership move for StoreLandingPage*
- Contracts wave / Redis / folder moves
- Broad Host write cleanup

Evidence of exclusion: no Domain type moves; Catalog.Domain StoreLandingPage* unchanged as ownership.
