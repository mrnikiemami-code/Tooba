# Disposition map — Story module

| Area | Disposition |
| --- | --- |
| Endpoints Admin/Seller/Storefront | KEEP — HTTP_ENDPOINT module-owned |
| StoryHttpErrors | KEEP — GLOBAL presentation via ApiResponseFactory |
| Error catalog + resx | KEEP — extend codes as Domain fails are coded |
| StoryFailureMapper.ToSemantic | REMOVE / REWRITE — message classification ILLEGAL |
| StoryFailureMapper review-status parse | KEEP in Application |
| StoryPresentationComposer | KEEP role; REMOVE IOE→message map Guard |
| Domain Story / StoryRules throws | REWRITE to SemanticException+codes |
| StoryDirectory IOE throws | REWRITE to SemanticException+codes |
| StoryContracts.cs | MUST_SPLIT (W3+ / later) |
| StoryEntities.cs | MUST_SPLIT (later structure wave) |
| IStoryDirectory | KEEP Application port |
| Grid ports/adapters | KEEP Infrastructure |
| Host Story | ZERO — preserved |
