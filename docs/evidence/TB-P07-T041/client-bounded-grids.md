# Client bounded grids — TB-P07-T041

Surfaces kept on **pure client** `createClientGridQueryAdapter` after one bounded load:

- Settlement balances — seller-party cardinality
- Promotions — seller-scoped promotion list
- Attribute definitions — catalog definition registry per tenant
- Category schema — effective schema per selected category
- Gift cards — admin wallet issuance list with existing server filter params on load

Reasoning documented in `dataset-classification.md`. No full-dataset browser simulation for non-trivial lists.
