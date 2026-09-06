# Runtime smoke

Target Host `:5088` / FE `:3000`. After migrate `20260907010000_CheckoutOperationalNotes`, open an admin order detail: add note → reload persists; history shows `order_created`+; invoice opens snapshot money; receipt when payment present. Host was stopped briefly during build (file lock); restart Host to apply endpoints/migration.
