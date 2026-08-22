# Tooba — AI Assistant & RAG Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T018
```

Documentation only. No LLM calls, embeddings, vector databases, agents, tools, prompts, UI, providers, or Shopeiva.

```text
AI != Business Source of Truth
AI != Search Source of Truth
AI != Authorization System
AI != Direct Database Client
```

```text
Locale != Market != Currency
Authentication != Authorization
Backend/module boundary != UI boundary
```

Hard rules:

```text
AI/RAG is not Catalog, Content, Search, Analytics, or Authorization truth.
Answers about products/content/business facts are grounded in approved sources.
Retrieval is authorization-aware; SpiceDB remains the authorization decision system.
Do not dump all Media assets or Analytics raw history into AI context.
No unrestricted AI access to internal databases.
AI consumes approved published knowledge and module projections/contracts.
```

Modular monolith. No cross-module DB joins. Catalog/Content/Pricing/Inventory/Order remain authoritative.

## A. Core Separation

AI Assistant / RAG orchestrates grounded retrieval and model response. It does **not** own catalog identity, published content, search ranking, prices, stock, orders, permissions, or analytics observations.

Business modules remain authoritative. SpiceDB remains the authorization decision system. AI consumes **approved sources and contracts** only.

Do not treat model weights, conversation memory, or a vector index as truth for mutable commerce facts.

## B. AI Use Cases

**Current strategic requirement:** customer-facing storefront assistant.

Preserve future use cases without promoting them to launch scope:

| Class | Examples | Scope posture |
| --- | --- | --- |
| Customer (strategic) | product discovery, comparison, shopping guidance, FAQ/help, content Q&A | Design for now |
| Customer (authorized) | order/help guidance for the authenticated principal | Design for now; authz-gated |
| Seller (future) | offer insights, inventory help, content drafting | Future copilot |
| Admin (future) | operational summaries, content drafting assistance | Future copilot |

Do not treat template/chatbot catalogs as product requirements. Seller/admin copilots may exist later with **separate** scopes.

## C. Grounding Principle

AI answers about Tooba products, content, and business facts must be grounded in approved sources.

Conceptual flow:

```text
User Question
→ Context Resolution (tenant, locale, market, currency, channel, principal)
→ Authorization
→ Retrieval
→ Grounded Context
→ Model
→ Response + Provenance
```

Do not allow model memory alone to assert mutable facts such as: price, stock, seller availability, order status, policy, published content.

## D. Knowledge Sources

Approved source classes (minimum):

| Class | Role |
| --- | --- |
| Published Content | Primary RAG/editorial knowledge |
| Catalog projections | Product identity and descriptive facts |
| Product specifications | Structured attributes from Catalog projections |
| Brand/category content | Editorial + catalog-linked copy |
| FAQ/help | Approved help knowledge |
| Public seller facts | Only where approved for the audience |
| Pricing quote/read contract | Live price where the question needs it |
| Inventory/availability read contract | Live availability where the question needs it |
| Order/customer facts | Only when authorized for that principal |

Do not expose raw module tables. Do not ingest unpublished drafts, internal notes, Media blobs, or Analytics raw event history as a default knowledge dump.

## E. Published Content Source

Content is a primary RAG source. Retrieval may use only content that is:

```text
approved
published
eligible for AI retrieval
correct tenant/store
correct locale/audience
```

Draft, internal, and unpublished content must not leak. Preserve revision/version metadata on retrieved chunks. AI eligibility is a Content publishing concern, not an LLM filter.

## F. Catalog Source

AI may retrieve **normalized Catalog projections**, not Catalog tables. Conceptual fields: ProductId, localized title, specifications, brand, category, variant facts, approved descriptive data.

Catalog remains authoritative. The AI/vector store holds a **projection only**. Sellability, offer binding, and marketplace participation remain with their owning modules.

## G. Pricing / Inventory Freshness

Do not embed stale static price/stock and treat them as durable truth.

For live questions such as “How much is this?” / “Is it in stock?”, prefer live/internal read contracts or **freshness-aware** projections.

| Path | Purpose |
| --- | --- |
| Semantic retrieval | Find relevant products/content/chunks |
| Live business fact lookup | Current price, availability, order status |

Search/RAG ranking must not be mistaken for a quote. Checkout revalidates independently.

## H. Search vs AI Retrieval

```text
Product Search Index != automatically AI Knowledge Index
```

| Concern | General Search | AI retrieval |
| --- | --- | --- |
| Optimizes | query/result ranking, facets | chunk retrieval, semantic relevance, grounding, provenance, authorization |
| Output | ranked product/content ids + display projection | evidence chunks + ids for grounded generation |
| Truth | not Catalog/Pricing/Inventory SoT | not any business SoT |

They may share **source projections** (Catalog/Content feeds) but must not be architecturally conflated. Do not query the customer Search index as if it were the AI knowledge store, and do not use the AI index as product Search.

## I. Retrieval Abstraction

Conceptual internal contract (names not locked):

```text
IAIKnowledgeRetriever
RetrieveAsync(query, context)
```

Context includes tenant, locale, market, currency, sales channel, principal, visibility class, and authorization scope.

Infrastructure may later use PostgreSQL vector extension, a vector database, hybrid lexical/vector search, or an external provider. **Do not choose technology now.** Domain/application code depends on the internal contract, not a vendor SDK.

## J. Hybrid Retrieval

Preserve combining:

```text
lexical search
semantic/vector search
structured filters
live business lookups
```

Not every query must use embeddings. Product codes, SKUs, and exact identifiers often need lexical/structured retrieval first, then optional semantic expansion.

## K. Chunking

Chunking is deliberate and may vary by source type (FAQ vs long article vs spec table). Do **not** lock chunk size/overlap.

Preserve metadata on chunks:

```text
SourceId
RevisionId
Locale
Heading/section
ChunkId
Tenant
Visibility
Effective dates
Entity references
```

Entity references are opaque ids (product, brand, category, content), resolved via contracts — not foreign-table joins.

## L. Embeddings

Embeddings are **derived projections**, not business truth.

Need (or equivalent): EmbeddingModelVersion, ChunkVersion, SourceRevision, GeneratedAt, Tenant.

Changing embedding model must permit reindex/rebuild. Deleting a vector store must not delete Catalog/Content truth.

## M. Provider Abstraction

LLM provider/model sit behind internal abstractions (conceptual):

```text
ILLMClient
IEmbeddingProvider
```

Do not couple domain/application to one vendor SDK. Future providers/models may differ by cost, latency, quality, language, data policy, availability. **No provider is chosen here.**

## N. Model Routing

Preserve future routing among: small/fast model, large/high-quality model, embedding model, classification/routing model.

Do not require one model for every AI operation. Exact routing policy is later (`NEEDS_LATER_P00_DETAIL`).

## O. Authorization-Aware Retrieval

Mandatory. Before private, customer, seller, or admin data reaches the model:

```text
authenticate principal
resolve tenant/context
authorize resource/scope
retrieve only permitted data
```

Do not retrieve everything and ask the LLM to hide forbidden data. Authorization happens **before/at** the retrieval boundary. SpiceDB / `IAuthorizationService` remains the decision system. Authentication ≠ Authorization.

## P. Public vs Private AI Context

| Context | Typical retrieval/tools |
| --- | --- |
| Public storefront assistant | Published content, public catalog projections, public seller facts |
| Authenticated customer | Public plus own order/account/help, after SpiceDB |
| Seller | Seller-scoped projections/tools only |
| Admin | Admin-scoped operational summaries/tools only |

Each has different retrieval and tool permissions. Do **not** reuse a broad admin index for public chat.

## Q. Tenant Isolation

**Single-Store:** every retrieval, index, chunk, cache, and conversation scope includes immutable TenantId (or equivalent resolved store identity).

**Marketplace:** use marketplace deployment, resource, seller, and customer scopes appropriately. Do not invent fake tenant semantics.

Hard rule: no cross-tenant retrieval due to a missing filter. Isolation applies to embeddings, caches, conversation stores, and tool results.

## R. Locale / Multilingual AI

Support Persian, English, and future locales.

Need: question locale detection/selection, retrieval in the correct locale, fallback policy, translated content provenance, multilingual embeddings/provider compatibility.

Do not assume English-only models or indexes. Locale is presentation language — not Market or Currency. Fallback must not silently mix unlabelled locales into one evidence bag without provenance.

## S. Market / Currency Context

Shopping answers may need Market, Currency, and SalesChannel, independent of Locale.

```text
Locale != Market != Currency
```

Do not quote a price from the wrong market because the user speaks that locale. Live price lookup uses Pricing contracts with resolved commercial context.

## T. Provenance / Citations

Grounded responses preserve evidence metadata (conceptual): Source Type, SourceId, Revision/Version, Title/Label, Route/URL reference if public, RetrievedAt.

UX should be able to show citations/source links where supported. No fabricated sources. If evidence is insufficient, do not invent a citation.

## U. Hallucination Guardrails

When evidence is insufficient, preferred behavior:

```text
state uncertainty
ask for needed context where appropriate
use deterministic tool/read contract
or decline to assert unsupported fact
```

Do not make unsupported confident claims about live commerce data (price, stock, order status, policy).

## V. Tool / Action Boundary

Future AI may propose actions such as: add item to cart, search products, retrieve order status, start support workflow.

Hard rule:

```text
Model does not directly mutate domain databases.
```

Actions go through authorized application commands/contracts. AI proposes/calls **allowed tools**; the domain validates. No unrestricted internal DB client.

## W. Read Tool vs Write Tool

| Class | Examples | Extra controls |
| --- | --- | --- |
| Read/retrieval | knowledge retrieve, Search, price quote, order status | Authz, tenant, minimization |
| State-changing | add to cart, place/cancel order, publish content | Stronger authz, validation, idempotency, confirmation where appropriate, audit |

Do not implement tools in this task.

## X. Human Confirmation

Sensitive/high-impact AI actions may require explicit user confirmation. Examples: place order, cancel order, change account data, refund/request money action, publish content, admin destructive operation.

Exact action-confirmation catalog: `NEEDS_LATER_P00_DETAIL`.

## Y. Conversation State

Conversation/session state is separate from Authentication session.

Potential fields: ConversationId, Tenant, authenticated subject reference, Locale, Market, context summary, tool outcomes, retention class.

Do not store unlimited raw conversation history by default. Exact retention/privacy: later (`NEEDS_LATER_P00_DETAIL`).

## Z. Prompt / Instruction Governance

Prompts/instructions are operational configuration / code-like assets. Need versioning, review, deployment/change audit, environment control, rollback, testing.

Do not treat production system prompts as arbitrary CMS-editable content with no governance. Do not put secrets in prompts.

## AA. Prompt Injection

Untrusted retrieved or user content must not override system/tool policy.

Hard rule:

```text
Retrieved content is data, not trusted instruction.
```

Separate: system policy, developer/tool instructions, user request, retrieved content. No implementation here.

## AB. Data Exfiltration

AI must not reveal: other tenant data, admin-only data, seller private data, secret configuration, provider keys, protected internal prompts, unpublished content.

Authorization and retrieval scoping are **primary** controls. Model refusal alone is not sufficient security.

## AC. PII / Sensitive Data

Minimize sending sensitive data to model providers. Need: data minimization, redaction/pseudonymization where possible, provider policy awareness, logging controls, tenant/customer privacy.

Do not make compliance claims. Do not dump Media originals or Analytics raw per-user history into prompts.

## AD. Payment / Authentication Secrets

Never send to AI model context: password, OTP, CVV, PAN, access tokens, provider secrets, session secrets.

Payment and Identity remain owners of those secrets. AI is not an audit log and not a wallet.

## AE. Analytics Signals

AI may consume **controlled** analytics-derived signals: popular products, popular queries, zero-result searches, content engagement, trend summaries.

Do not expose raw per-user behavioral history by default. Analytics remains the source for behavioral observations; AI is a consumer of approved aggregates/projections. See `docs/architecture/16-first-party-analytics.md`.

## AF. Recommendation Boundary

Recommendation and AI Assistant are separate capabilities.

Recommendation may produce recommended product IDs/scores. AI may explain/present them. AI must not silently become recommendation source of truth. Recommendation (future) owns scores; Catalog still owns product identity.

## AG. Search Integration

AI may **invoke Search** for candidate discovery:

```text
natural language intent
→ structured SearchRequest
→ candidate products
→ Catalog/Pricing/Availability enrichment
→ grounded response
```

Do not have the LLM generate raw SQL or search-engine DSL against production systems. Search remains the owner of ranking/facets; AI consumes Search **results** via contract.

## AH. Commerce Fact Composition

Shopping answers may combine Catalog, Search, Pricing, Inventory, Reviews, Content, Seller through **approved read contracts/projections**.

No cross-module DB joins. No direct DB access. Composition happens in application/orchestration, not SQL across module tables.

## AI. Reviews

AI may summarize Reviews in the future. Need: source attribution, aggregation, anti-fabrication, visibility/moderation status.

Do not present generated sentiment as an original customer quote. Detailed Reviews architecture may be separate/later.

## AJ. Order / Customer Context

Authenticated customer assistant may read own orders, own delivery status, own account/help context — **only after SpiceDB authorization**.

Do not leak other customers’ records. Use application read contracts, not Order tables. Guest public assistant has no order lookup by guessing ids.

## AK. Seller / Admin AI

Future seller/admin copilots require stronger authorization and **separate** tool scopes and knowledge stores/projections.

Examples: seller offer insights, inventory help, content drafting, admin operational summaries.

Do not expose broad internal access just because the assistant is labelled “admin AI.”

## AL. Content Drafting AI

If future Content editors use AI drafting, generated content remains **Draft** until human/editorial workflow approves and publishes it.

AI output must not bypass the Content review/publishing lifecycle or AI-eligibility flags.

## AM. AI-Generated SEO Content

Do not automatically publish mass AI-generated pages. SEO/programmatic content still requires quality, uniqueness, editorial/policy approval, and indexability rules.

AI is a tool, not permission to generate thin SEO spam. SEO technical ownership remains with SEO policy; Content remains editorial SoT.

## AN. Response Caching

AI responses may be cacheable only when context allows.

Possible cache dimensions: Tenant, Locale, Market, authorization/public scope, knowledge version, model/prompt version.

Private/personal responses require strong isolation. Do not cache sensitive personalized answers globally.

## AO. Cost Controls

Preserve controls for: token budget, retrieval limit, model routing, rate limiting, per-tenant quota, per-user quota, tool-call limit, conversation length.

No provider-specific billing assumptions.

## AP. Abuse Protection

Need controls for: spam, automated scraping via AI, prompt injection attempts, tool abuse, expensive-query abuse, denial-of-wallet.

Do not select CAPTCHA or a vendor here.

## AQ. Latency / UX

Customer AI UX must be responsive. Preserve: streaming response future, progress/loading, tool-call progress where useful, cancel, retry, source display, fallback when AI unavailable.

AI must not become a blocking dependency for normal catalog browsing or purchase. Core storefront works without AI.

## AR. UI / UX

AI UI must be professional and trustworthy — not a generic detached chatbot that ignores commerce.

Future UX should support: clear assistant identity, citations/sources, product cards, price/availability freshness cues, follow-up questions, conversation history as policy permits, mobile UX, RTL/LTR, keyboard/accessibility, loading/streaming states, error/fallback states, safe confirmation for actions.

```text
Backend/module boundary != UI boundary
```

Distinguish AI suggestion from confirmed business fact (especially price/stock). Visual evidence and Architect visual acceptance apply to future UI tasks.

## AS. AI Unavailable Degradation

If AI provider or retrieval is unavailable: storefront, search, checkout, and account remain usable.

AI is an enhancement, not a single point of failure for core commerce.

## AT. Observability

Need AI telemetry such as: request latency, model/provider, token usage, retrieval latency, retrieved source count, tool calls, error rate, fallback rate, grounding/citation coverage, cost, tenant usage.

Do not log sensitive prompt/context indiscriminately. Technical AI telemetry ≠ product Analytics and ≠ audit of payments/authn.

## AU. Evaluation

Quality requires systematic evaluation, not only manual chat testing.

Preserve: golden question set, groundedness checks, citation correctness, answer relevance, retrieval recall, hallucination rate, authorization leakage tests, Persian/English quality, tool-action correctness.

## AV. Safety Testing

Future implementation must test: prompt injection, cross-tenant leakage, unauthorized order lookup, unpublished content retrieval, secret exfiltration, malicious document instructions, tool misuse, false price/stock claims, citation mismatch.

## AW. Knowledge Refresh

When source facts change (ContentPublished, ContentUnpublished, CatalogChanged, ProductArchived, and equivalents), AI knowledge projections must update or remove stale content.

Need: event-driven refresh, rebuild/backfill, tombstone/delete, version awareness.

Dynamic price/inventory should prefer live lookups rather than waiting on embedding refresh.

## AX. Rebuild / Reindex

AI vector/knowledge indexes must be rebuildable from authoritative approved sources.

Need: source export, chunk rebuild, embedding regeneration, new index/version, validation, cutover.

No business truth is lost if the vector store is deleted.

## AY. Model / Embedding Versioning

Preserve (or equivalent): PromptVersion, ModelVersion, EmbeddingModelVersion, KnowledgeIndexVersion, RetrieverVersion, ToolSchemaVersion.

This enables reproducibility, evaluation, and rollback.

## AZ. External Provider Failure

Need fallback behavior for: LLM unavailable, embedding provider unavailable, vector store unavailable, retrieval timeout, tool timeout, quota exhausted, rate limited.

Do not fabricate answers when grounding infrastructure fails. Prefer decline, Search fallback for product discovery where appropriate, or “AI unavailable” UX — never invented prices/stock.

## BA. Data Ownership Matrix

Marks: `OWNER` | `SOURCE` | `PROJECTION` | `CONSUMER` | `TOOL` | `NOT_OWNER`

| Fact | AI Assistant | AI Knowledge Projection | Content | Catalog | Search | Pricing | Inventory | Order | Authorization | Analytics | Recommendation (future) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Published content | CONSUMER | PROJECTION | OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Product truth | CONSUMER | PROJECTION | NOT_OWNER | OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER |
| Search ranking | CONSUMER / TOOL | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Embedding | CONSUMER | OWNER | SOURCE | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Chunk | CONSUMER | OWNER | SOURCE | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Live price | TOOL | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Stock | TOOL | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Customer order | TOOL | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Permission | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER |
| Behavioral signal | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | CONSUMER |
| Assistant response | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Recommendation score | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER |

Assistant **response** is an AI-owned utterance with provenance; it is never Catalog/Content/Order truth. Search ranking consumed via Search contract is TOOL/CONSUMER, not AI-owned ranking.

## BB. Failure Matrix

| Case | Degrade? | Refuse/decline assertion? | Retry? | Fallback to Search? | Show sources? | Operational alert? |
| --- | --- | --- | --- | --- | --- | --- |
| No Relevant Evidence | Yes — limited answer | Yes for live/mutable facts | Optional rephrase | Optional for product find | If any exist | Low |
| Stale Knowledge | Prefer live lookup | Yes if cannot refresh live fact | Refresh projection | If discovery still needed | With RetrievedAt / version | Medium if chronic |
| Unauthorized Source | Exclude from retrieve | Do not answer from it | No | Public Search only if allowed | No forbidden sources | Yes if systematic |
| Cross-Tenant Retrieval | Fail closed | Always | No | No | No | Yes |
| LLM Provider Down | AI UX fallback | Do not invent facts | Bounded retry | Yes for product discovery UX | N/A | Yes |
| Vector Store Down | Lexical/structured retrieve if available | If no evidence | Bounded retry | Yes | If lexical hits | Yes |
| Live Price Lookup Fails | No price claim | Yes on price | Bounded retry | Search without treating rank as quote | Product identity only | Medium |
| Inventory Lookup Fails | No stock claim | Yes on stock | Bounded retry | Search flags are not SoT | Product identity only | Medium |
| Tool Authorization Denied | Skip tool | Yes for that action | No | Public path only | N/A | Medium |
| Tool Timeout | Skip/partial | For that fact/action | Bounded retry | If it was Search tool | Partial | Medium |
| Prompt Injection Detected | Strip/ignore injected policy | May refuse tool/override | No | Safe public retrieve only | Untrusted as data | Yes |
| Citation Missing | Qualify answer | For claims that need evidence | Re-retrieve | Optional | Do not fabricate | Low/medium |
| Model Output Invalid | Discard/retry schema | Yes until valid | Bounded retry | Optional | Only valid citations | Medium |
| Quota Exceeded | AI unavailable UX | No new generation | After quota window | Storefront Search | N/A | Yes |

Core catalog browse, Search, cart, checkout, and account must remain available regardless of these AI failures.

## BC. Testing Strategy — Architecture Level

Future implementation must test: grounded public Q&A; published-only content; tenant isolation; authorization-aware private retrieval; Persian/English retrieval; live price lookup; live inventory lookup; Search + RAG composition; prompt injection defense; source deletion propagation; embedding/model reindex; citation provenance; provider fallback; tool confirmation; action idempotency; AI-unavailable core-storefront behavior.

No tests in this task.

## BD. Decision Summary

Not an ADR lock.

### RECOMMENDED_FOR_ADR

1. AI is not business/search/authorization truth.
2. Customer AI answers are grounded on approved sources.
3. Content AI retrieval uses only published/approved/eligible revisions.
4. Catalog facts enter via projections/contracts.
5. Dynamic price/inventory use freshness-aware live reads where needed.
6. General Search index and AI knowledge index are separate concerns.
7. AI retrieval/provider infrastructure is hidden behind internal abstractions.
8. Authorization happens before/at retrieval, never delegated to the LLM.
9. Tenant isolation applies to chunks/indexes/cache/conversations/tools.
10. Locale/Market/Currency remain separate AI context.
11. Provenance/citations are preserved.
12. Retrieved/user content is untrusted data, not system instruction.
13. AI has no direct DB mutation/access path.
14. State-changing actions use authorized application commands/tools.
15. Sensitive/high-impact actions support explicit confirmation policy.
16. Prompt/model/embedding/retriever/tool schemas are versioned.
17. AI indexes are rebuildable from authoritative sources.
18. AI provider failure does not break core commerce.
19. Analytics/recommendation signals enter through controlled interfaces.
20. AI UI/UX is commerce-aware, cited, mobile/RTL/a11y capable.
21. AI quality requires automated evaluation including authorization-leak tests.
22. Mass AI-generated SEO content cannot bypass Content/SEO governance.

### NEEDS_LATER_P00_DETAIL

- Exact model routing policy
- Action-confirmation catalog
- Conversation retention/privacy
- Locale detection and fallback policy details
- Chunk size/overlap strategies by source type
- Cache TTLs and personalization isolation rules
- Quota/rate numbers and abuse-control vendors
- Reviews summarization policy (if not a later Reviews doc)

### DEFERRED

- Provider/model selection
- Vector/embedding infrastructure choice
- RAG/tool/prompt/UI implementation
- Seller/admin copilots beyond scope reservation
- Recommendation implementation
- Shopeiva integration
- Final ADR
