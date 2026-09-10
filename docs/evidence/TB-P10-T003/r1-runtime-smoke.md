# R1 runtime smoke
Host :5088. Multi-seller KG+Arman, shipping 200000.
A allocation: StoreShipping separate; sellers == merchandise; amount==payable.
B reverse offer order: same seller amounts + same shipping bucket.
C settlement semantics: seller buckets exclude shipping.
D idempotent replay same paymentId.
E single-store still works with StoreShipping.
Raw: r1-runtime-raw.json
