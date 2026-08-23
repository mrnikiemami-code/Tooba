# Tooba — Professional Data Grid Foundation

Status:

```text
Foundation implemented — not visual ACCEPT, not workspace ACCEPT
```

Task:

```text
TB-P04-T003
```

The Data Grid is reusable UI infrastructure inside the Design System. It is not a domain module. Backend/module boundary is not a UI boundary. Workspaces later compose this Grid; this task does not implement Product, Order, Seller, or Customer workspaces.

## Architecture

Owned path: `src/frontend/design-system/data-grid/`.

Separated seams:

| Concern | Location |
| --- | --- |
| Column / filter / query types | `types.ts` |
| Serializable query and layout | `serialize.ts` |
| In-memory demonstration of the server contract | `query-engine.ts` |
| Message catalogs | `messages.ts` |
| Typed filter controls | `FilterControl.tsx` |
| Toolbar, table/cards, pagination, selection | `DataGrid.tsx` |
| Storage-agnostic saved views | `SavedViewStore` + `createMemorySavedViewStore` |

The Grid consumes row view-models (`T extends { id: string }`). It does not import Catalog, Order, Party, Pricing, or SpiceDB.

## Column model

`GridColumnDef<T>`: id, header, accessor, optional cell renderer, sortable, filterKind/filterable, resizable, reorderable, hideable, sticky start/end, align, width/min/max, exportable, enumOptions.

Layout (`GridColumnLayout`): order, visibility, widths. Restore defaults resets to column definitions.

## Typed filters

| Kind | Value contract |
| --- | --- |
| text | operator contains/equals/startsWith + query |
| number | numeric operators + value/valueTo |
| money | same operators + amount/currency/amountTo |
| date | on/before/after/between + canonical ISO (`iso`/`isoTo`). Jalali is presentation only |
| enum | values[] |
| status | values[] |
| boolean | tri-state all/true/false |
| entity | ids[] + optional search; `EntityFilterAdapter.search` supplied by the feature |

Inactive filters are omitted from the demonstration query engine.

## Sorting

Serializable `GridSort[]`. UI cycles a column through asc → desc → none. The contract allows multiple sorts; the demonstration engine applies them stably. Adapters send the array to the server.

## Saved views

`SavedGridView`: id, name, filters, sorts, layout, pageSize, optional density. `SavedViewStore` is list/save/remove. Memory store is demo-only. Future HTTP persistence implements the same interface.

## Pagination and server query

`GridServerQuery`: page, pageSize, sorts, filters, optional search. `GridQueryAdapter` returns `{ rows, total }`. No SQL/EF types in the Grid. Shareable state serializes via `serializeGridQuery` without coupling to the Next router.

## Selection, bulk, export

Page selection and per-row toggle. No select-all-across-pages. Bulk actions: id, label, isAvailable, requiresConfirmation, execute. Export: visible columns CSV, selected rows CSV, `onServerExport(query)` seam. Browser CSV is not for full server dumps.

## Sticky, keyboard, RTL, density

Sticky header. Optional sticky start/end using `inset-inline-*` (`stickyLogicalSide`). Keyboard: Tab, sort Enter/Space, Alt+Arrow and drawer buttons for reorder, range input for resize, filter/column drawers, checkbox selection. Density comfortable (default) or compact. Not a certified ARIA grid.

## Responsive

Desktop: horizontal overflow, priority sticky key column. Viewport &lt; 768px: card adapter for the current page, filter drawer. Screens own layout composition; the Grid does not shrink dense tables until unreadable.

## Performance

Adapters page on the server. The demonstration engine exists to prove the contract, not to ship large arrays to the browser. Virtualization is not included and is not blocked: row rendering is a page of view-models.

## Dependency decision

No TanStack Table and no enterprise grid package. T001 classified Shopeiva tables as REBUILD. Native table + Design System primitives keep RTL logical CSS, token density, and Persian documentation under Tooba ownership. Revisit TanStack only with a later envelope if virtualization or column virtualization is required.

## Future API / workspace integration

A workspace supplies: view-model mapping, `GridQueryAdapter` over HTTP, permission flags via `isAvailable`/column visibility, `EntityFilterAdapter`, saved-view HTTP store, server export endpoint. SpiceDB stays behind the feature API.

## Showcase

Internal `/design-system` (robots noindex) hosts a synthetic operations grid. Not visual product ACCEPT.
