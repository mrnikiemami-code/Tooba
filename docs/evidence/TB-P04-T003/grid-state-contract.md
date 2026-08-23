# TB-P04-T003 — Grid state contract

Shareable query (`serializeGridQuery` / `deserializeGridQuery`):

- page
- pageSize
- sorts: `{ columnId, direction }[]`
- filters: `Record<columnId, GridFilterValue>`
- search optional

Not coupled to Next.js searchParams. A workspace may persist the JSON string in the URL.

Saved view (`serializeSavedView`):

- id, name
- filters, sorts
- layout.order, layout.visibility, layout.widths
- pageSize
- density optional

Money normalization: finite amount, non-empty currency uppercased.

Date normalization: canonical ISO `YYYY-MM-DD`. Jalali belongs to input widgets later, not this contract.

Selection is UI session state, not part of the shareable query (avoids leaking row ids into bookmarks unless a feature opts in).
