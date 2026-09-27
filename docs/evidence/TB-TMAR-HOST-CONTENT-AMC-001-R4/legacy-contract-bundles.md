# Legacy contract bundles — R4

## Before
Application/Models/*Contracts.cs mixed DTOs/snapshots and command-shaped inputs:
- ContentContracts.cs
- ContentArticleCommentContracts.cs
- ContentArticleMediaContracts.cs
- ContentAuthorContracts.cs
- ContentCategoryContracts.cs
- ContentPublicTaxonomyContracts.cs
- ContentTagContracts.cs

## After
ZERO Application *Contracts.cs files.
Models split by capability under Application/&lt;Capability&gt;/Models/.
Duplicate command-shaped records removed; directories accept authoritative MediatR commands.
